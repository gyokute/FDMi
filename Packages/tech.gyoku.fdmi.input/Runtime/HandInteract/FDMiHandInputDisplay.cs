using FDMi.core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace FDMi.input
{
    /// <summary>
    /// 手元に「今できる操作」と「実行中のジェスチャーの値」を表示する FDMiHandInputElement。
    /// InputElements に自動登録され、Group から要素イベントが流れてくるだけで駆動される
    /// (Group 側の呼び出し経路・Update は持たない)。
    /// 表示内容はすべて FDMiHandInputDisplayBuilder (Editor) が焼く。Runtime は値の読み出しのみ。
    /// </summary>
    public class FDMiHandInputDisplay : FDMiHandInputElement
    {
        [Tooltip("TrackingData の手位置 (コントローラ本来のグリップ点) から Panel をずらす量 (手のローカル空間)")]
        public Vector3 handOffset = new Vector3(0f, 0.06f, 0.03f);

        // 以下は Builder が焼く参照。実行時に書き換えないため Editor 設定のみで保持する。
        [HideInInspector]
        public RectTransform panel;

        // Builder が焼いたレイアウト構造のバージョン。実行時には参照しない (Editor の陳腐化検知用)。
        [HideInInspector]
        public int layoutVersion;

        // 値行ごとの並列配列: 値源 (FDMiData) と正規化範囲、数値テキスト、バー Fill。
        [HideInInspector]
        public FDMiData[] valueData = new FDMiData[0];

        [HideInInspector]
        public float[] valueMin = new float[0];

        [HideInInspector]
        public float[] valueMax = new float[0];

        [HideInInspector]
        public TextMeshProUGUI[] valueTexts = new TextMeshProUGUI[0];

        [HideInInspector]
        public Image[] barFills = new Image[0];

        private VRCPlayerApi localPlayer;
        private int[] numCache;
        private int[] barCache;

        private void OnEnable()
        {
            localPlayer = Networking.LocalPlayer;
        }

        public override void OnSelectStart(FDMiHandInputGroup group)
        {
            Activate();
            Follow(group);
        }

        public override void OnGrabStart(FDMiHandInputGroup group)
        {
            Activate();
            Follow(group);
        }

        public override void WhileSelect(FDMiHandInputGroup group)
        {
            Follow(group);
        }

        public override void WhileGrab(FDMiHandInputGroup group)
        {
            Follow(group);
            UpdateValues();
        }

        public override void OnSelectEnd()
        {
            Deactivate();
        }

        public override void OnGrabEnd()
        {
            Deactivate();
        }

        private void Activate()
        {
            // Follow より先に Active 化する: 非アクティブ起動時も OnEnable が走り
            // localPlayer が初期化された状態で Follow に入るため。
            gameObject.SetActive(true);
            if (numCache == null || numCache.Length != valueData.Length)
            {
                numCache = new int[valueData.Length];
                barCache = new int[valueData.Length];
            }
            for (int i = 0; i < numCache.Length; i++)
            {
                numCache[i] = int.MinValue;
                barCache[i] = int.MinValue;
            }
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private void UpdateValues()
        {
            // 数値とバーはそれぞれ値を int に量子化して変化検知し、変わった時だけ書き込む。
            for (int i = 0; i < valueData.Length; i++)
            {
                float value = valueData[i].GetFloat();

                int numX100 = Mathf.RoundToInt(value * 100f);
                if (numX100 != numCache[i])
                {
                    numCache[i] = numX100;
                    valueTexts[i].text = (numX100 / 100f).ToString("F2");
                }

                int barX200 = Mathf.RoundToInt(Mathf.InverseLerp(valueMin[i], valueMax[i], value) * 200f);
                if (barX200 != barCache[i])
                {
                    barCache[i] = barX200;
                    barFills[i].transform.localScale = new Vector3(barX200 / 200f, 1f, 1f);
                }
            }
        }

        private void Follow(FDMiHandInputGroup group)
        {
            FDMiHandTracker tracker = FindInteractingTracker(group);
            // Hand ボーン (GetBoneTransform) は Udon 未対応。
            // トラッカーのインタラクション計算と同じ TrackingData (コントローラ本来の手位置) を
            // 基準にし、handOffset で手の上に配置する。
            VRCPlayerApi.TrackingData hand = localPlayer.GetTrackingData(tracker.trackingHand);
            panel.position = hand.position + hand.rotation * handOffset;
            panel.rotation = Quaternion.LookRotation(
                localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position
                    - panel.position);
        }

        // グループに触れているトラッカーを解決する。Grab 中は確定、選択中は contact 状態で判定する。
        private static FDMiHandTracker FindInteractingTracker(FDMiHandInputGroup group)
        {
            FDMiHandTracker[] trackers = group.Trackers;
            for (int i = 0; i < trackers.Length; i++)
            {
                FDMiHandTracker tracker = trackers[i];
                if (tracker && (tracker.grabingGroup == group || tracker.contactedGroup == group))
                    return tracker;
            }
            return null;
        }
    }
}
