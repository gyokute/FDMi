using UnityEditor;
using UnityEngine;

namespace FDMi.input.Editor
{
    // 手の移動量 / 回転量を Scene ビューに描く。Lever と Yoke で共有する。
    public static class FDMiHandPreviewDraw
    {
        public static void DrawTravel(
            Vector3 origin,
            Vector3 axis,
            bool pull,
            float travel,
            Color color,
            string label
        )
        {
            if (Mathf.Abs(travel) < 1e-4f || axis.sqrMagnitude < 1e-8f)
                return;

            Vector3 normal = axis.normalized;
            Handles.color = color;

            if (pull)
            {
                Vector3 end = origin + normal * travel;
                Handles.DrawLine(origin, end);

                Vector3 direction = travel >= 0f ? normal : -normal;
                Vector3 side = Vector3.Cross(direction, Vector3.up);
                if (side.sqrMagnitude < 1e-4f)
                    side = Vector3.Cross(direction, Vector3.right);
                side.Normalize();

                float head = Mathf.Max(HandleUtility.GetHandleSize(end) * 0.05f, 0.02f);
                Handles.DrawLine(end, end - direction * head + side * head * 0.5f);
                Handles.DrawLine(end, end - direction * head - side * head * 0.5f);
                Handles.Label(end, label);
                return;
            }

            float radius = Mathf.Max(HandleUtility.GetHandleSize(origin) * 0.2f, 0.05f);
            Vector3 from = Perpendicular(normal);
            Handles.DrawWireArc(origin, normal, from, travel, radius);
            Vector3 endPoint = origin + Quaternion.AngleAxis(travel, normal) * from * radius;
            Handles.Label(endPoint, label);
        }

        static Vector3 Perpendicular(Vector3 normal)
        {
            Vector3 candidate = Vector3.ProjectOnPlane(Vector3.right, normal);
            if (candidate.sqrMagnitude < 1e-4f)
                candidate = Vector3.ProjectOnPlane(Vector3.up, normal);
            if (candidate.sqrMagnitude < 1e-4f)
                candidate = Vector3.ProjectOnPlane(Vector3.forward, normal);
            return candidate.normalized;
        }
    }
}
