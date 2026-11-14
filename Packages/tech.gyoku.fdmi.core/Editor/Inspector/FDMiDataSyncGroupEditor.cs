using FDMi.core;
using FDMi.core.Editor.Application.UseCases;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Inspector
{
    /// <summary>
    /// FDMiDataSyncGroup のチャネル表を編集する。
    /// 行は「パス・幅・量子化・min/max」で、U# の直列化制約に合わせて並列配列へ書き込む。
    /// </summary>
    [CustomEditor(typeof(FDMiDataSyncGroup))]
    public class FDMiDataSyncGroupEditor : UnityEditor.Editor
    {
        static readonly string[] ScaleLabels = { "Raw", "Bool", "Unit", "Signed", "Range", "RawS" };
        static readonly string[] WidthLabels = { "8", "16", "32" };
        static readonly byte[] WidthValues = { 8, 16, 32 };

        SerializedProperty _paths;
        SerializedProperty _widths;
        SerializedProperty _scales;
        SerializedProperty _mins;
        SerializedProperty _maxs;
        SerializedProperty _interval;

        void OnEnable()
        {
            _paths = serializedObject.FindProperty(nameof(FDMiDataSyncGroup.channelPaths));
            _widths = serializedObject.FindProperty(nameof(FDMiDataSyncGroup.channelWidths));
            _scales = serializedObject.FindProperty(nameof(FDMiDataSyncGroup.channelScales));
            _mins = serializedObject.FindProperty(nameof(FDMiDataSyncGroup.channelMins));
            _maxs = serializedObject.FindProperty(nameof(FDMiDataSyncGroup.channelMaxs));
            _interval = serializedObject.FindProperty("updateInterval");

            Resolve(target);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_interval, new GUIContent("Update Interval [s]"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Channels", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "幅の小さい順（Bool → 8 → 16 → 32）に並べると詰め物が減る。幅は量子化規則が決める。",
                EditorStyles.miniLabel
            );

            int removeAt = -1;
            for (int i = 0; i < _paths.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var pathProp = _paths.GetArrayElementAtIndex(i);
                pathProp.stringValue = EditorGUILayout.TextField(pathProp.stringValue);

                DrawWidthAndScale(i);
                DrawRange(i);

                if (GUILayout.Button("−", GUILayout.Width(24f)))
                    removeAt = i;

                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0)
                RemoveChannel(removeAt);

            if (GUILayout.Button("Add Channel"))
                AddChannel();

            var group = (FDMiDataSyncGroup)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                $"channels {group.channels.Length} / words {group.words.Length} / {_paths.arraySize} defs",
                EditorStyles.miniLabel
            );

            for (int i = 0; i < group.channels.Length; i++)
                if (group.channels[i] == null)
                    EditorGUILayout.HelpBox($"channel {i}: 解決できないパス", MessageType.Warning);

            if (serializedObject.ApplyModifiedProperties())
            {
                Resolve(target);
                serializedObject.Update();
            }
        }

        void DrawWidthAndScale(int i)
        {
            int scale = _scales.GetArrayElementAtIndex(i).intValue;
            bool widthLocked = scale == FDMiDataSyncGroup.ScaleBool || scale == FDMiDataSyncGroup.ScaleUnit
                || scale == FDMiDataSyncGroup.ScaleSigned;

            // 幅は ResolveSyncGroupUseCase が量子化規則から確定する（Bool=1, Unit/Signed=8）。
            // Inspector 側で書き換えない。描画中に書くと変更検知と実行順がずれる。
            using (new EditorGUI.DisabledScope(widthLocked))
            {
                int current = _widths.GetArrayElementAtIndex(i).intValue;
                int index = 0;
                for (int w = 0; w < WidthValues.Length; w++)
                    if (WidthValues[w] == current)
                        index = w;
                int next = EditorGUILayout.Popup(index, WidthLabels, GUILayout.Width(48f));
                _widths.GetArrayElementAtIndex(i).intValue = WidthValues[next];
            }

            int sel = EditorGUILayout.Popup(scale, ScaleLabels, GUILayout.Width(72f));
            _scales.GetArrayElementAtIndex(i).intValue = sel;
        }

        void DrawRange(int i)
        {
            if (_scales.GetArrayElementAtIndex(i).intValue != FDMiDataSyncGroup.ScaleRange)
                return;

            var minProp = _mins.GetArrayElementAtIndex(i);
            var maxProp = _maxs.GetArrayElementAtIndex(i);
            minProp.floatValue = EditorGUILayout.FloatField(minProp.floatValue, GUILayout.Width(56f));
            maxProp.floatValue = EditorGUILayout.FloatField(maxProp.floatValue, GUILayout.Width(56f));
        }

        void AddChannel()
        {
            int index = _paths.arraySize;
            _paths.arraySize = index + 1;
            _widths.arraySize = index + 1;
            _scales.arraySize = index + 1;
            _mins.arraySize = index + 1;
            _maxs.arraySize = index + 1;

            _paths.GetArrayElementAtIndex(index).stringValue = string.Empty;
            _widths.GetArrayElementAtIndex(index).intValue = 8;
            _scales.GetArrayElementAtIndex(index).intValue = FDMiDataSyncGroup.ScaleUnit;
            _mins.GetArrayElementAtIndex(index).floatValue = 0f;
            _maxs.GetArrayElementAtIndex(index).floatValue = 1f;
        }

        void RemoveChannel(int index)
        {
            _paths.DeleteArrayElementAtIndex(index);
            _widths.DeleteArrayElementAtIndex(index);
            _scales.DeleteArrayElementAtIndex(index);
            _mins.DeleteArrayElementAtIndex(index);
            _maxs.DeleteArrayElementAtIndex(index);
        }

        static void Resolve(Object target)
        {
            new ResolveSyncGroupUseCase().Execute(target);
            new RegisterCallbacksUseCase().Execute(target);
        }
    }
}
