
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInputManager : UdonSharpBehaviour
    {
        [HideInInspector] public bool[] isGrab = new bool[3];
        [HideInInspector] public FDMiInput[] holdingInput = new FDMiInput[3];
        // public FDMiInput[] inputs;
        public void OnStartGrab(FDMiFingerTrackerType handType, FDMiInput target)
        {
            isGrab[(int)handType] = true;
            holdingInput[(int)handType] = target;
        }
        public void OnDropGrab(FDMiFingerTrackerType handType, FDMiInput target)
        {
            isGrab[(int)handType] = false;
            holdingInput[(int)handType] = null;
        }
    }
}