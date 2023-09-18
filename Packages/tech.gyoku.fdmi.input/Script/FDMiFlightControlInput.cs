
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiFlightControlInput : FDMiInput
    {
        public FDMiSyncedFloat Pitch, Roll, Yaw, Trim;
        private float[] pitch, roll, yaw, trim;

        void Start()
        {
            pitch = Pitch.data;
            roll = Roll.data;
            yaw = Yaw.data;
            trim = Trim.data;
        }


    }
}