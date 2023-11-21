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
    [CustomEditor(typeof(FDMiEvent), true)]
    public class FDMiEventEditor : FDMiEditorExt
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiEvent.VariableName))
            {
                if ((FDMiEditorUI.Button(false, "Maybe"))) property.stringValue = tgt.transform.name;
                if ((FDMiEditorUI.Button(false, "FullPath"))) property.stringValue = GetFullPath(tgt.transform);
            }
        }
        public static string GetFullPath(Transform t)
        {
            string path = t.name;
            var parent = t.parent;
            var grandParent = parent.parent;
            Rigidbody rb;
            FDMiObjectManager man;
            while (parent)
            {
                if (grandParent.TryGetComponent<Rigidbody>(out rb)) break;
                if (grandParent.TryGetComponent<FDMiObjectManager>(out man)) break;
                path = $"{parent.name}/{path}";
                parent = grandParent;
                grandParent = parent.parent;
            }
            return path;
        }

    }
}