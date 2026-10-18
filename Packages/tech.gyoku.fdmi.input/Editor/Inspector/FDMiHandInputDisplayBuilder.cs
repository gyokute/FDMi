using System.Linq;
using FDMi.core;
using FDMi.input;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FDMi.input.Editor
{
    /// <summary>
    /// FDMiHandInputGroup の下にハンドディスプレイ (Canvas / 行 / 参照結線) を構築する。
    /// 実行は冪等で、既存の HandInputDisplay を破棄して作り直す。
    /// 要素ごとのブロック (2 行: Description+Gesture / Label+数値+バー)
    /// を縦に積む。値は複数持つ要素 (FlightControl など) はその数だけ値行を重ねる。
    /// </summary>
    public static class FDMiHandInputDisplayBuilder
    {
        // 1px = CanvasScale [m] ≒ 0.35mm。パネル幅 380px ≒ 13.3cm。
        private const float CanvasScale = 0.00035f;
        private const float PanelWidth = 380f;
        private const float Padding = 10f;

        private const float HeaderHeight = 34f;
        private const float ValueHeight = 28f;
        private const float BlockGap = 6f;

        private const float GestureWidth = 130f;
        private const float ColumnGap = 8f;
        // Gesture は右端から右詰め、Description は左端から左詰め (列の中心は左原点で逆算)。
        private const float DescriptionWidth = PanelWidth - Padding * 2f - GestureWidth - ColumnGap;
        private const float LabelWidth = 22f;
        private const float NumberWidth = 84f;
        private const float BarWidth = PanelWidth - Padding * 2f - LabelWidth - NumberWidth;
        private const float BarHeight = 14f;

        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color FillColor = new Color(0.55f, 0.85f, 1f, 0.85f);

        // 行ごとの内包する x 中心。1 行は幅 PanelWidth の中心原点で、内部にテキスト/バーを絶対配置する。
        // ヘッダ: Description 左 / Gesture 右。値行: Label 左 / Number / Bar 右。左右の端は Padding で揃う。
        private const float DescriptionX = -PanelWidth / 2f + Padding + DescriptionWidth / 2f;
        private const float GestureX = PanelWidth / 2f - Padding - GestureWidth / 2f;
        private const float LabelX = -PanelWidth / 2f + Padding + LabelWidth / 2f;
        private const float NumberX = LabelX + LabelWidth / 2f + NumberWidth / 2f;
        private const float BarX = NumberX + NumberWidth / 2f + BarWidth / 2f;

        /// <summary>
        /// レイアウト構造を変更したら上げる。焼き上がりにこの値を持たせ、旧構造の焼上が放置されるのを防ぐ。
        /// </summary>
        private const int LayoutVersion = 2;

        public static void Build(FDMiHandInputGroup group)
        {
            var content = CollectContentElements(group);
            Rebuild(group, content);
        }

        private static void Rebuild(FDMiHandInputGroup group, FDMiHandInputElement[] content)
        {
            var existing = group.GetComponentInChildren<FDMiHandInputDisplay>(true);
            if (existing)
                Undo.DestroyObjectImmediate(existing.gameObject);

            var display = CreateDisplay(group, content);
            WireGroup(group, display);
        }

        /// <summary>
        /// 焼上が未達、または構造・文言・値源が陳腐化していれば再構築する。
        /// 新鮮な場合は GameObject を一切触らない (選択のたびに走るため、無駄な再構築を防ぐ)。
        /// 表示する要素が無いグループでは残骸があれば掃除するだけにする。
        /// </summary>
        public static void Ensure(FDMiHandInputGroup group)
        {
            // 選択時フックは PlayMode 中にも走る。Play 中の再構築はセッション状態の破壊になるためしない
            // (強制リビルドを行う明示ボタンはここを通らない)。
            if (EditorApplication.isPlaying)
                return;

            var content = CollectContentElements(group);
            var display = FindDisplay(group);
            if (content.Length == 0)
            {
                if (display)
                    Undo.DestroyObjectImmediate(display.gameObject);
                return;
            }
            if (!IsUpToDate(group, content))
                Rebuild(group, content);
        }

        private static FDMiHandInputElement[] CollectContentElements(FDMiHandInputGroup group)
        {
            // Display 自身も要素として InputElements に登録されているため、行の生成対象からは除く。
            // DFUNC の表示は FDMiHandDFUNCDisplay が担うため、こちらも除く。
            return group.InputElements.Where(e => e && e is not FDMiHandInputDisplay && e is not FDMiHandDFUNC).ToArray();
        }

        private static bool IsUpToDate(FDMiHandInputGroup group, FDMiHandInputElement[] content)
        {
            var display = FindDisplay(group);
            if (display == null)
                return false;
            if (display.layoutVersion != LayoutVersion)
                return false;

            var stale =
                display.valueData == null || display.valueMin == null || display.valueMax == null
                || display.valueTexts == null || display.barFills == null
                || display.valueData.Length != display.valueTexts.Length
                || display.valueMin.Length != display.valueTexts.Length
                || display.valueMax.Length != display.valueTexts.Length
                || display.barFills.Length != display.valueTexts.Length;
            if (stale)
                return false;

            int valueLine = 0;
            for (int i = 0; i < content.Length; i++)
            {
                FDMiHandInputElement element = content[i];

                // 選択時に常に表示がすり替わらないよう、焼いた文言が現在値とズレていれば焼直し。
                // 説明は焼きのみ (Runtime での書き換えはしない) なので階層から読んで比較する。
                var descTransform = display.transform.Find("Header_" + i + "/Description");
                if (descTransform == null)
                    return false;
                var desc = descTransform.GetComponent<TextMeshProUGUI>();
                if (desc == null || desc.text != element.description)
                    return false;

                var gestureTransform = display.transform.Find("Header_" + i + "/Gesture");
                if (gestureTransform == null)
                    return false;
                var gesture = gestureTransform.GetComponent<TextMeshProUGUI>();
                if (gesture == null || gesture.text != GetGestureText(element))
                    return false;

                for (int slot = 0; slot < CountValueLines(element); slot++)
                {
                    var (data, min, max, _) = GetValueSource(element, slot);
                    if (display.valueData[valueLine] != data
                        || display.valueMin[valueLine] != min
                        || display.valueMax[valueLine] != max
                        || display.valueTexts[valueLine] == null
                        || display.barFills[valueLine] == null)
                        return false;
                    valueLine++;
                }
            }
            return true;
        }

        private static FDMiHandInputDisplay FindDisplay(FDMiHandInputGroup group)
        {
            foreach (Transform child in group.transform)
                if (child.TryGetComponent<FDMiHandInputDisplay>(out var display))
                    return display;
            return null;
        }

        /// <summary>
        /// 値行 (数値+バー) を「所有する要素」と「その要素内スロット番号」で列挙して焼く。
        /// 構築と陳腐化検知の両方がこの共通定義を使う。
        /// </summary>
        private static int CountValueLines(FDMiHandInputElement element)
        {
            if (element is FDMiHandFlightControl) return 3;
            if (element is FDMiHandPullInput
                || element is FDMiHandRotateInput
                || element is FDMiHandTwistInput
                || element is FDMiHandAxisInput) return 1;
            return 0;
        }

        /// <summary>
        /// 値行の値源 (FDMiData) と正規化範囲、数値行ラベルを決める。
        /// </summary>
        private static (FDMiData data, float min, float max, string label) GetValueSource(
            FDMiHandInputElement element,
            int slot
        )
        {
            if (element is FDMiHandFlightControl flightControl)
            {
                // 出力は Clamp(-1, 1) で対称なので、範囲も固定で -1..1。
                var data = slot == 0 ? flightControl.pitch
                    : slot == 1 ? flightControl.roll
                    : flightControl.yaw;
                return (data, -1f, 1f, slot == 0 ? "P" : slot == 1 ? "R" : "Y");
            }

            // Lever / Axis 系。min/max は protected のため Serialized 経路で読む。
            var so = new SerializedObject(element);
            var output = so.FindProperty("output");
            var min = so.FindProperty("min");
            var max = so.FindProperty("max");
            return (
                output?.objectReferenceValue as FDMiData,
                min != null ? min.floatValue : 0f,
                max != null ? max.floatValue : 1f,
                string.Empty
            );
        }

        private static FDMiHandInputDisplay CreateDisplay(FDMiHandInputGroup group, FDMiHandInputElement[] content)
        {
            GameObject root = new GameObject("HandInputDisplay", typeof(FDMiHandInputDisplay), typeof(Canvas));
            Undo.RegisterCreatedObjectUndo(root, "Build Hand Input Display");
            root.transform.SetParent(group.transform, false);
            root.layer = group.gameObject.layer;

            // 高さ: 余白x2 + Σ(要素ブロック = ヘッダ + 値行×値スロット数 + ブロック間隔)
            float height = Padding * 2f;
            foreach (var element in content)
                height += HeaderHeight + CountValueLines(element) * ValueHeight + BlockGap;

            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(PanelWidth, height);
            rootRect.localScale = Vector3.one * CanvasScale;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CreateBackground(root.transform, height);

            var descriptionTexts = new TextMeshProUGUI[content.Length];
            int totalValueLines = TotalValueLineCount(content);
            var valueData = new FDMiData[totalValueLines];
            var valueMin = new float[totalValueLines];
            var valueMax = new float[totalValueLines];
            var valueTexts = new TextMeshProUGUI[totalValueLines];
            var barFills = new Image[totalValueLines];

            // 上端から下にカーソルを送って行を積む。
            float cursor = height / 2f - Padding;
            int valueLine = 0;
            for (int elementIndex = 0; elementIndex < content.Length; elementIndex++)
            {
                FDMiHandInputElement element = content[elementIndex];

                GameObject header = CreateLine(root.transform, "Header_" + elementIndex, cursor, HeaderHeight);
                cursor -= HeaderHeight;

                // Description は左端から左詰め、操作方法は右端から右詰めで表示する。
                descriptionTexts[elementIndex] = CreateText(header.transform, "Description", element.description,
                    DescriptionWidth, DescriptionX, HeaderHeight, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, 24);
                // ジェスチャー文言は Udon 実行時に型判定できないため、ここ (Editor) で確定させる。
                CreateText(header.transform, "Gesture", GetGestureText(element),
                    GestureWidth, GestureX, HeaderHeight, TextAlignmentOptions.MidlineRight, FontStyles.Bold, 24);

                for (int slot = 0; slot < CountValueLines(element); slot++)
                {
                    GameObject line = CreateLine(root.transform, element.name + "_Value" + slot, cursor, ValueHeight);
                    cursor -= ValueHeight;

                    var (data, min, max, label) = GetValueSource(element, slot);
                    valueData[valueLine] = data;
                    valueMin[valueLine] = min;
                    valueMax[valueLine] = max;
                    CreateText(line.transform, "Label", label,
                        LabelWidth, LabelX, ValueHeight, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, 18);
                    valueTexts[valueLine] = CreateText(line.transform, "Number", "",
                        NumberWidth, NumberX, ValueHeight, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, 20);
                    barFills[valueLine] = CreateBar(line.transform);
                    valueLine++;
                }
                cursor -= BlockGap;
            }

            root.SetActive(false);

            var display = root.GetComponent<FDMiHandInputDisplay>();
            display.layoutVersion = LayoutVersion;
            display.valueData = valueData;
            display.valueMin = valueMin;
            display.valueMax = valueMax;
            display.valueTexts = valueTexts;
            display.barFills = barFills;
            return display;
        }

        private static int TotalValueLineCount(FDMiHandInputElement[] content)
        {
            int total = 0;
            foreach (var element in content)
                total += CountValueLines(element);
            return total;
        }

        private static void WireGroup(FDMiHandInputGroup group, FDMiHandInputDisplay display)
        {
            display.panel = (RectTransform)display.transform;
            EditorUtility.SetDirty(display);

            // Prefab instance でもオーバーライドとして正しく保持されるよう Serialized 経路で結線する。
            var serializedGroup = new SerializedObject(group);
            var elementsProp = serializedGroup.FindProperty(nameof(FDMiHandInputGroup.InputElements));
            bool registered = false;
            for (int i = 0; i < elementsProp.arraySize; i++)
                if (elementsProp.GetArrayElementAtIndex(i).objectReferenceValue == display)
                {
                    registered = true;
                    break;
                }
            if (!registered)
            {
                // 手の要素イベントだけで駆動されるため、要素列への登録が必須。
                elementsProp.arraySize++;
                elementsProp.GetArrayElementAtIndex(elementsProp.arraySize - 1).objectReferenceValue = display;
            }
            serializedGroup.ApplyModifiedProperties();
        }

        private static GameObject CreateLine(Transform parent, string name, float y, float height)
        {
            GameObject line = CreateChild(parent, name, typeof(RectTransform));
            var rect = (RectTransform)line.transform;
            rect.sizeDelta = new Vector2(PanelWidth, height);
            rect.anchoredPosition = new Vector2(0f, y - height / 2f);
            return line;
        }

        private static void CreateBackground(Transform parent, float height)
        {
            GameObject background = CreateChild(parent, "Background", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)background.transform;
            rect.sizeDelta = new Vector2(PanelWidth, height);

            Image image = background.GetComponent<Image>();
            image.color = PanelColor;
            image.raycastTarget = false;
        }

        private static Image CreateBar(Transform parent)
        {
            GameObject track = CreateChild(parent, "Track", typeof(RectTransform), typeof(Image));
            var trackRect = (RectTransform)track.transform;
            trackRect.sizeDelta = new Vector2(BarWidth, BarHeight);
            trackRect.anchoredPosition = new Vector2(BarX, 0f);
            Image trackImage = track.GetComponent<Image>();
            trackImage.color = TrackColor;
            trackImage.raycastTarget = false;

            // Fill は Track の左端に pivot を置いて伸ばす。実行時は localScale.x の変更のみ。
            // Image.fillAmount は Udon 未対応のための回避。
            GameObject fill = CreateChild(track.transform, "Fill", typeof(RectTransform), typeof(Image));
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(BarWidth, BarHeight);
            fillRect.localScale = Vector3.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = FillColor;
            fillImage.raycastTarget = false;
            return fillImage;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string content,
            float width,
            float x,
            float height,
            TextAlignmentOptions alignment,
            FontStyles style,
            int fontSize
        )
        {
            GameObject go = CreateChild(parent, name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, 0f);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            // 折り返しで行数が伸びて行レイアウトを崩さないよう、単一行 + Truncate で確定させる。
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static GameObject CreateChild(Transform parent, string name, params System.Type[] components)
        {
            GameObject go = new GameObject(name, components);
            Undo.RegisterCreatedObjectUndo(go, "Build Hand Input Display");
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        private static string GetGestureText(FDMiHandInputElement element)
        {
            if (element is FDMiHandFlightControl flightControl)
                return string.Join(" + ", new[]
                {
                    flightControl.pitchType.ToString(),
                    flightControl.rollType.ToString(),
                    flightControl.yawType.ToString(),
                }.Distinct());
            if (element is FDMiHandPullInput) return "Pull";
            if (element is FDMiHandRotateInput) return "Rotate";
            if (element is FDMiHandTwistInput) return "Twist";
            if (element is FDMiHandAxisInput axisInput) return GetAxisGesture(axisInput.inputAxisType);

            return string.Empty;
        }

        private static string GetAxisGesture(FDMiHandAxisType axisType)
        {
            switch (axisType)
            {
                case FDMiHandAxisType.Trigger: return "Trigger";
                case FDMiHandAxisType.Grab: return "Grip";
                case FDMiHandAxisType.PadPush: return "Push";
                case FDMiHandAxisType.PadV:
                case FDMiHandAxisType.PadH:
                    return "Thumbstick";
                default:
                    return string.Empty;
            }
        }
    }
}
