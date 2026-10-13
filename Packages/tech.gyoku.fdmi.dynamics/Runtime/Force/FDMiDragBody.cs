using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 胴体・ナセル等の寄生抗力。機体前方 (+Z 基準) に -Drag 方向の力を加える。
    // 旧 aerodynamics からの移植 (NAL-SP-00012 胴体式)。
    // cdA はEditorの焼き付けが設計値 (d, l, cdp, lambda) から解く。実行時は掛け算のみ。
    // TAS は |v| を入れること (旧版は前方成分だった)。
    public class FDMiDragBody : FDMiBehaviour
    {
        [Header("Wiring")]
        public Rigidbody body;
        public FDMiFloat Rho; // [kg/m3]
        public FDMiFloat TAS; // |v| [m/s]

        [Header("Design (NAL-SP-00012 fuselage)")]
        public float d = 4f; // 胴体直径 [m]
        public float l = 40f; // 胴体長さ [m]
        public float cdp = 0.075f; // 圧力抗力係数 (形状依存)
        public float lambda = 0.016f; // 摩擦項係数

        [Header("Bake results (Editor が設定。触らない)")]
        public float cdA = 1f; // 等価抗力面積 [m2]

        [Header("Telemetry")]
        public float Drag; // 抗力 [N]

        float[] _rho, _tas;

        void Start()
        {
            _rho = Rho.data;
            _tas = TAS.data;
        }

        void FixedUpdate()
        {
            if (body == null)
                return;
            float v = _tas[0];
            Drag = 0.5f * _rho[0] * cdA * v * v;
            body.AddRelativeForce(Drag * Vector3.back);
        }
    }
}
