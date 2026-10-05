using System.Collections.Generic;
using FDMi.core;
using UnityEditor;
using UnityEngine;

namespace FDMi.input.Editor
{
    [CustomEditor(typeof(FDMiHandInputGroup), true)]
    public class FDMiHandInputGroupEditor : UnityEditor.Editor
    {
        protected virtual void OnEnable()
        {
            var group = (FDMiHandInputGroup)target;
            AssignReferences(group);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        private void AssignReferences(FDMiHandInputGroup group)
        {
            var trackers = new List<FDMiHandTracker>();
            foreach (var root in group.gameObject.scene.GetRootGameObjects())
                trackers.AddRange(root.GetComponentsInChildren<FDMiHandTracker>(true));

            var elements = new List<FDMiHandInputElement>();
            var parentNameSpace = group.GetComponentInParent<FDMiNamespace>();
            if(parentNameSpace)
                elements.AddRange(parentNameSpace.GetComponentsInChildren<FDMiHandInputElement>(true));

            serializedObject.Update();
            SetArray(serializedObject.FindProperty(nameof(FDMiHandInputGroup.Trackers)), trackers.ToArray());
            SetArray(serializedObject.FindProperty(nameof(FDMiHandInputGroup.InputElements)), elements.ToArray());
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetArray<T>(SerializedProperty property, T[] components)
            where T : Component
        {
            property.arraySize = components.Length;
            for (var i = 0; i < components.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = components[i];
        }
    }
}
