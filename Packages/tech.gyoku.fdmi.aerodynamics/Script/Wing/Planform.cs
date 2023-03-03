````
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.v2.core;

namespace tech.gyoku.FDMi.v2.aerodynamics
{
    /// <summary>
    /// Apply aerodynamic forces to rigidbody.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public class Planform : VorticityField
    {
        public Atmosphere Atomosphere;
        public VorticityField[] affectedField;
        public Planform fPlaform, lPlanform, rPlanform;
        public AirFoil lFoil, rFoil;

        
        [SelializedField] private float planfArea;

    
        private float[] atom;
        private float[] fPlnfG, lPlanfG, rPlanfG;
        private float[] lFoilG, rFoilG;

        public Vector3 planfMac, chordNormal, spanNormal, planfNormal;
        
        void Start()
        {
            // atom = Atomosphere
            fPlnfG = fPlaf.Gamma;
            UpdatePlanformShape();
            
        }
        void FixedUpdate()
        {
            // induced airspeed
            
            // estimate alpha
            
            // calculate gammma
            // Gamma[0] = 
        }

        void UpdatePlanfrmArea(){
            float chordLenAve = Mathf.Lerp(lFoil.chordLength, rFoil.chordLength, 0.5f);
            planfArea = Vector3.Cro                   ss(chordNormal, spanNormal).magnitude
            
        }

        void UpdatePlanformShape(){
            Vector3 planfMacTip = Vector3.Lerp(lFoil.position, rFoil.position, 0.5f);
            Vector3 planfMacToe = Vector3.Lerp(foilToe(lFoil), foilToe(rFoil), 0.5f);
            planfMac = Vector3.Lerp(planfMacTip, planfMacToe, 0.25f);
            chordNormal = Vector3.Normalize(planfMacToe - planfMacTip);
            Vector3 lMac = lFoil.position - lFoil.transform.forward * lFoil.chordLength;
            Vector3 rMac = rFoil.position - rFoil.transform.forward * rFoil.chordLength;
            planfMac = Vector3.Lerp(lMac, rMac,0.5f);
            spanNormal = Vector3.Normalize(rMac - lMac);
            chordNormal = -Vector3.Normalize(Vector3.Lerp(lFoil.transform.forward, rFoil.transform.forward, 0.5f);
            planfNormal = Vector3.Cross(spanNormal, chordNormal);
        }
   
        static Vector3 foilToe(AirFoil foil) => foil.position - foil.chordLength * foil.forward;
   
        void UpdateTottionOffset(){

        }

    }
}