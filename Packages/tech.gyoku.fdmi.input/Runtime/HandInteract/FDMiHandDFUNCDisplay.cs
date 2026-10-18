using FDMi.core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace FDMi.input
{
    /// <summary>
    /// DFUNC の放射状選択パネル。手に追従し頭部を向く。
    /// 判断はせず、FDMiHandDFUNC の通知で駆動される。
    /// 選択肢ラベル・値源 (選択肢ごとの FDMiData と正規化範囲) は
    /// FDMiHandDFUNCDisplayBuilder (Editor) が焼き、実行時は読み出しのみ。
    /// </summary>
    public class FDMiHandDFUNCDisplay : FDMiBehaviour
    {
        [Tooltip("TrackingData の手位置 (コントローラ本来のグリップ点) から Panel をずらす量 (手のローカル空間)")]
        public Vector3 handOffset = new Vector3(0f, -0.02f, 0.10f);

        // 以下は Builder が焼く参照。実行時に書き換えないため Editor 設定のみで保持する。
        [HideInInspector]
        public FDMiHandTracker tracker;

        [HideInInspector]
        public RectTransform panel;

        // 選択肢ごとの並列配列: ラベル、掴み通知の転送先の有無 (色の初期復帰用)、値源と正規化範囲。
        [HideInInspector]
        public TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[0];

        [HideInInspector]
        public bool[] hasTarget = new bool[0];

        [HideInInspector]
        public FDMiData[] valueDatas = new FDMiData[0];

        [HideInInspector]
        public float[] valueMins = new float[0];

        [HideInInspector]
        public float[] valueMaxs = new float[0];

        [HideInInspector]
        public Color idleColor = new Color(1f, 1f, 1f, 0.6f);

        [HideInInspector]
        public Color selectedColor = new Color(0.55f, 0.85f, 1f, 1f);

        [HideInInspector]
        public Color disabledColor = new Color(1f, 1f, 1f, 0.2f);

        [HideInInspector]
        public GameObject valueLine;

        [HideInInspector]
        public TextMeshProUGUI valueText;

        [HideInInspector]
        public Image barFill;

        private VRCPlayerApi localPlayer;
        private FDMiData sessionData;
        private float sessionMin;
        private float sessionMax;
        private int highlighted = -1;
        private int numCache = int.MinValue;
        private int barCache = int.MinValue;

        private void OnEnable()
        {
            localPlayer = Networking.LocalPlayer;
        }

        public void OnSelectStart(int selected)
        {
            // Follow より先に Active 化する: 非アクティブ起動時も OnEnable が走り
            // localPlayer が初期化された状態で Follow に入るため。
            gameObject.SetActive(true);
            if (selected != highlighted)
                SetSelection(selected);
        }

        public void OnTick()
        {
            Follow();
            if (!sessionData)
                return;
            UpdateValue();
        }

        public void OnSelectEnd()
        {
            gameObject.SetActive(false);
        }

        public void SetSelection(int selected)
        {
            if (highlighted >= 0)
                optionTexts[highlighted].color = hasTarget[highlighted] ? idleColor : disabledColor;
            optionTexts[selected].color = selectedColor;
            highlighted = selected;
        }

        public void SetSessionTarget(int index)
        {
            if (index >= 0)
                sessionData = valueDatas[index];
            else
                sessionData = null;
            // 値源のない選択肢 (target 未設定または値行の焼いていない target) では値行を隠す。
            bool active = sessionData != null;
            if (valueLine)
                valueLine.SetActive(active);
            if (active)
            {
                sessionMin = valueMins[index];
                sessionMax = valueMaxs[index];
                numCache = int.MinValue;
                barCache = int.MinValue;
                valueText.text = "";
            }
        }

        private void UpdateValue()
        {
            // 数値とバーはそれぞれ値を int に量子化して変化検知し、変わった時だけ書き込む。
            float value = sessionData.GetFloat();
            int numX100 = Mathf.RoundToInt(value * 100f);
            if (numX100 != numCache)
            {
                numCache = numX100;
                valueText.text = (numX100 / 100f).ToString("F2");
            }
            int barX200 = Mathf.RoundToInt(Mathf.InverseLerp(sessionMin, sessionMax, value) * 200f);
            if (barX200 != barCache)
            {
                barCache = barX200;
                barFill.transform.localScale = new Vector3(barX200 / 200f, 1f, 1f);
            }
        }

        private void Follow()
        {
            // Hand ボーン (GetBoneTransform) は Udon 未対応。
            // トラッカーのインタラクション計算と同じ TrackingData (コントローラ本来の手位置) を
            // 基準にし、handOffset で手の下に配置する。
            VRCPlayerApi.TrackingData hand = localPlayer.GetTrackingData(tracker.trackingHand);
            panel.position = hand.position + hand.rotation * handOffset;
            panel.rotation = Quaternion.LookRotation(
                localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position
                    - panel.position);
        }
    }
}
