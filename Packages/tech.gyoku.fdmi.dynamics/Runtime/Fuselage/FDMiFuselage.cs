
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiFuselage : FDMiAttribute
    {
        public FDMiFloat Mass;
        public FDMiVector3 CoG, InertiaTensor;
        [SerializeField] private bool overrideMass, overrideInertia, overrideCog;
        // weight and COG settings
        private float[] mass;
        private Vector3[] cog, inertia;
        public override void init()
        {
            base.init();
            mass = Mass.data;
            cog = CoG.data;
            inertia = InertiaTensor.data;
            if (overrideMass) body.mass = mass[0];
            if (overrideInertia) body.inertiaTensor = inertia[0];
            if (overrideCog) body.centerOfMass = cog[0];
        }

        void Update()
        {
            if (!isInit) return;
            if (overrideMass) body.mass = mass[0];
            if (overrideInertia) body.inertiaTensor = inertia[0];
            if (overrideCog) body.centerOfMass = cog[0];
        }
    }
}
