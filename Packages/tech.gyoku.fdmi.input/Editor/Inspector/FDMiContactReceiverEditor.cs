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
    [CustomEditor(typeof(FDMiContactReceiver), true)]
    public class FDMiContactReceiverEditor : FDMiHandInputGroupEditor
    {
        ResolveDataPathsUseCase dataPathUseCase;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (dataPathUseCase == null)
                dataPathUseCase = new ResolveDataPathsUseCase();
            dataPathUseCase.Execute(target);

            FDMiContactReceiver component = (FDMiContactReceiver)target;
            VRCContactReceiver contactReceiver = component.GetComponent<VRCContactReceiver>();
            
            DispatchRootTransform(contactReceiver, component.targetTransform);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FDMiContactReceiver component = (FDMiContactReceiver)target;
            VRCContactReceiver contactReceiver = component.GetComponent<VRCContactReceiver>();

            MoveReceiverPosition(component.transform, contactReceiver);
            EditorGUI.BeginChangeCheck();
            if (EditorGUI.EndChangeCheck())
            {
                dataPathUseCase.Execute(target);
                DispatchRootTransform(contactReceiver, component.targetTransform);
            }
        }

        void MoveReceiverPosition(Transform helperTransform, VRCContactReceiver contactReceiver)
        {
            var pos = helperTransform.position;
            if (contactReceiver.rootTransform)
                contactReceiver.position = contactReceiver.rootTransform.InverseTransformPoint(pos);
        }

        void DispatchRootTransform(VRCContactReceiver contactReceiver, FDMiTransformRef targetTransform)
        {
            if (targetTransform && targetTransform.Data)
                contactReceiver.rootTransform = targetTransform.Data;
        }
    }
}
