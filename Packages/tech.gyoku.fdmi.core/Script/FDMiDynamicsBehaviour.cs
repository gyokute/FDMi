
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.v2.core
{
    public class FDMiDynamicsBehaviour : FDMiBehaviour
    {
        public bool autoResolveVehicleRigidbody;
        [HideInInspector] public Rigidbody vehicle;

    }
}