
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
    [FDMiEditorProcessPriority(-1200000)]
    public class FDMiDataBusGatherProcess : FDMiEditorProcess
    {

        public override void execute()
        {
            GameObject[] sceneRootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var dataBuses = sceneRootObjects.SelectMany(s => s.GetComponentsInChildren<FDMiDataBus>()).ToArray();
            foreach (var dataBus in dataBuses)
            {
                var serializedObject = new SerializedObject(dataBus);
                serializedObject.Update();
                gatherFromTerminals(dataBus, serializedObject);
                gatherFromDataAdapters(dataBus, dataBuses, serializedObject);
                serializedObject.ApplyModifiedProperties();
            }
        }

        static void gatherFromTerminals(FDMiDataBus dataBus, SerializedObject so)
        {
            Transform dataBusTransform = dataBus.transform;
            Transform DataRoot = dataBusTransform.Find("Data");
            if (!DataRoot)
            {
                GameObject DataRootGO = new GameObject("Data");
                Undo.RegisterCreatedObjectUndo(DataRootGO, "Create Data GameObject");
                DataRoot = DataRootGO.transform;
                Undo.SetTransformParent(DataRoot, dataBusTransform, "Set Data Transform Parent");
            }
            DataRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var dn = so.FindProperty("dn");
            var data = so.FindProperty("data");

            var childAdapters = dataBus.GetComponentsInChildren<FDMiDataAdapter>();
            var adapterVariables = childAdapters.SelectMany(adapter => adapter.replicaDN).ToList();

            var childTerminals = dataBus.GetComponentsInChildren<FDMiDataBusTerminal>();
            var termVariables = childTerminals.SelectMany(
                terminal => terminal.globalName.Select((gn, i) => i),
                (terminal, i) => new
                {
                    dn = terminal.globalName[i],
                    dt = terminal.dataType[i],
                    isGlobal = terminal.isGlobal[i]
                }
            ).Where(t => t.isGlobal == true && !adapterVariables.Contains(t.dn))
            .GroupBy(i => i.dn, (k, v) => v.OrderByDescending(o => o.dt).First()).ToArray();

            dn.arraySize = termVariables.Length;
            data.arraySize = termVariables.Length;
            for (var i = 0; i < termVariables.Length; i++)
            {
                dn.GetArrayElementAtIndex(i).stringValue = termVariables[i].dn;
                Transform datamTran = DataRoot.GetComponentsInChildren<FDMiData>().Select(datam => datam.transform).FirstOrDefault(datam => datam.name == termVariables[i].dn);
                if (!datamTran)
                {
                    var dataObj = new GameObject(termVariables[i].dn);
                    datamTran = dataObj.transform;
                    Undo.RegisterCreatedObjectUndo(dataObj, "Create FDMiData");
                }
                Undo.SetTransformParent(datamTran, DataRoot, "Set FDMiData Transform Parent");

                Type dataType = FDMiDataTypeUtils.getFDMiDataType(termVariables[i].dt);
                FDMiData dataComponent = datamTran.GetComponent(dataType) as FDMiData;
                if (!dataComponent)
                    dataComponent = Undo.AddComponent(datamTran.gameObject, dataType) as FDMiData;

                data.GetArrayElementAtIndex(i).objectReferenceValue = dataComponent;
                dataComponent.VariableName = termVariables[i].dn;


            }
            DataRoot.localPosition = Vector3.zero;
            DataRoot.localRotation = Quaternion.identity;
            DataRoot.SetAsFirstSibling();
        }


        static void gatherFromDataAdapters(FDMiDataBus dataBus, FDMiDataBus[] dataBuses, SerializedObject so)
        {
            Transform dataBusTransform = dataBus.transform;
            Transform DataRoot = dataBusTransform.Find("Data");
            if (!DataRoot)
            {
                GameObject DataRootGO = new GameObject("Data");
                Undo.RegisterCreatedObjectUndo(DataRootGO, "Create Data GameObject");
                DataRoot = DataRootGO.transform;
                Undo.SetTransformParent(DataRoot, dataBusTransform, "Set Data Transform Parent");
            }
            DataRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var dn = so.FindProperty("dn");
            var data = so.FindProperty("data");

            var adapterVariables = dataBus.GetComponentsInChildren<FDMiDataAdapter>().SelectMany(
                adapter => adapter.primaryDN.Select((pdn, i) => i),
                (adapter, i) => new
                {
                    bus = dataBuses.Where(db => db.dataBusName == adapter.primaryBusName).First(),
                    pdn = adapter.primaryDN[i],
                    rdn = adapter.replicaDN[i]
                }).SelectMany(val => val.bus.data, (val, datam) => new
                {
                    data = datam,
                    pdn = val.pdn,
                    rdn = val.rdn
                }).Where(d => d.data.VariableName == d.pdn).GroupBy(i => i.pdn, (k, v) => v.First());

            foreach (var i in adapterVariables)
            {
                data.arraySize += 1;
                dn.arraySize += 1;
                dn.GetArrayElementAtIndex(dn.arraySize - 1).stringValue = i.rdn;
                data.GetArrayElementAtIndex(dn.arraySize - 1).objectReferenceValue = i.data;
            }
            DataRoot.localPosition = Vector3.zero;
            DataRoot.localRotation = Quaternion.identity;
        }
    }
}