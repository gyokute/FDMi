using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    [DefaultExecutionOrder(1000)]
    public class FDMiObjectActivateByBoolean : FDMiBehaviour
    {
        [Tooltip("入力データの解決パス。名前空間の区切りは /")]
        public string ValuePath = "~/InZone";

        [FDMiDataPath(nameof(ValuePath)), FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("表示を切り替える入力")]
        public FDMiBool Value;

        [Tooltip("有効化を反転する。true で偽のとき表示")]
        public bool onWhenDisable;

        private void Start()
        {
            OnChange();
        }

        public void OnChange()
        {
            gameObject.SetActive(Value.Data ^ onWhenDisable);
        }
    }
}
