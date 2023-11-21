using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace tech.gyoku.FDMi.core.editor
{
    public static class FDMiEditorUI
    {
        public static bool Button(bool forceSetup, string label)
        {
            if (forceSetup) return true;
            else return GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
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


        public static void SetObjectArrayProperty<T>(SerializedProperty property, IEnumerable<T> enumerable) where T : UnityEngine.Object
        {
            var array = enumerable.ToArray();
            property.arraySize = array.Length;

            for (var i = 0; i < array.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
            }
        }

        public static IEnumerable<T> FindChildrenComponents<T>(Component from)
        {
            return from.GetComponentsInChildren<T>();
        }

        public static void SetParamator<T>(SerializedProperty property, T target) where T : UnityEngine.Object
        {
            property.objectReferenceValue = target;
        }

        public static T FindParentComponent<T>(Component from)
        {
            return from.GetComponentInParent<T>();
        }
        public static void DrawProperty(SerializedObject so, string str)
        {
            var property = so.FindProperty(str);
            EditorGUILayout.PropertyField(property, true);
        }
        public static SerializedProperty GetProperty<T>(SerializedObject so, string str)
        {
            return so.FindProperty(str);
        }

    }
}