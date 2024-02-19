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
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiAttribute.objectManager) && (FDMiEditorUI.Button(forceSetup, "Find") || forceSetup))
            {
                FDMiObjectManager man = FDMiEditorUI.FindParentComponent<FDMiObjectManager>(tgt);
                if (man) property.objectReferenceValue = man;
            }

            if (property.name == nameof(FDMiAttribute.body) && (FDMiEditorUI.Button(forceSetup, "Find") || forceSetup))
            {
                FDMiObjectManager man = FDMiEditorUI.FindParentComponent<FDMiObjectManager>(tgt);
                if(!man) return;
                Rigidbody b = man.GetComponentInChildren<Rigidbody>();
                if (b) property.objectReferenceValue = b;
            }
        }
    }
}