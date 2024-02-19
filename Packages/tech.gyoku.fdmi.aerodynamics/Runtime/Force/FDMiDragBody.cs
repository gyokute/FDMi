
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.aerodynamics
{
    public class FDMiDragBody : FDMiAttribute
    {
        // from NAL-SP-00012 
        public FDMiFloat Rho, TAS;
        public float d, l;
        public float cdp = 0.075f;
        public float lambda = 0.016f;
        public float cdA;
        [SerializeField] float Drag;
        float[] rho, tas;
        void Start()
        {
            rho = Rho.data;
            tas = TAS.data;
        }
        void FixedUpdate()
        {
            Drag = rho[0] * cdA * tas[0] * tas[0];
            body.AddRelativeForce(-Drag * Vector3.forward);
        }
    }
}