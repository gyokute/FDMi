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
        public override void ShowPropertyOption(Component tgt, SerializedProperty property)
        {
            base.ShowPropertyOption(tgt, property);
            if (property.name == nameof(FDMiStationManager.stations))
            {
                if (FDMiEditorUI.Button("Find"))
                {
                    FDMiStation[] stations = FDMiEditorUI.FindChildrenComponents<FDMiStation>(tgt).OrderBy(s => s.pilotPriority).ToArray();
                    FDMiEditorUI.SetObjectArrayProperty<FDMiStation>(property, stations);

                }
            }
        }

        public override void SetupAll()
        {
            base.SetupAll();
            serializedObject.Update();
            var tgt = target as FDMiStationManager;
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                if (property.name == nameof(FDMiStationManager.stations))
                {
                    FDMiStation[] stations = FDMiEditorUI.FindChildrenComponents<FDMiStation>(tgt).OrderBy(s => s.pilotPriority).ToArray();
                    FDMiEditorUI.SetObjectArrayProperty<FDMiStation>(property, stations);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}