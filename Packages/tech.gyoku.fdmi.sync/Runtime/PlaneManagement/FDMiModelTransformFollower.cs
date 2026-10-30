using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiModelTransformFollower : FDMiBehaviour
    {
        [Tooltip("root 判定の解決パス")]
        public string IsRootPath = "~/IsRoot";

        [FDMiDataPath(nameof(IsRootPath)), FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("この機体が root か")]
        public FDMiBool IsRoot;

        [Tooltip("root のときの親")]
        public Transform parentWhenTrue;

        [Tooltip("root でないときの親")]
        public Transform parentWhenFalse;

        public void OnChange()
        {
            if (IsRoot.Data)
            {
                transform.parent = parentWhenTrue;
                transform.position = Vector3.zero;
            }
            else
            {
                transform.parent = parentWhenFalse;
                transform.localPosition = Vector3.zero;
            }
        }
    }
}
