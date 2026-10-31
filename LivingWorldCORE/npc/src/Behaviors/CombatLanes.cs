using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// A2.3/A2.4 (3e5ac3c) lane terrain check. READ-ONLY: physics queries only; it never moves anything.
    /// A lane is clear when its end is standable ground (no steep slope, no drop/climb over 1.2 m, not under water) and a
    /// straight line at chest height meets no obstacle (characters ignored: the lane planner judges those).
    /// </summary>
    public static class CombatLanes
    {
        const float MaxStep = 1.2f;
        static readonly RaycastHit[] hits = new RaycastHit[8];

        public static bool Clear(Vector3 from, P2 direction, float length)
        {
            Vector3 dir = new Vector3(direction.X, 0f, direction.Z);
            if (dir.sqrMagnitude < .01f || length < .05f) return false;
            dir.Normalize();
            Vector3 end = from + dir * length;
            float ground;
            if (!Ground(end, from.y, out ground) || Mathf.Abs(ground - from.y) > MaxStep) return false;
            if (ZoneSystem.instance != null && ground < ZoneSystem.instance.m_waterLevel - .3f) return false;
            int count = Physics.RaycastNonAlloc(from + Vector3.up * .9f, dir, hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (hits[i].collider != null && hits[i].collider.GetComponentInParent<Character>() == null) return false;
            return true;
        }

        static bool Ground(Vector3 point, float fromHeight, out float height)
        {
            height = fromHeight;
            Vector3 top = new Vector3(point.x, fromHeight + 2f, point.z);
            int count = Physics.RaycastNonAlloc(top, Vector3.down, hits, 4.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            float closest = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.collider.GetComponentInParent<Character>() != null || hit.normal.y < .6f || hit.distance >= closest) continue;
                closest = hit.distance;
                height = hit.point.y;
                found = true;
            }
            return found;
        }
    }
}
