
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor.process
{
    [FDMiEditorProcessPriority(-1000000)]
    public class FDMiBehaviourGatherProcess : FDMiEditorProcess
    {

        public override void execute()
        {
            GameObject[] sceneRootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var entities = sceneRootObjects.SelectMany(s => s.GetComponentsInChildren<FDMiBehaviour>());

            foreach (var entity in entities)
            {
                var serializedObject = new SerializedObject(entity);
                serializedObject.Update();
                setFDMiData(entity, serializedObject);
                serializedObject.ApplyModifiedProperties();
            }
        }

        void setFDMiData(FDMiBehaviour target, SerializedObject so)
        {
            FDMiDataBusTerminal terminal = target.GetComponentInParent<FDMiDataBusTerminal>();
            if (!terminal) return;

            var fields = target.GetType().GetFields()
                .Where(x => x.FieldType.IsSubclassOf(typeof(FDMiData)))
                .Select(x => new { typ = x.FieldType, name = x.Name, property = so.FindProperty(x.Name) });

            foreach (var f in fields)
            {
                int index = Array.IndexOf(terminal.privateName, f.name);
                if (index >= 0)
                    f.property.objectReferenceValue = terminal.data[Array.IndexOf(terminal.privateName, f.name)];
            }

            var arrayFields = target.GetType().GetFields()
                .Where(x => x.FieldType.IsArray)
                .Where(x => x.FieldType.GetElementType().IsSubclassOf(typeof(FDMiData)))
                .Select(x => new { typ = x.FieldType, name = x.Name, property = so.FindProperty(x.Name) });

            foreach (var f in arrayFields)
            {
                var data = terminal.data.Where((x, i) => terminal.privateName[i] == f.name).ToList();
                if (data.Count > 0)
                {
                    f.property.arraySize = data.Count;
                    for (int i = 0; i < data.Count; i++)
                        f.property.GetArrayElementAtIndex(i).objectReferenceValue = data[i];
                }
            }
        }

    }
}