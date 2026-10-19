using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;

namespace FDMi.sync
{
    public class FDMiRelativeGroundSync : FDMiReferencePoint
    {
        public float disableKmPos = 9999f;
        void Start()
        {
            setPositionAbsolute(transform.position);
            _rotation = transform.rotation;
            transform.SetPositionAndRotation(getViewPosition(), getViewRotation());
        }
        public void Update()
        {
            transform.SetPositionAndRotation(getViewPosition(), getViewRotation());
        }

        public override void windupPositionAndRotation()
        {
            transform.SetPositionAndRotation(getViewPosition(), getViewRotation());
        }
    }
}