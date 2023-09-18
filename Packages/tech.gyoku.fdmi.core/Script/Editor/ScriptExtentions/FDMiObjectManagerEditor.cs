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
    public class FDMiObjectManagerEditor : FDMiEditorExt
    {
        public override void ShowPropertyOption(Component tgt, SerializedProperty property)
        {
            base.ShowPropertyOption(tgt, property);
            if (property.name == nameof(FDMiObjectManager.body))
                if (FDMiEditorUI.Button("Find"))
                    property.objectReferenceValue = tgt.GetComponentsInChildren<Rigidbody>().OrderByDescending(s => s.mass).FirstOrDefault();

            if (property.name == nameof(FDMiObjectManager.attributes))
                if (FDMiEditorUI.Button("Find"))
                    FDMiEditorUI.SetObjectArrayProperty(property, FDMiEditorUI.FindChildrenComponents<FDMiAttribute>(tgt));
        }

        public override void SetupAll()
        {
            base.SetupAll();
            serializedObject.Update();
            var tgt = target as FDMiObjectManager;
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                if (property.name == nameof(tgt.body))
                    property.objectReferenceValue = tgt.GetComponentsInChildren<Rigidbody>().OrderByDescending(s => s.mass).FirstOrDefault();
                if (property.name == nameof(tgt.attributes))
                    FDMiEditorUI.SetObjectArrayProperty(property, FDMiEditorUI.FindChildrenComponents<FDMiAttribute>(tgt));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}