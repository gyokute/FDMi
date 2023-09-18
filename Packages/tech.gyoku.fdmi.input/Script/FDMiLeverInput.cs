
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiLeverInput : FDMiInput
    {
        public FDMiSyncedFloat LeverOutput;
        public Vector3 rotationAxis;
        public float initialValue;
        public float multiply, min, max;
        public float[] detents;
        private Vector3 initDir;
        public override void OnStartGrab()
        {
            initDir = pickup.transform.position - transform.position;
            initialValue = LeverOutput.data[0];
        }
        public override void OnDropGrab()
        {
            int detentIndex = -1;
            float maxDetentsDiff = 1000f;
            for (int i = 0; i < detents.Length; i++)
            {
                float detentDiff = Mathf.Abs(detents[i] - LeverOutput.data[0]);
                if (detentDiff < maxDetentsDiff) detentIndex = i;
                maxDetentsDiff = detentDiff;
            }
            if (detentIndex >= 0) LeverOutput.Data = detents[detentIndex];
        }
        void LateUpdate()
        {
            Vector3 currentDir = pickup.transform.position - transform.position;
            LeverOutput.Data = Mathf.Lerp(initialValue + multiply * Vector3.SignedAngle(initDir, currentDir, rotationAxis), min, max);
        }

    }
}