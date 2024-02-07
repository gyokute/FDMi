
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
    [CustomEditor(typeof(FDMiTransformRotationDriverArray), true)]
    public class FDMiTransformRotationDriverArrayEditor : UnityEditor.Editor
    {
        private bool foldOut = true, debugFoldOut = false;
        private SerializedProperty Input, multiplier, rotateTransform, rotateAxis, repeatValue;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var animDriver = target as FDMiAnimationDriverArray;
            Input = serializedObject.FindProperty("Input");
            multiplier = serializedObject.FindProperty("multiplier");
            rotateTransform = serializedObject.FindProperty("rotateTransform");
            rotateAxis = serializedObject.FindProperty("rotateAxis");
            repeatValue = serializedObject.FindProperty("repeatValue");
            serializedObject.Update();
            debugFoldOut = EditorGUILayout.Foldout(debugFoldOut, "入力後データ");
            if (debugFoldOut) base.OnInspectorGUI();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateMode"), true);
            using (new EditorGUILayout.HorizontalScope())
            {
                foldOut = EditorGUILayout.Foldout(foldOut, "Manupulate Transform / 回転させるTransform");
                if (FDMiEditorUI.Button(false, "Add Transform")) Addmultiplier();
            }
            if (foldOut) for (int i = 0; i < multiplier.arraySize; i++) animatorSetup(i);

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
                    if (FDMiEditorUI.Button(false, "X")) Removemultiplier(i);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("Transform", GUILayout.Width(70));
                        EditorGUILayout.PropertyField(rotateTransform.GetArrayElementAtIndex(i), GUIContent.none, true);
                    }
                    EditorGUILayout.LabelField("=", GUILayout.Width(10));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("Curve", GUILayout.Width(50));
                        multiplier.GetArrayElementAtIndex(i).animationCurveValue = EditorGUILayout.CurveField(GUIContent.none, multiplier.GetArrayElementAtIndex(i).animationCurveValue, GUILayout.MaxWidth(70));
                    }
                    EditorGUILayout.LabelField("*", GUILayout.Width(10));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("FDMiFloat", GUILayout.Width(70));
                        EditorGUILayout.PropertyField(Input.GetArrayElementAtIndex(i), GUIContent.none, true);
                    }
                    // EditorGUILayout.LabelField("+", GUILayout.Width(10));
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            EditorGUILayout.LabelField("axis", GUILayout.Width(70));
                            EditorGUILayout.PropertyField(rotateAxis.GetArrayElementAtIndex(i), GUIContent.none, true);
                        }
                        using (new EditorGUILayout.VerticalScope())
                        {
                            EditorGUILayout.LabelField("repeat Value", GUILayout.Width(70));
                            EditorGUILayout.PropertyField(repeatValue.GetArrayElementAtIndex(i), GUIContent.none, true);
                        }
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void Addmultiplier()
        {
            int newArrSize = multiplier.arraySize + 1;
            foreach (SerializedProperty sp in new SerializedProperty[] { Input, multiplier, rotateTransform, rotateAxis, repeatValue })
                sp.arraySize = newArrSize;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
        private void Removemultiplier(int i)
        {
            foreach (SerializedProperty sp in new SerializedProperty[] { Input, multiplier, rotateTransform, rotateAxis, repeatValue })
                sp.DeleteArrayElementAtIndex(i);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        public static void Setmultiplier<T>(SerializedProperty property, T target) where T : UnityEngine.Object
        {
            property.objectReferenceValue = target;
        }

    }
}