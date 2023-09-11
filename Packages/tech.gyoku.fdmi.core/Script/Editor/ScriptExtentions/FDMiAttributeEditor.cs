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
    [CustomEditor(typeof(FDMiAttribute), true)]
    public class FDMiAttributeEditor : FDMiEditorExt
    {
        public override void ShowPropertyOption(Component tgt, SerializedProperty property)
        {
            base.ShowPropertyOption(tgt, property);
            if (property.name == nameof(FDMiAttribute.objectManager))
            {
                if (FDMiEditorUI.Button("Find"))
                    property.objectReferenceValue = FDMiEditorUI.FindParentComponent<FDMiObjectManager>(tgt);
            }
        }

        public override void SetupAll()
        {
            base.SetupAll();
            serializedObject.Update();
            var tgt = target as FDMiAttribute;
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                if (property.name == nameof(tgt.objectManager))
                    property.objectReferenceValue = FDMiEditorUI.FindParentComponent<FDMiObjectManager>(tgt);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}