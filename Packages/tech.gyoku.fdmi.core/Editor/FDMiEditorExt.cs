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

        public virtual void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup = false)
        {
            UnityEngine.Object refevent = null;
            if (property.type == "PPtr<$FDMiEvent>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiEvent>(tgt, property.name);
            if (property.type == "PPtr<$FDMiBool>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiBool>(tgt, property.name);
            if (property.type == "PPtr<$FDMiInt>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiInt>(tgt, property.name);
            if (property.type == "PPtr<$FDMiFloat>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiFloat>(tgt, property.name);
            if (property.type == "PPtr<$FDMiVector3>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiVector3>(tgt, property.name);
            if (property.type == "PPtr<$FDMiQuaternion>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiQuaternion>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedBool>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiSyncedBool>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedFloat>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiSyncedFloat>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedVector3>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiSyncedVector3>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedQuaternion>" && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                refevent = FindDataByName<FDMiSyncedQuaternion>(tgt, property.name);
            if (refevent) property.objectReferenceValue = refevent;
        }
        public virtual void SetupAll(Component tgt, SerializedObject serializedObject)
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

        public T FindDataByName<T>(Component tgt, string name) where T : FDMiEvent
        {
            // Find FDMiEvent in same gameObject.
            var objs = tgt.GetComponents<T>();
            var ret = objs.FirstOrDefault(o => o.VariableName == name);
            if (ret) return ret;
            // If not, find FDMiEvent in children.
            objs = tgt.GetComponentsInChildren<T>();
            ret = objs.FirstOrDefault(o => o.VariableName == name);
            if (ret) return ret;
            // with underbar, find in children
            if (name.Contains("_"))
            {
                ret = objs.FirstOrDefault(o => o.VariableName.Contains(name.Substring(1)));
                if (ret) return ret;
            }
            // If not, find FDMiEvent from parent.
            objs = tgt.transform.parent.GetComponents<T>();
            ret = objs.FirstOrDefault(o => o.VariableName == name);
            if (ret) return ret;
            // If not, find through each vehicle
            var objMan = tgt.GetComponentInParent<FDMiObjectManager>();
            if (objMan)
            {
                objs = objMan.GetComponentsInChildren<T>();
                ret = objs.FirstOrDefault(o => o.VariableName == name);
                if (ret) return ret;
            }
            // If not, find through scene... and it's maybe heavy!
            objs = FindObjectsOfType<T>();
            ret = objs.FirstOrDefault(o => o.VariableName == name);
            if (ret) return ret;

            return null;
        }
    }
}