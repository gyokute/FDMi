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
    [CustomEditor(typeof(FDMiStationManager), true)]
    public class FDMiStationManagerEditor : FDMiAttributeEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiStationManager.stations))
            {
                if ((FDMiEditorUI.Button("Find")|| forceSetup))
                {
                    FDMiStation[] stations = FDMiEditorUI.FindChildrenComponents<FDMiStation>(tgt).OrderBy(s => s.pilotPriority).ToArray();
                    FDMiEditorUI.SetObjectArrayProperty<FDMiStation>(property, stations);

                }
            }
        }
    }
}