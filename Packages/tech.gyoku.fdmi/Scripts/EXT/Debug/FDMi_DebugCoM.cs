
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace SaccFlight_FDMi
{
    public class FDMi_DebugCoM : FDMi_Attributes
    {
        private void FixedUpdate()
        {
            Vector3 com = VehicleRigidbody.centerOfMass;
            com = VehicleRigidbody.transform.TransformPoint(com);
            transform.position = com;
        }
    }
}