using UnityEngine;
namespace DeliveryService.Mok
{
    public sealed class ThrownBrick : MonoBehaviour, IInteractable
    {
        public WeaponKind kind = WeaponKind.Brick; public PlayerWeapon owner; bool hit; float born;

        public float noiseRadius = 18f;
        public int noisePriority = 2; 
        private bool hasMadeNoise = false;

        void Awake() { born = Time.time; gameObject.layer = 13; }

        void OnCollisionEnter(Collision c)
        {
            
            bool isPlayerCollision = (owner != null && c.transform.IsChildOf(owner.transform)) || c.transform.CompareTag("Player") || c.transform.root.CompareTag("Player");
            if (isPlayerCollision) return;
            if (!hasMadeNoise && c.relativeVelocity.magnitude > 1.5f)
            {
                hasMadeNoise = true;
                Vector3 impactPos = c.contacts.Length > 0 ? c.contacts[0].point : transform.position;

                Collider[] colliders = Physics.OverlapSphere(impactPos, noiseRadius);
                foreach (Collider col in colliders)
                {
                    StealthCitizenAI npc = col.GetComponentInParent<StealthCitizenAI>();
                    if (npc != null) npc.HearSound(impactPos, noisePriority);
                }
            }

            if (hit) return;
            hit = true;
            if (owner != null) owner.Hit(c.collider, c.relativeVelocity.normalized, kind == WeaponKind.Brick ? 50 : WeaponCatalog.Damage[(int)kind]);
        }

        public bool CanRecover(GameObject player) => owner == null || (owner.gameObject == player && owner.Thrown == this && Time.time > born + .4f);

        public void Interact(ToppingInventory inventory)
        {
            if (inventory == null) return;

            if (owner != null && CanRecover(inventory.gameObject))
            {
                owner.Recover(this);
            }
            else if (owner == null)
            {
                PlayerWeapon pw = inventory.GetComponent<PlayerWeapon>();
                if (pw != null)
                {
                    pw.Equip(kind);
                    Destroy(gameObject);
                }
            }
        }

        void OnGUI()
        {
            if (owner == null || owner.Thrown != this || owner.Combat == null || !owner.Combat.CanAct || Time.time < born + .4f) return;
            var cam = owner.Combat.View; if (cam == null) return; var point = cam.WorldToViewportPoint(transform.position + Vector3.up * .28f);
            if (point.z <= 0) return;
            float x = Mathf.Clamp(point.x * Screen.width, 100, Screen.width - 100), y = Mathf.Clamp((1 - point.y) * Screen.height, 55, Screen.height - 140);
            var style = new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = Mathf.Max(16, Screen.height / 48), alignment = TextAnchor.MiddleCenter }; style.normal.textColor = new Color(1, .72f, .3f);
            GUI.Label(new Rect(x - 110, y - 25, 220, 62), $"▼ {WeaponCatalog.Names[(int)kind]}\n{Vector3.Distance(cam.transform.position, transform.position):0}m", style);
            var input = owner.GetComponent<PlayerInteraction>(); var hover = owner.GetComponentInChildren<InteractHoverManager>();
            if (input != null && (hover == null || hover.interactText == null) && InteractionFocus.Find(input) == transform) { style.normal.textColor = Color.white; GUI.Label(new Rect(0, Screen.height - 125, Screen.width, 40), "F를 눌러 줍기", style); }
        }

        void Update()
        {
            if (transform.position.y < -50)
            {
                if (owner != null) { transform.position = owner.transform.position + Vector3.up; GetComponent<Rigidbody>().linearVelocity = Vector3.zero; }
                else Destroy(gameObject);
            }
        }
    }
}