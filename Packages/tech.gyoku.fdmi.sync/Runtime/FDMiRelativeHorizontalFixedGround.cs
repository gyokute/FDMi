using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace FDMi.sync
{
    public class FDMiRelativeHorizontalFixedGround : FDMiReferencePoint
    {
        void Start()
        {
            setPositionAbsolute(transform.position);
            _rotation = transform.rotation;
        }
        public void Update()
        {
            transform.position = getViewPosition();

        }
        public override Vector3 getViewPosition()
        {
            Vector3 v = base.getViewPosition();
            // Why-not: 接線近似のまま x/z を落とす。動径射影への置換は地形系と併せて別途行う。
            v.x = 0f;
            v.z = 0f;
            return v;
        }
    }
}
