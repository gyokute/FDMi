using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor
{
    [CustomEditor(typeof(FDMiDataBus), true)]
    public class FDMiDataBusEditor : UnityEditor.Editor
    {
        private bool foldOut = true, debugFoldOut = false;
        private SerializedProperty indexName, data;
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var tgt = target as FDMiDataBus;
            serializedObject.Update();

            indexName = serializedObject.FindProperty(nameof(tgt.indexName));
            data = serializedObject.FindProperty(nameof(tgt.data));

            debugFoldOut = EditorGUILayout.Foldout(debugFoldOut, "入力後データ");
            if (debugFoldOut) base.OnInspectorGUI();

            FDMiEditorUI.PropertyField(serializedObject, nameof(tgt.busName));
            using (new EditorGUILayout.HorizontalScope())
            {
                foldOut = EditorGUILayout.Foldout(foldOut, "Paramators");
                if (FDMiEditorUI.Button("Add Paramator")) AddParamator();
            }
            if (foldOut) for (int i = 0; i < data.arraySize; i++) setup(i);

            serializedObject.ApplyModifiedProperties();
        }

        private void setup(int i)
        {
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.black;
            GUIStyle oneLetter = new GUIStyle();
            oneLetter.stretchWidth = false;
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.backgroundColor = defaultColor;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (FDMiEditorUI.Button("X")) RemoveParamator(i);
                    EditorGUILayout.LabelField("index:", GUILayout.Width(35));
                    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(50));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("name:", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(indexName.GetArrayElementAtIndex(i), GUIContent.none, true);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("value:", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(data.GetArrayElementAtIndex(i), GUIContent.none, true);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void AddParamator()
        {
            int newArrSize = data.arraySize + 1;
            foreach (SerializedProperty sp in new SerializedProperty[] { indexName, data })
                sp.arraySize = newArrSize;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
        private void RemoveParamator(int i)
        {
            foreach (SerializedProperty sp in new SerializedProperty[] { indexName, data })
                sp.DeleteArrayElementAtIndex(i);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        public static void SetParamator<T>(SerializedProperty property, T target) where T : UnityEngine.Object
        {
            property.objectReferenceValue = target;
        }
    }
}

