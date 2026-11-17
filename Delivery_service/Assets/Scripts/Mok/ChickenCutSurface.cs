using UnityEngine;

namespace DeliveryService.Mok
{
    public static class ChickenCutSurface
    {
        // Coordinates match the cooking UI's 1920 x 1080 reference canvas.
        // Use a screen-sized gesture, independent of the chicken mesh's small silhouette.
        public static bool TryLine(GameObject chicken, Camera camera, out Vector2 start, out Vector2 end)
        {
            start = end = Vector2.zero;
            if (!chicken || !camera) return false;
            Vector3 screen = camera.WorldToScreenPoint(chicken.transform.position);
            if (screen.z <= 0) return false;
            Vector2 centre = new Vector2(screen.x / Screen.width * 1920,
                (1 - screen.y / Screen.height) * 1080);
            // Keep both markers away from screen edges and the instruction panel below.
            centre.x = Mathf.Clamp(centre.x, 560, 1360);
            centre.y = Mathf.Clamp(centre.y, 280, 520);
            float angle = Random.Range(-12f, 12f) * Mathf.Deg2Rad;
            Vector2 halfLine = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 480;
            start = centre - halfLine;
            end = centre + halfLine;
            return true;
        }
    }
}
