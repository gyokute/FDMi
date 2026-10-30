using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiBodyKinematicManagement : FDMiBehaviour
    {
        [Tooltip("root 判定の解決パス")]
        public string IsRootPath = "~/IsRoot";

        [FDMiDataPath(nameof(IsRootPath)), FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("この機体が root か")]
        public FDMiBool IsRoot;

        [Tooltip("パイロット判定の解決パス")]
        public string IsPilotPath = "~/IsPilot";

        [FDMiDataPath(nameof(IsPilotPath)), FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("ローカルプレイヤーがパイロットか")]
        public FDMiBool IsPilot;

        [Tooltip("キネマティックを切り替える機体の Rigidbody")]
        public Rigidbody body;

        [Tooltip("パイロット時に所有権を移す機体内のコンポーネント。Editor が収集する")]
        public FDMiBehaviour[] inPlaneBehaviours;

        public void OnChange()
        {
            body.isKinematic = !(IsRoot.Data && IsPilot.Data);

            if (!(IsRoot.Data && IsPilot.Data))
                return;

            VRCPlayerApi player = Networking.LocalPlayer;
            foreach (var i in inPlaneBehaviours)
            {
                // Udon は UdonSharpBehaviour の裏の UdonBehaviour を配列に書くため、
                // 裏付けを持たない (未 Setup の) コンポーネントは null として現れる。
                if (!i)
                    continue;
                Networking.SetOwner(player, i.gameObject);
            }
        }
    }
}
