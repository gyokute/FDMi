
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiFuselage : FDMiAttribute
    {
        public FDMiFloat OEW, Fuel, Payload;
        public FDMiVector3 CoG, InertiaTensor;
        public FDMiSyncedBool Kinematic;
        [SerializeField] private bool overrideMass, overrideInertia, overrideCog;
        // weight and COG settings
        private float[] oew, fuel, payload;
        private Vector3[] cog, inertia;
        public override void init()
        {
            base.init();
            Kinematic.subscribe(this, "OnChangeKinematic");
            oew = OEW.data;
            fuel = Fuel.data;
            payload = Payload.data;
            cog = CoG.data;
            inertia = InertiaTensor.data;
            if (overrideMass) body.mass = oew[0] + fuel[0] + payload[0];
            if (overrideInertia) body.inertiaTensor = inertia[0];
            if (overrideCog) body.centerOfMass = cog[0];
        }

        void Update()
        {
            if (!isInit) return;
            if (overrideMass) body.mass = oew[0] + fuel[0] + payload[0];
            if (overrideInertia) body.inertiaTensor = inertia[0];
            if (overrideCog) body.centerOfMass = cog[0];
        }

        public void OnChangeKinematic()
        {
            body.isKinematic = Kinematic.data[0];
        }
    }
}
