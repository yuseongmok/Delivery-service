using UnityEngine;

namespace DeliveryService.Mok
{
    public sealed class HelicopterCombatProjectile : MonoBehaviour
    {
        public bool isMissile;
        public bool fromPlayer;
        public float speed = 130f;
        public float damage = 12f;
        public float lifeTime = 4f;
        public float missileTurnRate = 48f;  
        public float hitRadius = 3.2f;

        private Transform target;
        private float spawnTime;

        public void Launch(Vector3 direction, bool byPlayer, bool missile, float dmg, float spd, Transform homingTarget = null)
        {
            fromPlayer = byPlayer;
            isMissile = missile;
            damage = dmg;
            speed = spd;
            target = homingTarget;
            spawnTime = Time.time;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized);
            }
        }

        private void Update()
        {
            if (Time.time - spawnTime >= lifeTime)
            {
                Destroy(gameObject);
                return;
            }

            // 미사일은 초반 1.8초 동안만 제한된 각속도로 유도되어 플레이어가 회피 기동으로 따돌릴 수 있음
            if (isMissile && target != null && (Time.time - spawnTime) < 2.2f)
            {
                Vector3 toTarget = (target.position - transform.position).normalized;
                Quaternion desiredRot = Quaternion.LookRotation(toTarget);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRot, missileTurnRate * Time.deltaTime);
            }

            float step = speed * Time.deltaTime;
            Vector3 nextPos = transform.position + transform.forward * step;

            if (Physics.SphereCast(transform.position, isMissile ? 1.2f : 0.45f, transform.forward, out RaycastHit hit, step + 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (TryApplyHit(hit.collider))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            // 콜라이더가 작거나 빠진 프로토타입 모델링을 위한 거리 기반 보조 명중 판정
            if (fromPlayer && EnemyHelicopterAI.Current != null && !EnemyHelicopterAI.Current.IsDead)
            {
                if (Vector3.Distance(nextPos, EnemyHelicopterAI.Current.transform.position) <= hitRadius)
                {
                    EnemyHelicopterAI.Current.TakeDamage(damage, transform.position);
                    Destroy(gameObject);
                    return;
                }
            }
            else if (!fromPlayer && PlayerHelicopterController.Current != null && PlayerHelicopterController.Current.IsPiloting)
            {
                float pRadius = isMissile ? 4.2f : 3.2f;
                if (Vector3.Distance(nextPos, PlayerHelicopterController.Current.transform.position) <= pRadius)
                {
                    PlayerHelicopterController.Current.TakeDamage(damage, isMissile);
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position = nextPos;
        }

        private bool TryApplyHit(Collider col)
        {
            if (fromPlayer)
            {
                var enemy = col.GetComponentInParent<EnemyHelicopterAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, transform.position);
                    return true;
                }
                if (col.GetComponentInParent<PlayerHelicopterController>() != null) return false;
            }
            else
            {
                var playerHeli = col.GetComponentInParent<PlayerHelicopterController>();
                if (playerHeli != null)
                {
                    playerHeli.TakeDamage(damage, isMissile);
                    return true;
                }
                if (col.GetComponentInParent<EnemyHelicopterAI>() != null) return false;
            }

            return !col.isTrigger;
        }

        public static HelicopterCombatProjectile SpawnFallback(Vector3 pos, Vector3 dir, bool byPlayer, bool missile, float dmg, float spd, Transform homingTarget = null)
        {
            GameObject go = GameObject.CreatePrimitive(missile ? PrimitiveType.Capsule : PrimitiveType.Cube);
            go.name = missile ? "Heli_Missile" : "Heli_Tracer";
            Destroy(go.GetComponent<Collider>());

            go.transform.position = pos;
            go.transform.localScale = missile ? new Vector3(0.45f, 0.45f, 1.8f) : new Vector3(0.14f, 0.14f, 2.6f);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Shader sh = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material mat = new Material(sh);
                mat.color = missile ? new Color(1f, 0.25f, 0.15f) : (byPlayer ? new Color(1f, 0.9f, 0.25f) : new Color(1f, 0.45f, 0.1f));
                mr.sharedMaterial = mat;
            }

            var proj = go.AddComponent<HelicopterCombatProjectile>();
            proj.Launch(dir, byPlayer, missile, dmg, spd, homingTarget);
            return proj;
        }
    }
}