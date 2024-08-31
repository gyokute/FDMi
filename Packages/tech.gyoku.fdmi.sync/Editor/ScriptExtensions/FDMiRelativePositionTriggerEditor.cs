
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
using tech.gyoku.FDMi.sync;

namespace tech.gyoku.FDMi.sync.editor
{
    [CustomEditor(typeof(FDMiRelativePositionTrigger), true)]
    public class FDMiRelativePositionTriggerEditor : FDMiSyncAttributeEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiRelativePositionTrigger.refPoint))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    FDMiObjectManager man = tgt.GetComponentInParent<FDMiObjectManager>();
                    property.objectReferenceValue = man.GetComponentInChildren<FDMiReferencePoint>();
                }
            }
        }
    }
}