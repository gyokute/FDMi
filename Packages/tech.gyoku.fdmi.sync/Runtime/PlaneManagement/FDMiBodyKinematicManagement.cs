using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // 機体の Rigidbody を、ローカルプレイヤーが「root かつパイロット」のときだけ動かす。
    // 姿勢の権威 (所有権) と選出、アンカーのパイロットフラグは FDMiPilotAuthority が担う。
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

        void Start()
        {
            OnChange();
        }

        public void OnChange()
        {
            body.isKinematic = !(IsRoot.Data && IsPilot.Data);
        }
    }
}
