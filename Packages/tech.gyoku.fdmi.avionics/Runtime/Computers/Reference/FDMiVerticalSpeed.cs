
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiVerticalSpeed : FDMiAttribute
    {
        public FDMiFloat VerticalSpeed;
        public FDMiVector3 Velocity;
        Vector3[] vel;
        void Start()
        {
            vel = Velocity.data;
        }
        void Update()
        {
            VerticalSpeed.Data = vel[0].y;
        }
    }
}