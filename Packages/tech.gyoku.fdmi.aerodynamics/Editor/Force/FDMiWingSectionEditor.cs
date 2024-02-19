using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using tech.gyoku.FDMi.aerodynamics;

namespace tech.gyoku.FDMi.aerodynamics.editor
{
    [CustomEditor(typeof(FDMiWingSection), true)]
    public class FDMiWingSectionEditor : UnityEditor.Editor
    {
        public void OnSceneGUI()
        {
            var FDMiWingSection = target as FDMiWingSection;
            drawChord(FDMiWingSection);
        }
        public static void drawChord(FDMiWingSection tgt)
        {
            using (new Handles.DrawingScope(tgt.transform.localToWorldMatrix))
            {
                Handles.color = Color.magenta;
                Handles.DrawLine(Vector3.zero, -tgt.chordLength * Vector3.forward);
            }
        }
    }
}