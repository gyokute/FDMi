using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiModelTransformFollower : FDMiBehaviour
    {
        public string IsRootPath = "~/IsRootPath";

        [FDMiDataPath(nameof(IsRootPath)), FDMiRegisterCallback(nameof(OnChange))]
        public FDMiBool IsRoot;

        public Transform parentWhenTrue;
        public Transform parentWhenFalse;

        public void OnChange()
        {
            transform.parent = IsRoot.Data ? parentWhenTrue : parentWhenFalse;
            transform.position = Vector3.zero;
        }
    }
}
