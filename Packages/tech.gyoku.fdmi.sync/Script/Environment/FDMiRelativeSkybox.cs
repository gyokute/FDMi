
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeSkybox : FDMiRelativeGroundSync
    {

        public override Vector3 getViewPosition()
        {
            return Vector3.zero;
        }

    }
}