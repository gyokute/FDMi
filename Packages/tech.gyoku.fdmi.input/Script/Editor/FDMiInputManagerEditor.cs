﻿
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
    [CustomEditor(typeof(FDMiInputManager), true)]
    public class FDMiInputManagerEditor : FDMiEditorExt
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiInputManager.fingerTrackers) && (FDMiEditorUI.Button("Find") || forceSetup))
                FDMiEditorUI.SetObjectArrayProperty<FDMiFingerTracker>(property, FindObjectsOfType<FDMiFingerTracker>());

        }
    }
}