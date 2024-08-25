
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using tech.gyoku.FDMi.core;
using tech.gyoku.FDMi.core.editor;

namespace tech.gyoku.FDMi.input.editor
{
    [FDMiEditorProcessPriority(0)]
    public class InputFDMiBehaviourSetupProcess : FDMiEditorProcess
    {

        public override void execute()
        {
            GameObject[] sceneRootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var entities = sceneRootObjects.SelectMany(s => s.GetComponentsInChildren<FDMiBehaviour>());
            var classes = Assembly
                .GetAssembly(typeof(FDMiInputBehaviourEditor)).GetTypes()
                .Where(x => x.IsSubclassOf(typeof(FDMiInputBehaviourEditor)) && !x.IsAbstract).ToList();

            foreach (var entity in entities)
            {
                var editorComponents = classes.Select(c => new { typ = c, editor = Editor.CreateEditor(entity, c) });
                var serializedObject = new SerializedObject(entity);
                serializedObject.Update();
                foreach (var ec in editorComponents)
                {
                    ((dynamic)ec.editor).SetupAll(entity, serializedObject);
                    UnityEngine.Object.DestroyImmediate(ec.editor);
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}