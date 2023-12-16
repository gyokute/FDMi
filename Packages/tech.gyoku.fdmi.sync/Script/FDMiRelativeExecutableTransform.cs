
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeExecutableTransform : FDMiReferencePoint
    {
        public Transform targetTransform;
        void Start()
        {
            setPosition(targetTransform.localPosition);
            _rotation = targetTransform.localRotation;
        }
        public override Vector3 getViewPosition()
        {
            if (isRoot) return Vector3.zero;
            if (!Networking.IsOwner(gameObject))
            {
                _kmPosition = syncedKmPos;
                _position = syncedPos;
            }
            return 1000f * _kmPosition + _position;
        }

        public override Quaternion getViewRotation()
        {
            if (!Networking.IsOwner(gameObject)) _rotation = syncedRot;
            return _rotation;
        }
        public override Vector3 getViewPositionInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewPosition();
            _position = Vector3.Lerp(_position, 1000f * syncedKmPos + syncedPos, Time.deltaTime * 10);
            return _position;
        }
        public override Quaternion getViewRotationInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewRotation();
            _rotation = Quaternion.Slerp(_rotation, syncedRot, Time.deltaTime * 10);
            return _rotation;
        }

        public override void windupPositionAndRotation()
        {
            prevPos = getViewPosition();
            prevRot = getViewRotation();
        }
        void LateUpdate()
        {
            if (!isInit) return;
            if (Networking.IsOwner(gameObject))
            {
                setPosition(targetTransform.localPosition);
                _rotation = targetTransform.localRotation;
                return;
            }
            targetTransform.localPosition = getViewPositionInterpolated();
            targetTransform.localRotation = getViewRotationInterpolated();
        }
    }
}