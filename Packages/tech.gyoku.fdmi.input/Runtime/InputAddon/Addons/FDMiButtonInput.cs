﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiButtonInput : FDMiInputAddon
    {
        public FDMiSyncedBool Output;
        [SerializeField] AxisBehaviourType behaviourType;
        // protected override void Update()
        // {
        //     base.Update();
        //     if (behaviourType == AxisBehaviourType.momentum) Output.set(true);
        // }
        // public override void OnCalled(VRCPlayerApi.TrackingDataType trackType)
        // {
        //     base.OnCalled(trackType);
        //     if (behaviourType == AxisBehaviourType.alternate)
        //     {
        //         Output.set(!Output.Data);
        //     }
        // }
        // public override void OnReleased()
        // {
        //     base.OnReleased();
        //     if (behaviourType == AxisBehaviourType.momentum) Output.set(false);
        // }
    }
}