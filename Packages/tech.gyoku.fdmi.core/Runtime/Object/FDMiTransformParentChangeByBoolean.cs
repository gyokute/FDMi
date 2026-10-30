using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiTransformParentChangeByBoolean : FDMiBehaviour
    {
        [Tooltip("入力データの解決パス。名前空間の区切りは /")]
        public string ValuePath = "~/IsRoot";

        [FDMiDataPath(nameof(ValuePath)), FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("親を切り替える入力")]
        public FDMiBool Value;

        [Tooltip("入力が true のときの親")]
        public Transform parentWhenTrue;

        [Tooltip("入力が false のときの親")]
        public Transform parentWhenFalse;

        public void OnChange()
        {
            transform.parent = Value.Data ? parentWhenTrue : parentWhenFalse;
        }
    }
}
