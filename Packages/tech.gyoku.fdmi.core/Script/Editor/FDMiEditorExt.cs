using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
namespace tech.gyoku.FDMi.core.editor
{
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
                    ShowPropertyOption(tgt, property);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        public virtual void ShowPropertyOption(Component tgt, SerializedProperty property)
        {
            if (property.type == "PPtr<$FDMiEvent>" && FDMiEditorUI.Button("Find"))
                property.objectReferenceValue = FindDataByName<FDMiEvent>(tgt, property.name);
            if (property.type == "PPtr<$FDMiFloat>" && FDMiEditorUI.Button("Find"))
                property.objectReferenceValue = FindDataByName<FDMiFloat>(tgt, property.name);
            if (property.type == "PPtr<$FDMiVector3>" && FDMiEditorUI.Button("Find"))
                property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, property.name);
            if (property.type == "PPtr<$FDMiQuaternion>" && FDMiEditorUI.Button("Find"))
                property.objectReferenceValue = FindDataByName<FDMiQuaternion>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedFloat>" && FDMiEditorUI.Button("Find"))
                property.objectReferenceValue = FindDataByName<FDMiSyncedFloat>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedVector3>" && FDMiEditorUI.Button("Find"))
                property.objectReferenceValue = FindDataByName<FDMiSyncedVector3>(tgt, property.name);
            if (property.type == "PPtr<$FDMiSyncedQuaternion>" && FDMiEditorUI.Button("Find"))
                property.objectReferenceValue = FindDataByName<FDMiSyncedQuaternion>(tgt, property.name);
        }

        public virtual void SetupAll()
        {
            var tgt = target as Component;
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                if (property.type == "PPtr<$FDMiEvent>")
                    property.objectReferenceValue = FindDataByName<FDMiEvent>(tgt, property.name);
                if (property.type == "PPtr<$FDMiFloat>")
                    property.objectReferenceValue = FindDataByName<FDMiFloat>(tgt, property.name);
                if (property.type == "PPtr<$FDMiVector3>")
                    property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, property.name);
                if (property.type == "PPtr<$FDMiQuaternion>")
                    property.objectReferenceValue = FindDataByName<FDMiQuaternion>(tgt, property.name);
                if (property.type == "PPtr<$FDMiSyncedFloat>")
                    property.objectReferenceValue = FindDataByName<FDMiSyncedFloat>(tgt, property.name);
                if (property.type == "PPtr<$FDMiSyncedVector3>")
                    property.objectReferenceValue = FindDataByName<FDMiSyncedVector3>(tgt, property.name);
                if (property.type == "PPtr<$FDMiSyncedQuaternion>")
                    property.objectReferenceValue = FindDataByName<FDMiSyncedQuaternion>(tgt, property.name);
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