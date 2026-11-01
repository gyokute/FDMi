using FDMi.core;
using FDMi.core.Editor.Inspector;
using FDMi.input;
using UnityEditor;
using UnityEngine;

namespace FDMi.input.Editor
{
    // 5本の並列配列を1行ずつ編集する。配列長は常に全列で揃える。
    [CustomEditor(typeof(FDMiKeyInput), true)]
    public class FDMiKeyInputEditor : FDMiBehaviourEditor
    {
        const float KeyWidth = 110f;
        const float ModeWidth = 84f;
        const float ValueWidth = 48f;
        const float RemoveWidth = 20f;

        SerializedProperty keys;
        SerializedProperty modes;
        SerializedProperty outputs;
        SerializedProperty onValues;
        SerializedProperty offValues;

        protected override void OnEnable()
        {
            base.OnEnable();
            keys = serializedObject.FindProperty(nameof(FDMiKeyInput.keys));
            modes = serializedObject.FindProperty(nameof(FDMiKeyInput.modes));
            outputs = serializedObject.FindProperty(nameof(FDMiKeyInput.outputs));
            onValues = serializedObject.FindProperty(nameof(FDMiKeyInput.onValues));
            offValues = serializedObject.FindProperty(nameof(FDMiKeyInput.offValues));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Bindings", EditorStyles.boldLabel);
            DrawColumnLabels();

            // 行の途中で配列を縮めるとレイアウトが崩れるので、削除はループの後にまとめて行う。
            int removeIndex = -1;
            for (int i = 0; i < keys.arraySize; i++)
                if (DrawRow(i))
                    removeIndex = i;
            if (removeIndex >= 0)
                RemoveRow(removeIndex);

            if (GUILayout.Button("+"))
                AddRow();

            serializedObject.ApplyModifiedProperties();
        }

        static void DrawColumnLabels()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key", EditorStyles.miniBoldLabel, GUILayout.Width(KeyWidth));
            EditorGUILayout.LabelField("Mode", EditorStyles.miniBoldLabel, GUILayout.Width(ModeWidth));
            EditorGUILayout.LabelField("On", EditorStyles.miniBoldLabel, GUILayout.Width(ValueWidth));
            EditorGUILayout.LabelField("Off", EditorStyles.miniBoldLabel, GUILayout.Width(ValueWidth));
            EditorGUILayout.LabelField("Output", EditorStyles.miniBoldLabel);
            GUILayout.Space(RemoveWidth);
            EditorGUILayout.EndHorizontal();
        }

        bool DrawRow(int i)
        {
            bool remove = false;
            EditorGUILayout.BeginHorizontal();

            SerializedProperty key = keys.GetArrayElementAtIndex(i);
            key.intValue = (int)(KeyCode)EditorGUILayout.EnumPopup((KeyCode)key.intValue, GUILayout.Width(KeyWidth));

            SerializedProperty mode = modes.GetArrayElementAtIndex(i);
            mode.intValue = (int)(FDMiKeyMode)EditorGUILayout.EnumPopup((FDMiKeyMode)mode.intValue, GUILayout.Width(ModeWidth));

            SerializedProperty on = onValues.GetArrayElementAtIndex(i);
            on.floatValue = EditorGUILayout.FloatField(on.floatValue, GUILayout.Width(ValueWidth));

            SerializedProperty off = offValues.GetArrayElementAtIndex(i);
            off.floatValue = EditorGUILayout.FloatField(off.floatValue, GUILayout.Width(ValueWidth));

            SerializedProperty output = outputs.GetArrayElementAtIndex(i);
            output.objectReferenceValue = EditorGUILayout.ObjectField(output.objectReferenceValue, typeof(FDMiData), true);

            if (GUILayout.Button("-", GUILayout.Width(RemoveWidth)))
                remove = true;

            EditorGUILayout.EndHorizontal();
            return remove;
        }

        void AddRow()
        {
            int i = keys.arraySize;
            ResizeAll(i + 1);
            keys.GetArrayElementAtIndex(i).intValue = (int)KeyCode.None;
            modes.GetArrayElementAtIndex(i).intValue = (int)FDMiKeyMode.Momentum;
            onValues.GetArrayElementAtIndex(i).floatValue = 1f;
            offValues.GetArrayElementAtIndex(i).floatValue = 0f;
            outputs.GetArrayElementAtIndex(i).objectReferenceValue = null;
        }

        void RemoveRow(int index)
        {
            int last = keys.arraySize - 1;
            MoveUpToEnd(keys, index, last);
            MoveUpToEnd(modes, index, last);
            MoveUpToEnd(outputs, index, last);
            MoveUpToEnd(onValues, index, last);
            MoveUpToEnd(offValues, index, last);
            ResizeAll(last);
        }

        // Why not DeleteArrayElementAtIndex: object 参照の配列では1回目が null 代入になり、要素が消えない。
        static void MoveUpToEnd(SerializedProperty array, int index, int last)
        {
            for (int i = index; i < last; i++)
                array.MoveArrayElement(i + 1, i);
        }

        void ResizeAll(int size)
        {
            keys.arraySize = size;
            modes.arraySize = size;
            outputs.arraySize = size;
            onValues.arraySize = size;
            offValues.arraySize = size;
        }
    }
}
