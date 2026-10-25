using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiModelTransformFollower : FDMiBehaviour
    {
        public string IsRootPath = "~/IsRoot";

        [FDMiDataPath(nameof(IsRootPath)), FDMiRegisterCallback(nameof(OnChange))]
        public FDMiBool IsRoot;

        public Transform parentWhenTrue;
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
