
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
    [FDMiEditorProcessPriority(-1100000)]
    public class FDMiDataBusTerminalGatherProcess : FDMiEditorProcess
    {

        public override void execute()
        {
            GameObject[] sceneRootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var entities = sceneRootObjects.SelectMany(s => s.GetComponentsInChildren<FDMiDataBusTerminal>());
            foreach (var entity in entities)
            {
                var serializedObject = new SerializedObject(entity);
                serializedObject.Update();
                exec(entity, serializedObject);
                serializedObject.ApplyModifiedProperties();
            }
        }

        static void exec(FDMiDataBusTerminal target, SerializedObject so)
        {
            FDMiDataBus dataBus = target.GetComponentInParent<FDMiDataBus>();
            var data = so.FindProperty("data");
            var globalNames = so.FindProperty("globalName");
            var privateNames = so.FindProperty("privateName");
            var isGlobal = so.FindProperty("isGlobal");
            var dataType = so.FindProperty("dataType");
            var privateData = target.GetComponentsInChildren<FDMiData>();

            FDMiData dataComponent;
            data.arraySize = globalNames.arraySize;
            for (int i = 0; i < globalNames.arraySize; i++)
            {
                if (isGlobal.GetArrayElementAtIndex(i).boolValue)
                {
                    string dn = globalNames.GetArrayElementAtIndex(i).stringValue;
                    dataComponent = dataBus.dn
                    .Zip(dataBus.data, (n, d) => new { dn = n, data = d })
                    .Where(d => d.dn == dn)
                    .Select(d => d.data).First();
                }
                else
                {
                    string dn = privateNames.GetArrayElementAtIndex(i).stringValue;
                    var privateDatam = privateData.Where(p => p.name == dn).ToArray();
                    if (privateDatam.Length > 0)
                        dataComponent = privateDatam.First();
                    else
                    {
                        GameObject dataGO = new GameObject(dn);
                        Transform dataTran = dataGO.transform;
                        Undo.RegisterCreatedObjectUndo(dataGO, "Create local FDMiData");
                        Undo.SetTransformParent(dataTran, target.transform, "Set FDMiData Parent");
                        Type typ = FDMiDataTypeUtils.getFDMiDataType((FDMiDataType)dataType.GetArrayElementAtIndex(i).intValue);
                        dataComponent = Undo.AddComponent(dataTran.gameObject, typ) as FDMiData;
                    }
                    dataComponent.VariableName = dn;
                }
                data.GetArrayElementAtIndex(i).objectReferenceValue = dataComponent;
            }

        }

    }
}