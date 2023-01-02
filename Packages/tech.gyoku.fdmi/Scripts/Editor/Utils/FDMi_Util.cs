using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;


namespace Saccflight_FDMi.Editor
{
    public static class FDMi_Util
    {
        // Some functions from esnyaSFAddons
        // https://github.com/esnya/EsnyaSFAddons/tree/beta
        public static void SetObjectArrayProperty<T>(SerializedProperty property, IEnumerable<T> enumerable) where T : UnityEngine.Object
        {
            var array = enumerable.ToArray();
            property.arraySize = array.Length;

            for (var i = 0; i < array.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
            }
        }

        public static IEnumerable<T> FindChildrenComponents<T>(UdonSharpBehaviour from)
        {
            return from.GetComponentsInChildren<T>();
        }

        public static void SetParamator<T>(SerializedProperty property, T target) where T : UnityEngine.Object
        {
            property.objectReferenceValue = target;
        }

        public static T FindParentComponent<T>(UdonSharpBehaviour from)
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