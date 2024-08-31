using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor
{
    public abstract class FDMiEditorExt : UnityEditor.Editor
    {
        public abstract void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup = false);
        public abstract void SetupAll(Component tgt, SerializedObject serializedObject);
    }
}