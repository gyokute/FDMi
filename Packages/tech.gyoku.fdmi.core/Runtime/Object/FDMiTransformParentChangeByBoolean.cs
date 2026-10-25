using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiTransformParentChangeByBoolean : FDMiBehaviour
    {
        public string ValuePath = "~/IsRoot";

        [FDMiDataPath(nameof(ValuePath)), FDMiRegisterCallback(nameof(OnChange))]
        public FDMiBool Value;

        public Transform parentWhenTrue;
        public Transform parentWhenFalse;

        public void OnChange()
        {
            transform.parent = Value.Data ? parentWhenTrue : parentWhenFalse;
        }
    }
}
