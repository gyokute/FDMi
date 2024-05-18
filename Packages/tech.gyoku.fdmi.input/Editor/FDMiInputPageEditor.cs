
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using tech.gyoku.FDMi.core;
using tech.gyoku.FDMi.core.editor;
using tech.gyoku.FDMi.input;

namespace tech.gyoku.FDMi.input.editor
{
    [CustomEditor(typeof(FDMiInputPage), true)]
    public class FDMiInputPageEditor : FDMiBehaviourEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiInputPage.inputManager) && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
            {
                FDMiObjectManager man = tgt.GetComponentInParent<FDMiObjectManager>();
                property.objectReferenceValue = man.GetComponentInChildren<FDMiInputManager>();
            }
            if (property.name == nameof(FDMiInputPage.InputAddons) && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                FDMiEditorUI.SetObjectArrayProperty<FDMiInputAddon>(property, FDMiEditorUI.FindChildrenComponents<FDMiInputAddon>(tgt.transform.parent));
        }
    }
}