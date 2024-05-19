
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

    [CustomEditor(typeof(FDMiDataAdapter), true)]
    public class FDMiDataAdapterEditor : UnityEditor.Editor
    {
        private bool foldOut = true, debugFoldOut = false;
        private SerializedProperty primaryBusName, primaryDN, replicaDN;
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            primaryBusName = serializedObject.FindProperty("primaryBusName");
            primaryDN = serializedObject.FindProperty("primaryDN");
            replicaDN = serializedObject.FindProperty("replicaDN");
            serializedObject.Update();

            debugFoldOut = EditorGUILayout.Foldout(debugFoldOut, "Debug/デバッグ用");
            if (debugFoldOut) base.OnInspectorGUI();
            GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("", GUILayout.Width(20));
                EditorGUILayout.LabelField("primary /プライマリ", GUILayout.Width(150));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("", GUILayout.Width(20));
                EditorGUILayout.PropertyField(primaryBusName, GUIContent.none, GUILayout.Width(150));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                foldOut = EditorGUILayout.Foldout(foldOut, "adapter setting/アダプター読替設定");
            }
            if (foldOut) ShowDataAdapterList();
            serializedObject.ApplyModifiedProperties();
        }

        private void ShowDataAdapterList()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (FDMiEditorUI.Button(false, "+")) AddParamator();
                        EditorGUILayout.LabelField("primary DN/プライマリ識別名", GUILayout.Width(150));
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("replica DN/レプリカ識別名", GUILayout.Width(150));
                    }
                    for (int i = 0; i < primaryDN.arraySize; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (FDMiEditorUI.Button(false, "X")) RemoveParamator(i);
                            EditorGUILayout.PropertyField(primaryDN.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(150));
                            EditorGUILayout.LabelField("->", GUILayout.Width(20));
                            EditorGUILayout.PropertyField(replicaDN.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(150));
                        }

                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void AddParamator()
        {
            int newArrSize = primaryDN.arraySize + 1;
            foreach (SerializedProperty sp in new SerializedProperty[] { primaryDN, replicaDN })
                sp.arraySize = newArrSize;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
        private void RemoveParamator(int i)
        {
            foreach (SerializedProperty sp in new SerializedProperty[] { primaryDN, replicaDN })
            {
                sp.DeleteArrayElementAtIndex(i);
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}