
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor
{

    [CustomEditor(typeof(FDMiDataBusTerminal), true)]
    public class FDMiDataBusTerminalEditor : UnityEditor.Editor
    {
        private bool foldOut = true, debugFoldOut = false;
        private SerializedProperty globalName, privateName, data, isGlobal, dataType;
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            globalName = serializedObject.FindProperty("globalName");
            privateName = serializedObject.FindProperty("privateName");
            data = serializedObject.FindProperty("data");
            isGlobal = serializedObject.FindProperty("isGlobal");
            dataType = serializedObject.FindProperty("dataType");
            serializedObject.Update();

            debugFoldOut = EditorGUILayout.Foldout(debugFoldOut, "Debug/デバッグ用");
            if (debugFoldOut) base.OnInspectorGUI();

            if (foldOut) ShowDataList();
            serializedObject.ApplyModifiedProperties();
        }

        private void ShowDataList()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    if (FDMiEditorUI.Button(false, "+")) AddParamator();
                    for (int i = 0; i < globalName.arraySize; i++)
                    {
                        if (FDMiEditorUI.Button(false, "X")) RemoveParamator(i);
                    }
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    for (int i = 0; i < globalName.arraySize; i++)
                    {
                        EditorGUILayout.PropertyField(isGlobal.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.MaxWidth(20));
                    }
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("global Name", GUILayout.Width(90));
                    EditorGUILayout.LabelField("バス共有 変数名", GUILayout.Width(90));
                    for (int i = 0; i < globalName.arraySize; i++)
                    {
                        EditorGUILayout.PropertyField(globalName.GetArrayElementAtIndex(i), GUIContent.none);
                    }
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    for (int i = 0; i < globalName.arraySize; i++)
                    {
                        EditorGUILayout.LabelField("<-", GUILayout.Width(20));
                    }
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("local Name", GUILayout.Width(100));
                    EditorGUILayout.LabelField("ターミナル下変数名", GUILayout.Width(100));
                    for (int i = 0; i < globalName.arraySize; i++)
                    {
                        EditorGUILayout.PropertyField(privateName.GetArrayElementAtIndex(i), GUIContent.none);
                    }
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    for (int i = 0; i < globalName.arraySize; i++)
                    {
                        EditorGUILayout.PropertyField(dataType.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(120));
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void AddParamator()
        {
            int newArrSize = globalName.arraySize + 1;
            foreach (SerializedProperty sp in new SerializedProperty[] { globalName, privateName, data, isGlobal, dataType })
                sp.arraySize = newArrSize;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
        private void RemoveParamator(int i)
        {
            foreach (SerializedProperty sp in new SerializedProperty[] { globalName, privateName, data, isGlobal, dataType })
            {
                sp.DeleteArrayElementAtIndex(i);
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}