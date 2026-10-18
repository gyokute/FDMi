using FDMi.core;
using FDMi.input;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FDMi.input.Editor
{
    /// <summary>
    /// FDMiHandDFUNC の下に放射状の選択パネル (Canvas / 選択肢ラベル / 値行 / 参照結線) を構築する。
    /// 実行は冪等で、既存の DFUNCDisplay を破棄して作り直す。
    /// ラベル文字・薄色表示は生成時に確定させ、実行時はハイライトの色差し替えのみを行う。
    /// </summary>
    public static class FDMiHandDFUNCDisplayBuilder
    {
        // 1px = CanvasScale [m] ≒ 0.35mm。パネル 340px ≒ 11.9cm 四方。
        private const float CanvasScale = 0.00035f;
        private const float PanelSize = 340f;
        private const float Radius = 115f;
        private const int FontSize = 22;

        private const float OptionWidth = 110f;
        private const float OptionHeight = 28f;
        private const float ValueWidth = 170f;
        private const float ValueHeight = 28f;
        private const float NumberWidth = 60f;
        private const float BarWidth = ValueWidth - NumberWidth;

        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color FillColor = new Color(0.55f, 0.85f, 1f, 0.85f);

        public static void Build(FDMiHandDFUNC dfunc)
        {
            var existing = dfunc.GetComponentInChildren<FDMiHandDFUNCDisplay>(true);
            if (existing)
                Undo.DestroyObjectImmediate(existing.gameObject);

            var display = CreateDisplay(dfunc);
            Wire(dfunc, display);
        }

        private static FDMiHandDFUNCDisplay CreateDisplay(FDMiHandDFUNC dfunc)
        {
            GameObject root = new GameObject("DFUNCDisplay", typeof(FDMiHandDFUNCDisplay), typeof(Canvas));
            Undo.RegisterCreatedObjectUndo(root, "Build DFUNC Display");
            root.transform.SetParent(dfunc.transform, false);
            root.layer = dfunc.gameObject.layer;

            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(PanelSize, PanelSize);
            rootRect.localScale = Vector3.one * CanvasScale;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CreateBackground(root.transform);

            var display = root.GetComponent<FDMiHandDFUNCDisplay>();

            // targets を基準にする (labels のみ書かれている場合はその数で空きスロットとして並べる)。
            int count = Mathf.Max(dfunc.targets.Length, dfunc.labels.Length);
            var optionTexts = new TextMeshProUGUI[count];
            var hasTarget = new bool[count];
            var valueDatas = new FDMiData[count];
            var valueMins = new float[count];
            var valueMaxs = new float[count];
            for (int i = 0; i < count; i++)
            {
                // 正面上 (option 0) を基準に時計回りへ配置 (実行時の選択計算と同じ配置)。
                float angle = (90f - i * 360f / count) * Mathf.Deg2Rad;
                Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Radius;
                hasTarget[i] = i < dfunc.targets.Length && dfunc.targets[i];
                optionTexts[i] = CreateText(root.transform, "Option_" + i,
                    i < dfunc.labels.Length ? dfunc.labels[i] : string.Empty,
                    position, OptionWidth, OptionHeight, TextAlignmentOptions.Center);
                optionTexts[i].color = hasTarget[i] ? display.idleColor : display.disabledColor;

                // 値源は target 要素の output (FDMiData) と min/max を焼く。
                // output のない target は値行を出さない (valueDatas[i] = null)。
                if (hasTarget[i])
                {
                    var so = new SerializedObject(dfunc.targets[i]);
                    valueDatas[i] = so.FindProperty("output")?.objectReferenceValue as FDMiData;
                    var min = so.FindProperty("min");
                    var max = so.FindProperty("max");
                    valueMins[i] = min != null ? min.floatValue : 0f;
                    valueMaxs[i] = max != null ? max.floatValue : 1f;
                }
            }

            TextMeshProUGUI valueText;
            Image barFill;
            display.valueLine = CreateValueLine(root.transform, out valueText, out barFill);
            display.valueText = valueText;
            display.barFill = barFill;

            root.SetActive(false);

            display.panel = rootRect;
            display.tracker = FindTracker(dfunc.matchingTag);
            display.optionTexts = optionTexts;
            display.hasTarget = hasTarget;
            display.valueDatas = valueDatas;
            display.valueMins = valueMins;
            display.valueMaxs = valueMaxs;
            return display;
        }

        private static GameObject CreateValueLine(Transform parent, out TextMeshProUGUI valueText, out Image barFill)
        {
            GameObject line = new GameObject("ValueLine", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(line, "Build DFUNC Display");
            line.transform.SetParent(parent, false);
            line.layer = parent.gameObject.layer;
            var lineRect = (RectTransform)line.transform;
            lineRect.sizeDelta = new Vector2(ValueWidth, ValueHeight);
            lineRect.anchoredPosition = Vector2.zero;

            GameObject track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(track, "Build DFUNC Display");
            track.transform.SetParent(line.transform, false);
            track.layer = line.layer;
            var trackRect = (RectTransform)track.transform;
            trackRect.sizeDelta = new Vector2(BarWidth, ValueHeight);
            trackRect.anchoredPosition = new Vector2(-ValueWidth / 2f + NumberWidth + BarWidth / 2f, 0f);
            Image trackImage = track.GetComponent<Image>();
            trackImage.color = TrackColor;
            trackImage.raycastTarget = false;

            // Fill は Track の左端に pivot を置いて伸ばす。実行時は localScale.x の変更のみ。
            // Image.fillAmount は Udon 未対応のための回避。
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(fill, "Build DFUNC Display");
            fill.transform.SetParent(track.transform, false);
            fill.layer = track.layer;
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(BarWidth, ValueHeight);
            fillRect.localScale = Vector3.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = FillColor;
            fillImage.raycastTarget = false;
            barFill = fillImage;

            valueText = CreateText(line.transform, "Number", string.Empty,
                new Vector2(-ValueWidth / 2f + NumberWidth / 2f, 0f), NumberWidth, ValueHeight, TextAlignmentOptions.MidlineRight);

            line.SetActive(false);
            return line;
        }

        private static void CreateBackground(Transform parent)
        {
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(background, "Build DFUNC Display");
            background.transform.SetParent(parent, false);
            background.layer = parent.gameObject.layer;
            var rect = (RectTransform)background.transform;
            rect.sizeDelta = new Vector2(PanelSize, PanelSize);
            Image image = background.GetComponent<Image>();
            image.color = PanelColor;
            image.raycastTarget = false;
        }

        private static void Wire(FDMiHandDFUNC dfunc, FDMiHandDFUNCDisplay display)
        {
            // Prefab instance でもオーバーライドとして正しく保持されるよう Serialized 経路で結線する。
            var serializedDfunc = new SerializedObject(dfunc);
            serializedDfunc
                .FindProperty(nameof(FDMiHandDFUNC.display))
                .objectReferenceValue = display;
            serializedDfunc.ApplyModifiedProperties();
            EditorUtility.SetDirty(display);
        }

        private static FDMiHandTracker FindTracker(string matchingTag)
        {
            foreach (var tracker in Object.FindObjectsByType<FDMiHandTracker>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tracker.contactTag == matchingTag)
                    return tracker;
            }
            return null;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string name, string content, Vector2 position,
            float width, float height, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "Build DFUNC Display");
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = position;

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = FontSize;
            text.alignment = alignment;
            text.raycastTarget = false;
            // 折り返しでレイアウトを崩さないよう、単一行 + Truncate で確定させる。
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }
    }
}
