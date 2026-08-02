using System.Collections.Generic;
using System.Linq;
using FDMi.core;
using FDMi.core.Editor.Application.UseCases;
using FDMi.input;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;

namespace FDMi.input.Editor
{
    [CustomEditor(typeof(FDMiContactReceiverHelper), true)]
    public class FDMiContactReceiverHelperEditor : UnityEditor.Editor
    {
        ResolveDataPathsUseCase dataPathUseCase;

        void OnEnable()
        {
            if (dataPathUseCase == null)
                dataPathUseCase = new ResolveDataPathsUseCase();
            dataPathUseCase.Execute(target);

            FDMiContactReceiverHelper component = (FDMiContactReceiverHelper)target;
            VRCContactReceiver contactReceiver = component.GetComponent<VRCContactReceiver>();
            DispatchRootTransform(contactReceiver, component.rootTransformRef);
        }

        public override void OnInspectorGUI()
        {
            FDMiContactReceiverHelper component = (FDMiContactReceiverHelper)target;
            VRCContactReceiver contactReceiver = component.GetComponent<VRCContactReceiver>();

            MoveReceiverPosition(component.transform, contactReceiver);
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                dataPathUseCase.Execute(target);
                DispatchRootTransform(contactReceiver, component.rootTransformRef);
            }
        }

        void MoveReceiverPosition(Transform helperTransform, VRCContactReceiver contactReceiver)
        {
            var pos = helperTransform.position;
            if (contactReceiver.rootTransform)
                contactReceiver.position = contactReceiver.rootTransform.InverseTransformPoint(pos);
        }

        void DispatchRootTransform(VRCContactReceiver contactReceiver, FDMiTransformRef rootTransformRef)
        {
            if (rootTransformRef && rootTransformRef.Data)
                contactReceiver.rootTransform = rootTransformRef.Data;
        }
    }
}
