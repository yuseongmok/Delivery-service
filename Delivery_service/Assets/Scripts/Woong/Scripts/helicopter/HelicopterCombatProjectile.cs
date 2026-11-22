using UnityEngine;

namespace DeliveryService.Mok
{
    public sealed class HelicopterCombatProjectile : MonoBehaviour
    {
        public bool isMissile;
        public bool fromPlayer;
        public float speed = 150f;
        public float damage = 14f;
        public float lifeTime = 5.5f;
        public float missileTurnRate = 52f;
        public float hitRadius = 3.2f;

        private Transform target;
        private float spawnTime;
        private float bulletHomingRate;
        private Vector3 initialArcDir;
        private float arcDuration;

        public void Launch(
            Vector3 direction,
            bool byPlayer,
            bool missile,
            float dmg,
            float spd,
            Transform homingTarget = null,
            float homingRate = 0f,
            Vector3 arcDir = default,
            float arcTime = 0f)
        {
            fromPlayer = byPlayer;
            isMissile = missile;
            damage = dmg;
            speed = spd;
            target = homingTarget;
            bulletHomingRate = homingRate;
            initialArcDir = arcDir.sqrMagnitude > 0.001f ? arcDir.normalized : Vector3.zero;
            arcDuration = arcTime;
            spawnTime = Time.time;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized);
            }
        }

        private void Update()
        {
            float elapsed = Time.time - spawnTime;
            if (elapsed >= lifeTime)
            {
                Destroy(gameObject);
                return;
            }

            if (isMissile)
            {
                if (elapsed < arcDuration && initialArcDir.sqrMagnitude > 0.001f)
                {
                    Quaternion arcRot = Quaternion.LookRotation(initialArcDir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, arcRot, 140f * Time.deltaTime);
                }
                else if (target != null && elapsed < arcDuration + 2.6f)
                {
                    Vector3 toTarget = (target.position - transform.position).normalized;
                    Quaternion desiredRot = Quaternion.LookRotation(toTarget);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRot, missileTurnRate * Time.deltaTime);
                }
            }
            else if (fromPlayer && bulletHomingRate > 0f && target != null && elapsed < 1.6f)
            {
                Vector3 toTarget = (target.position - transform.position).normalized;
                Quaternion desiredRot = Quaternion.LookRotation(toTarget);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRot, bulletHomingRate * Time.deltaTime);
            }

            float step = speed * Time.deltaTime;
            Vector3 nextPos = transform.position + transform.forward * step;

            if (Physics.SphereCast(transform.position, isMissile ? 1.3f : 0.65f, transform.forward, out RaycastHit hit, step + 0.6f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (TryApplyHit(hit.collider))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (fromPlayer && EnemyHelicopterAI.Current != null && !EnemyHelicopterAI.Current.IsDead)
            {
                if (Vector3.Distance(nextPos, EnemyHelicopterAI.Current.transform.position) <= hitRadius)
                {
                    EnemyHelicopterAI.Current.TakeDamage(damage, transform.position);
                    PlayerHelicopterController.Current?.NotifyHitConfirmed();
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
                    PlayerHelicopterController.Current?.NotifyHitConfirmed();
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

        public static HelicopterCombatProjectile SpawnFallback(
            Vector3 pos,
            Vector3 dir,
            bool byPlayer,
            bool missile,
            float dmg,
            float spd,
            Transform homingTarget = null,
            float homingRate = 0f,
            Vector3 arcDir = default,
            float arcTime = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(missile ? PrimitiveType.Capsule : PrimitiveType.Cube);
            go.name = missile ? "Heli_Missile" : "Heli_Tracer";
            Destroy(go.GetComponent<Collider>());

            go.transform.position = pos;
            go.transform.localScale = missile ? new Vector3(0.55f, 0.55f, 2.2f) : new Vector3(0.16f, 0.16f, 3.0f);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Shader sh = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material mat = new Material(sh);
                mat.color = missile ? new Color(1f, 0.22f, 0.12f) : (byPlayer ? new Color(1f, 0.92f, 0.25f) : new Color(1f, 0.48f, 0.1f));
                mr.sharedMaterial = mat;
            }

            if (missile)
            {
                var trail = go.AddComponent<TrailRenderer>();
                trail.time = 0.75f;
                trail.startWidth = 0.55f;
                trail.endWidth = 0.05f;
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                trail.material = new Material(sh);
                trail.startColor = new Color(1f, 0.45f, 0.15f, 0.9f);
                trail.endColor = new Color(0.9f, 0.9f, 0.9f, 0f);
            }

            var proj = go.AddComponent<HelicopterCombatProjectile>();
            proj.Launch(dir, byPlayer, missile, dmg, spd, homingTarget, homingRate, arcDir, arcTime);
            return proj;
        }
    }
}