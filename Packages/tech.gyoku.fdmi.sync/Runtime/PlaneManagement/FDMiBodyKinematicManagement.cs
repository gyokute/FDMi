using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    // 機体の Rigidbody を、ローカルプレイヤーが「root かつパイロット」のときだけ動かす。
    // 姿勢の権威 (所有権) と選出は FDMiPilotAuthority が担う。ここは物理とアンカーのゲートだけ。
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
        [Tooltip("ローカルプレイヤーがパイロットか。同期しないローカルフラグ")]
        public FDMiBool IsPilot;

        [Tooltip("キネマティックを切り替える機体の Rigidbody")]
        public Rigidbody body;

        [Tooltip("パイロットゲートを立てる機体の同期アンカー")]
        public FDMiDynamicObjectSync dynamicAnchor;

        void Start()
        {
            // 機体アンカーは所有者かつパイロットのときだけ姿勢を送る (§9.2)。
            if (dynamicAnchor != null)
                dynamicAnchor._pilotGated = true;
            OnChange();
        }

        public void OnChange()
        {
            bool isPilot = IsPilot.Data;
            body.isKinematic = !(IsRoot.Data && isPilot);
            if (dynamicAnchor != null)
                dynamicAnchor._isLocalPilot = isPilot;
        }
    }
}
