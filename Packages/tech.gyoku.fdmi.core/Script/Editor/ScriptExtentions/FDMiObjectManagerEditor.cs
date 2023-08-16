using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor
{
    [CustomEditor(typeof(FDMiObjectManager), true)]
    public class FDMiObjectManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var tgt = target as FDMiObjectManager;
            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    if (property.name == nameof(tgt.attributes))
                        if (FDMiEditorUI.Button("Find"))
                            FDMiEditorUI.SetObjectArrayProperty(property, FDMiEditorUI.FindChildrenComponents<FDMiAttribute>(tgt));
                    if (property.name == nameof(tgt.data))
                        if (FDMiEditorUI.Button("Find"))
                            FDMiEditorUI.SetObjectArrayProperty(property, FDMiEditorUI.FindChildrenComponents<FDMiDataBus>(tgt));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}