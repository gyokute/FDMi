
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiTentativeEngine : FDMiAttribute
    {
        public FDMiFloat Input;
        public float maxThrust;
        private float[] input;
        void Start()
        {
            input = Input.data;
        }
        void FixedUpdate()
        {
            if (!isInit) return;
            body.AddRelativeForce(maxThrust * input[0] * Vector3.forward);
        }
    }
}