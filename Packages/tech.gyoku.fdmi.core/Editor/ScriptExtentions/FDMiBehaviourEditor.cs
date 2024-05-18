using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor
{
    [CustomEditor(typeof(FDMiBehaviour), true)]
    public class FDMiBehaviourEditor : FDMiEditorExt
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            serializedObject.Update();
            var tgt = target as Component;
            if (FDMiEditorUI.BigButton("Setup This")) SetupAll(tgt, serializedObject);
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    SetPropertyOption(tgt, property, false);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup = false) { }
        public override void SetupAll(Component tgt, SerializedObject serializedObject)
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                SetPropertyOption(tgt, property, true);
            }
            serializedObject.ApplyModifiedProperties();
        }

    }
}