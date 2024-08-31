
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

namespace tech.gyoku.FDMi.sync.editor
{
    [FDMiEditorProcessPriority(0)]
    public class SyncFDMiBehaviourSetupProcess : FDMiEditorProcess
    {

        public override void execute()
        {
            GameObject[] sceneRootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var entities = sceneRootObjects.SelectMany(s => s.GetComponentsInChildren<FDMiBehaviour>());
            var attributeClasses = Assembly
                .GetAssembly(typeof(FDMiSyncAttributeEditor)).GetTypes()
                .Where(x => x.IsSubclassOf(typeof(FDMiSyncAttributeEditor)) && !x.IsAbstract).ToList();
            var classes = attributeClasses.Append(typeof(FDMiRelativeSyncStationEditor));

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