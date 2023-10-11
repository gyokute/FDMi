
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
        [SerializeField] float multiply = 1f;
        public override void Update()
        {
            base.Update();
            Output.set(multiply * inputAxis[(int)inputAxisType]);
        }
        public override void OnReleased()
        {
            base.OnReleased();
            Output.set(0f);
        }
    }
}