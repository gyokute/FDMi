
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeHorizontalFixedGround : FDMiRelativeGroundSync
    {

        public override Vector3 getViewPosition()
        {
            if (isRoot) return Vector3.zero;
            Vector3 kmDiff = _kmPosition;
            Vector3 diff = _position;
            Quaternion dir = Quaternion.identity;
            if (rootRefPoint)
            {
                kmDiff -= rootRefPoint._kmPosition;
                diff -= rootRefPoint._position;
                dir = Quaternion.Inverse(rootRefPoint._rotation);
            }
            diff += 1000f * (kmDiff - syncManager.localPlayerPosition._kmPosition);
            // diff += 1000f * kmDiff;
            diff.x = 0f;
            diff.z = 0f;
            return dir * diff;
        }
    }
}