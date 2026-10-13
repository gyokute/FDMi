using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 胴体抗力の設計値から等価抗力面積を焼き付ける (NAL-SP-00012 胴体式)。
    // 旧 aerodynamics の DragBodyEditor と同式。実行時は掛け算のみ。
    public static class FDMiDragBodyBake
    {
        public static void Bake(FDMiDragBody dragBody)
        {
            float d = Mathf.Max(dragBody.d, 0.01f);
            float l = Mathf.Max(dragBody.l, 0.01f);
            dragBody.cdA = 0.5f * (dragBody.cdp + dragBody.lambda * l / d) * Mathf.PI * d * d / 4f;
        }
    }
}
