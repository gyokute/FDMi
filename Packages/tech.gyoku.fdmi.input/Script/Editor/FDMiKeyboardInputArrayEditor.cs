
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
using tech.gyoku.FDMi.core.editor;
using tech.gyoku.FDMi.input;

namespace tech.gyoku.FDMi.input.editor
{
    [CustomEditor(typeof(FDMiKeyboardInputArray), true)]
    public class FDMiKeyboardInputArrayEditor : UnityEditor.Editor
    {
        private bool foldOut = true, debugFoldOut = false;
        private SerializedProperty floatVal, key, type, initial, multiplier, min, max;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var animDriver = target as FDMiAnimationDriverArray;
            floatVal = serializedObject.FindProperty("floatVal");
            key = serializedObject.FindProperty("key");
            type = serializedObject.FindProperty("type");
            initial = serializedObject.FindProperty("initial");
            multiplier = serializedObject.FindProperty("multiplier");
            min = serializedObject.FindProperty("min");
            max = serializedObject.FindProperty("max");
            serializedObject.Update();
            debugFoldOut = EditorGUILayout.Foldout(debugFoldOut, "入力後データ");
            if (debugFoldOut) base.OnInspectorGUI();

            using (new EditorGUILayout.HorizontalScope())
            {
                foldOut = EditorGUILayout.Foldout(foldOut, "Paramator Manupulate / 動作させるアニメータ");
                if (FDMiEditorUI.Button(false, "Add Paramator")) AddParamator();
            }
            if (foldOut) for (int i = 0; i < key.arraySize; i++) animatorSetup(i);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (FDMiEditorUI.BigButton("Add Paramator")) AddParamator();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void animatorSetup(int i)
        {
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            GUIStyle oneLetter = new GUIStyle();
            oneLetter.stretchWidth = false;
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.backgroundColor = defaultColor;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (FDMiEditorUI.Button(false, "X")) RemoveParamator(i);
                }
                EditorGUILayout.PropertyField(type.GetArrayElementAtIndex(i), new GUIContent("type"), true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("FDMiFloat", GUILayout.Width(70));
                        EditorGUILayout.PropertyField(floatVal.GetArrayElementAtIndex(i), GUIContent.none, true);
                    }
                    EditorGUILayout.LabelField("=", GUILayout.Width(10));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("mul", GUILayout.Width(70));
                        EditorGUILayout.PropertyField(multiplier.GetArrayElementAtIndex(i), GUIContent.none, true);
                    }
                    EditorGUILayout.LabelField("*", GUILayout.Width(10));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("key", GUILayout.Width(70));
                        KeyCode k = (KeyCode)EditorGUILayout.EnumPopup((KeyCode)key.GetArrayElementAtIndex(i).intValue);
                        key.GetArrayElementAtIndex(i).intValue = (int)k;
                    }
                    // EditorGUILayout.LabelField("+", GUILayout.Width(10));
                }
                EditorGUILayout.PropertyField(min.GetArrayElementAtIndex(i), new GUIContent("min"), true);
                EditorGUILayout.PropertyField(max.GetArrayElementAtIndex(i), new GUIContent("max"), true);

            }
            serializedObject.ApplyModifiedProperties();
        }

        private void AddParamator()
        {
            int newArrSize = key.arraySize + 1;
            foreach (SerializedProperty sp in new SerializedProperty[] { floatVal, key, type, initial, multiplier, min, max })
                sp.arraySize = newArrSize;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
        private void RemoveParamator(int i)
        {
            foreach (SerializedProperty sp in new SerializedProperty[] { floatVal, key, type, initial, multiplier, min, max })
            {
                if (sp.GetArrayElementAtIndex(i).objectReferenceValue != null)
                    sp.DeleteArrayElementAtIndex(i);
                sp.DeleteArrayElementAtIndex(i);
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        public static void SetParamator<T>(SerializedProperty property, T target) where T : UnityEngine.Object
        {
            property.objectReferenceValue = target;
        }

    }
}