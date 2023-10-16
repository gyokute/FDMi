
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiVerticalSpeed : FDMiAttribute
    {
        [SerializeField] FDMiFloat VerticalSpeed;
        [SerializeField] FDMiVector3 Velocity;
        float[] vs;
        Vector3[] vel;
        void Start()
        {
            vs = VerticalSpeed.data;
            vel = Velocity.data;
        }
        void Update()
        {
            vs[0] = vel[0].y;
        }
    }
}