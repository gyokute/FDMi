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
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiObjectManager.body))
                if ((FDMiEditorUI.Button(forceSetup, "Find")|| forceSetup))
                    property.objectReferenceValue = tgt.GetComponentsInChildren<Rigidbody>().OrderByDescending(s => s.mass).FirstOrDefault();

            if (property.name == nameof(FDMiObjectManager.attributes))
                if ((FDMiEditorUI.Button(forceSetup, "Find")|| forceSetup))
                    FDMiEditorUI.SetObjectArrayProperty(property, FDMiEditorUI.FindChildrenComponents<FDMiAttribute>(tgt));
        }

    }
}