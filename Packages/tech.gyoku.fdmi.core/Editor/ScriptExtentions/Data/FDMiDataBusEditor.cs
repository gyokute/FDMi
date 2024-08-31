
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

    [CustomEditor(typeof(FDMiDataBus), true)]
    public class FDMiDataBusEditor : FDMiEditorExt
    {
        private bool foldOut = true, debugFoldOut = false;
        private SerializedProperty dn, data, busName;

        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            // do nothing.
        }
        public override void SetupAll(Component tgt, SerializedObject serializedObject)
        {
            // do nothing.
        }
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            dn = serializedObject.FindProperty("dn");
            data = serializedObject.FindProperty("data");
            busName = serializedObject.FindProperty("dataBusName");
            serializedObject.Update();

            debugFoldOut = EditorGUILayout.Foldout(debugFoldOut, "Debug/デバッグ用");
            if (debugFoldOut) base.OnInspectorGUI();

            using (new GUILayout.VerticalScope())
            {
                EditorGUILayout.PropertyField(busName, new GUIContent("databus Identify Name"));
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("referenced by FDMiDataAdapter to connect dataBus. \nIf this is in vehicle, don't need to put.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("シーン内で一意なdataBusの名前.FDMiDataAdapterがdataBus間の接続に使用する。\n※各機体に一意な名前をつける、まではしなくて良い.", EditorStyles.wordWrappedLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.LabelField("Data List/データ一覧");
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Edit prohibited. Please fix automatic by using \"FDMi/Setup FDMi Components\".", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("編集不可. \"FDMi/Setup FDMi Components\" を用い自動設定せよ.", EditorStyles.wordWrappedLabel);

            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Identify Name/識別名", GUILayout.Width(140));
                    for (int i = 0; i < dn.arraySize; i++)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.PropertyField(dn.GetArrayElementAtIndex(i), GUIContent.none);
                        EditorGUI.EndDisabledGroup();
                    }
                }
                using (new GUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Data/データ本体", GUILayout.Width(100));
                    for (int i = 0; i < data.arraySize; i++)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.PropertyField(data.GetArrayElementAtIndex(i), GUIContent.none);
                        EditorGUI.EndDisabledGroup();
                    }
                }
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}