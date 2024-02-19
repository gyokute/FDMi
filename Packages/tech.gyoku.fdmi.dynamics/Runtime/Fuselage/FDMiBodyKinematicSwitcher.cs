
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiBodyKinematicSwitcher : FDMiAttribute
    {
        public FDMiBool IsKinematic, IsPilot;
        public override void init()
        {
            base.init();
            IsKinematic.subscribe(this, "OnChangeIsKinematic");
            IsPilot.subscribe(this, "OnChangeIsPilot");
            IsKinematic.data[0] = true;
            body.isKinematic = true;
        }

        public void OnChangeIsKinematic()
        {
            body.isKinematic = IsKinematic.data[0];
        }

        public void OnChangeIsPilot()
        {
            IsKinematic.data[0] = !IsPilot.data[0];
            body.isKinematic = !IsPilot.data[0];
        }
    }
}
