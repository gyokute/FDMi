
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{

    public class FDMiAxisInput : FDMiInputAddon
    {
        [SerializeField] FDMiSyncedFloat Output;
        [SerializeField] InputAxis inputAxisType;
        public override void Update()
        {
            base.Update();
            Output.set(input.inputAxis[(int)inputAxisType]);
        }
        public override void OnReleased()
        {
            base.OnReleased();
            Output.set(0f);
        }
    }
}