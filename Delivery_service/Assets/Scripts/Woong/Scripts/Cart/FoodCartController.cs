using System.Collections.Generic;
using UnityEngine;

namespace DeliveryService.Mok
{
    public class FoodCartController : MonoBehaviour, IInteractable
    {
        [Header("카트 본체 및 연결 설정")]
        public Transform cartRoot;
        public Rigidbody cartRigidbody;
        public FoodCartStorage cartStorage;

        [Header("카트 이동 및 방향 설정")]
        public float pushMoveSpeed = 10.0f;
        [Tooltip("카트를 밀 때 좌우 회전 반응 속도")]
        public float turnSmoothSpeed = 12f;
        [Tooltip("피자 1박스당 느려지는 속도")]
        public float speedPenaltyPerPizza = 0.1f;
        [Tooltip("플레이어 앞쪽으로 카트가 유지할 거리")]
        public float holdDistance = 1.35f;

        public Vector3 cartRotationOffset = Vector3.zero;

        [Header("NPC 레그돌 충돌 설정")]
        public float minKnockdownSpeed = 1.2f;
        public float knockdownForce = 9.5f;
        public int collisionDamage = 25;

        [Header("바퀴 회전 연출")]
        public Transform[] wheels;
        public float wheelRotateMultiplier = 180f;

        private bool isPushing = false;
        private Transform playerTransform;
        private CharacterController playerCC;
        private PlayerCombat playerCombat;
        private Vector3 lastCartPosition;
        public Vector3 CurrentVelocity { get; private set; }

        private Collider[] cartColliders;
        private Collider[] playerColliders;

        private Dictionary<int, float> hitCooldowns = new Dictionary<int, float>();

        private void Awake()
        {
            if (cartRoot == null) cartRoot = transform.root;
            if (cartRigidbody == null) cartRigidbody = cartRoot.GetComponent<Rigidbody>();
            if (cartStorage == null) cartStorage = cartRoot.GetComponentInChildren<FoodCartStorage>();

            // 카트의 모든 충돌체를 미리 찾아둠
            cartColliders = cartRoot.GetComponentsInChildren<Collider>();

            if (cartRigidbody != null)
            {
                cartRigidbody.mass = 80f;
                cartRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                cartRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                cartRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                cartRigidbody.isKinematic = true;
            }

            if (cartRoot.gameObject != gameObject && cartRoot.GetComponent<CartCollisionRelay>() == null)
            {
                var relay = cartRoot.gameObject.AddComponent<CartCollisionRelay>();
                relay.controller = this;
            }

            lastCartPosition = cartRoot.position;
        }

        public void Interact(ToppingInventory inventory)
        {
            if (inventory == null) return;

            if (!isPushing)
                StartPushing(inventory.transform);
            else
                StopPushing();
        }

        private void StartPushing(Transform player)
        {
            isPushing = true;
            playerTransform = player;
            playerCC = player.GetComponent<CharacterController>();
            playerCombat = player.GetComponent<PlayerCombat>();

            if (cartRigidbody != null) cartRigidbody.isKinematic = false;

            playerColliders = player.GetComponentsInChildren<Collider>();
            if (cartColliders != null && playerColliders != null)
            {
                foreach (var cCol in cartColliders)
                {
                    foreach (var pCol in playerColliders)
                    {
                        if (cCol != null && pCol != null) Physics.IgnoreCollision(cCol, pCol, true);
                    }
                }
            }
        }

        public void StopPushing()
        {
            isPushing = false;
            playerTransform = null;
            playerCC = null;

            if (cartRigidbody != null)
            {
                cartRigidbody.linearVelocity = Vector3.zero;
                cartRigidbody.angularVelocity = Vector3.zero;
                cartRigidbody.isKinematic = true;
            }

            if (cartColliders != null && playerColliders != null)
            {
                foreach (var cCol in cartColliders)
                {
                    foreach (var pCol in playerColliders)
                    {
                        if (cCol != null && pCol != null) Physics.IgnoreCollision(cCol, pCol, false);
                    }
                }
            }
            playerColliders = null;
        }

        private void Update()
        {
            if (!isPushing) return;

            if (playerTransform == null || (playerCombat != null && !playerCombat.CanAct))
            {
                StopPushing();
                return;
            }

            if (Vector3.Distance(playerTransform.position, transform.position) > 3.2f)
            {
                StopPushing();
                return;
            }
        }

        private void FixedUpdate()
        {
            CurrentVelocity = (cartRoot.position - lastCartPosition) / Mathf.Max(0.0001f, Time.fixedDeltaTime);
            lastCartPosition = cartRoot.position;

            RotateWheels(CurrentVelocity.magnitude);

            if (!isPushing || playerTransform == null || cartRigidbody == null) return;

            int pizzaCount = cartStorage != null ? cartStorage.StoredCount : 0;
            float targetSpeed = Mathf.Max(1.5f, pushMoveSpeed - (pizzaCount * speedPenaltyPerPizza));

            Vector3 targetPos = playerTransform.position + playerTransform.forward * holdDistance;
            targetPos.y = cartRoot.position.y;

            Vector3 moveDir = (targetPos - cartRoot.position);

            // 플레이어의 위치를 즉각적으로 따라가도록 힘을 높임
            Vector3 desiredVel = moveDir * 15f;

            if (desiredVel.magnitude > targetSpeed)
            {
                desiredVel = desiredVel.normalized * targetSpeed;
            }

            cartRigidbody.linearVelocity = new Vector3(desiredVel.x, cartRigidbody.linearVelocity.y, desiredVel.z);
            Quaternion baseRot = Quaternion.LookRotation(playerTransform.forward, Vector3.up);
            Quaternion targetRot = baseRot * Quaternion.Euler(cartRotationOffset);

            cartRigidbody.MoveRotation(Quaternion.Slerp(cartRigidbody.rotation, targetRot, Time.fixedDeltaTime * turnSmoothSpeed));
        }

        private void RotateWheels(float speed)
        {
            if (wheels == null || wheels.Length == 0 || speed < 0.05f) return;
            float angle = speed * wheelRotateMultiplier * Time.fixedDeltaTime;
            foreach (var w in wheels)
            {
                if (w != null) w.Rotate(Vector3.right, angle, Space.Self);
            }
        }

        private void OnCollisionEnter(Collision collision) => HandleCollision(collision);
        private void OnCollisionStay(Collision collision) => HandleCollision(collision);

        public void HandleCollision(Collision collision)
        {
            float speed = CurrentVelocity.magnitude;
            if (speed < minKnockdownSpeed) return;

            if (playerTransform != null && (collision.transform == playerTransform || collision.transform.IsChildOf(playerTransform)))
                return;

            NPCControl npc = collision.collider.GetComponentInParent<NPCControl>();
            if (npc != null && !npc.IsDead && !npc.IsKnockedDown)
            {
                int id = npc.GetInstanceID();
                if (hitCooldowns.TryGetValue(id, out float nextTime) && Time.time < nextTime) return;
                hitCooldowns[id] = Time.time + 1.5f;

                Vector3 hitDir = (npc.transform.position - cartRoot.position);
                hitDir.y = 0f;
                if (hitDir.sqrMagnitude < 0.01f) hitDir = cartRoot.forward;
                hitDir.Normalize();

                CitizenCombat combat = npc.GetComponent<CitizenCombat>();
                if (combat != null && collisionDamage > 0 && playerCombat != null)
                {
                    combat.ReceiveKick(playerCombat, hitDir, collisionDamage);
                }
                else
                {
                    npc.KnockDown(hitDir, Mathf.Max(knockdownForce, speed * 1.8f));
                }

                Collider[] cols = Physics.OverlapSphere(cartRoot.position, 12f);
                foreach (var col in cols)
                {
                    var stealthAI = col.GetComponentInParent<StealthCitizenAI>();
                    if (stealthAI != null) stealthAI.HearSound(cartRoot.position, 2);
                }
            }
        }
    }

    public class CartCollisionRelay : MonoBehaviour
    {
        [HideInInspector] public FoodCartController controller;
        private void OnCollisionEnter(Collision c) { if (controller != null) controller.HandleCollision(c); }
        private void OnCollisionStay(Collision c) { if (controller != null) controller.HandleCollision(c); }
    }
}