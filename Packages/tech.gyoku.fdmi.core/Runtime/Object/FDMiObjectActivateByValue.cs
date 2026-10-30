using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    [DefaultExecutionOrder(1000)]
    public class FDMiObjectActivateByValue : FDMiBehaviour
    {
        [Tooltip("入力データの解決パス。名前空間の区切りは /")]
        public string ValuePath = "~/InZone";

        [FDMiDataPath(nameof(ValuePath)), FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("表示を切り替える入力")]
        public FDMiData Value;

        [Tooltip("この値を超えたら表示する")]
        public float threshold = 0.5f;

        [Tooltip("有効化を反転する。true で閾値以下のとき表示")]
        public bool onWhenDisable;

        public void OnChange()
        {
            gameObject.SetActive((Value.GetFloat() > threshold) ^ onWhenDisable);
        }
    }
}
