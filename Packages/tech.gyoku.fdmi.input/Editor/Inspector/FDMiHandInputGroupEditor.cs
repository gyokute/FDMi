using System.Collections.Generic;
using FDMi.core;
using UnityEditor;
using UnityEngine;

namespace FDMi.input.Editor
{
    [CustomEditor(typeof(FDMiHandInputGroup), true)]
    public class FDMiHandInputGroupEditor : UnityEditor.Editor
    {
        BuildHandInputDisplaysUseCase displayUseCase;

        protected virtual void OnEnable()
        {
            var group = (FDMiHandInputGroup)target;
            AssignReferences(group);
            if (displayUseCase == null)
                displayUseCase = new BuildHandInputDisplaysUseCase();
            displayUseCase.Execute(group);
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
                displayUseCase.Execute((FDMiHandInputGroup)target);
            if (GUILayout.Button("Build Hand Input Display"))
                FDMiHandInputDisplayBuilder.Build((FDMiHandInputGroup)target);
        }

        private void AssignReferences(FDMiHandInputGroup group)
        {
            var trackers = new List<FDMiHandTracker>();
            foreach (var root in group.gameObject.scene.GetRootGameObjects())
                trackers.AddRange(root.GetComponentsInChildren<FDMiHandTracker>(true));

            var elements = new List<FDMiHandInputElement>();
            var parentNameSpace = group.GetComponentInParent<FDMiNamespace>();
            if (parentNameSpace)
            {
                var defaultGroup = group as FDMiDefaultInputGroup;
                foreach (var element in parentNameSpace.GetComponentsInChildren<FDMiHandInputElement>(true))
                {
                    // DFUNC は既定 Group (matchingTag 一致) にのみ割り当てる。
                    // 接触 Group に入ると接触中も Pad・Trigger が機能選択に奪われる。
                    if (element is FDMiHandDFUNC dfunc
                        && !(defaultGroup && dfunc.matchingTag == defaultGroup.matchingTag))
                        continue;
                    elements.Add(element);
                }
            }

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
