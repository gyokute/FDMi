﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInputManager : FDMiBehaviour
    {
        public FDMiFingerTracker[] fingerTrackers = new FDMiFingerTracker[(int)FDMiFingerTrackerType.None];
        [HideInInspector] public FDMiInputPage[] defaultGrabObject = new FDMiInputPage[(int)FDMiFingerTrackerType.None];
        [HideInInspector] public bool[] isGrab = new bool[(int)FDMiFingerTrackerType.None];
        [HideInInspector] public FDMiInputPage[] grabingInput = new FDMiInputPage[(int)FDMiFingerTrackerType.None];

        public void OnStartGrab(FDMiFingerTrackerType handType, FDMiInputPage target)
        {
            isGrab[(int)handType] = true;
            grabingInput[(int)handType] = target;
        }
        public void OnDropGrab(FDMiFingerTrackerType handType)
        {
            isGrab[(int)handType] = false;
            grabingInput[(int)handType] = null;
        }

        public void OnFingerEnter(FDMiFingerTrackerType handType)
        {
            if (!defaultGrabObject[(int)handType]) return;
            defaultGrabObject[(int)handType].OnFingerLeave(fingerTrackers[(int)handType]);
        }
        public void OnFingerLeave(FDMiFingerTrackerType handType)
        {
            if (!defaultGrabObject[(int)handType]) return;
            defaultGrabObject[(int)handType].OnFingerEnter(fingerTrackers[(int)handType]);
            fingerTrackers[(int)handType].targetInput = defaultGrabObject[(int)handType];
        }
    }
}