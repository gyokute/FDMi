
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.aerodynamics
{
    public class FDMiTotalAerodynamicForceDebug : UdonSharpBehaviour
    {
        public FDMiWing[] wings;
        public float totalLift, totalDrag, LbyD;
        public Vector3 totalForce;
        void Start()
        {
            wings = gameObject.GetComponentsInChildren<FDMiWing>();
        }
        void Update()
        {
            totalForce = Vector3.zero;
            foreach (FDMiWing w in wings)
            {
                totalForce += w.Force;
            }
            totalLift = Vector3.Project(totalForce, Vector3.up).magnitude;
            totalDrag = Vector3.Project(totalForce, Vector3.forward).magnitude;
            LbyD = totalLift / totalDrag;
        }
    }
}