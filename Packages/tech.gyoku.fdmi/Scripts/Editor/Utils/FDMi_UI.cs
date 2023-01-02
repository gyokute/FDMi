using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Saccflight_FDMi.Editor
{
    public static class FDMi_UI
    {
        public static bool Button(string label)
        {
            return GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
        }
        public static bool BigButton(string label)
        {
            return GUILayout.Button(label, new[] { GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true) });
        }
        public static void PropertyField(SerializedObject so, string str)
        {
            EditorGUILayout.PropertyField(so.FindProperty(str), true);
        }
        public static void HeaderlessPropertyField(SerializedObject so, string str)
        {
            EditorGUILayout.PropertyField(so.FindProperty(str), GUIContent.none, true);
        }
    }
}