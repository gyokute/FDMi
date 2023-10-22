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
    public class FDMiEditorExt : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            serializedObject.Update();
            if (FDMiEditorUI.BigButton("Setup All")) SetupAll();
            var tgt = target as Component;
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

        public virtual void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup = false)
        {
            UnityEngine.Object refevent = null;
            if (property.type == "PPtr<$FDMiEvent>" && (FDMiEditorUI.Button("Find") || forceSetup))
                refevent = FindDataByName<FDMiEvent>(tgt, property.name);
            if (property.type == "PPtr<$FDMiBool>" && (FDMiEditorUI.Button("Find") || forceSetup))
                refevent = FindDataByName<FDMiBool>(tgt, property.name);
            if (property.type == "PPtr<$FDMiFloat>" && (FDMiEditorUI.Button("Find") || forceSetup))
                refevent = FindDataByName<FDMiFloat>(tgt, property.name);
            if (property.type == "PPtr<$FDMiVector3>" && (FDMiEditorUI.Button("Find") || forceSetup))
                refevent = FindDataByName<FDMiVector3>(tgt, property.name);
            if (property.type == "PPtr<$FDMiQuaternion>" && (FDMiEditorUI.Button("Find") || forceSetup))
                refevent = FindDataByName<FDMiQuaternion>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedBool>" && (FDMiEditorUI.Button("Find") || forceSetup))
                refevent = FindDataByName<FDMiSyncedBool>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedFloat>" && (FDMiEditorUI.Button("Find") || forceSetup))
                refevent = FindDataByName<FDMiSyncedFloat>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedVector3>" && (FDMiEditorUI.Button("Find") || forceSetup))
                refevent = FindDataByName<FDMiSyncedVector3>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedQuaternion>" && (FDMiEditorUI.Button("Find") || forceSetup))
                refevent = FindDataByName<FDMiSyncedQuaternion>(tgt, property.name);
            if (refevent) property.objectReferenceValue = refevent;
        }
        public virtual void SetupAll()
        {
            var tgt = target as Component;
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                SetPropertyOption(tgt, property, true);
            }
            serializedObject.ApplyModifiedProperties();
        }

        public T FindDataByName<T>(Component tgt, string name) where T : FDMiEvent
        {
            // Find FDMiEvent in same gameObject.
            var objs = tgt.GetComponents<T>();
            var ret = objs.FirstOrDefault(o => o.name == name);
            if (ret) return ret;
            // If not, find FDMiEvent in children.
            objs = tgt.GetComponentsInChildren<T>();
            ret = objs.FirstOrDefault(o => o.name == name);
            if (ret) return ret;
            // If not, find through each vehicle
            var objMan = tgt.GetComponentInParent<FDMiObjectManager>();
            if (objMan)
            {
                objs = objMan.GetComponentsInChildren<T>();
                ret = objs.FirstOrDefault(o => o.name == name);
                if (ret) return ret;
            }
            // If not, find through scene... and it's maybe heavy!
            objs = FindObjectsOfType<T>();
            ret = objs.FirstOrDefault(o => o.name == name);
            if (ret) return ret;

            return null;
        }
    }
}