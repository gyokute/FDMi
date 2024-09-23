
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeHorizontalFixedGround : FDMiReferencePoint
    {
        void Start()
        {
            setPosition(transform.position);
            _rotation = transform.rotation;
        }
        public void Update()
        {
            transform.position = getViewPosition();

        }
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
            if (syncManager.localPlayerPosition) diff -= 1000f * syncManager.localPlayerPosition._kmPosition;
            diff += 1000f * kmDiff;
            diff.x = 0f;
            diff.z = 0f;
            return dir * diff;
        }
    }
}