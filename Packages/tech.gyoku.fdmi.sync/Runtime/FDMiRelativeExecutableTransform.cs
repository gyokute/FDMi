
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
        [UdonSynced] public Vector3 syncedPos;
        [UdonSynced] public Quaternion syncedRot = Quaternion.identity;
        [UdonSynced, FieldChangeCallback(nameof(SyncedKmPos))] public Vector3 syncedKmPos;
        public Vector3 SyncedKmPos
        {
            get => syncedKmPos;
            set
            {
                syncedKmPos = value;
                handleChangeKmPosition(value);
            }
        }
        
        public override void initReferencePoint()
        {
            base.initReferencePoint();
            setPosition(parentRefPoint.transform.InverseTransformPoint(transform.position));
            _rotation = Quaternion.Inverse(parentRefPoint.transform.rotation) * transform.rotation;
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
                setPosition(transform.localPosition);
                _rotation = transform.localRotation;
                if (Vector3.Distance(prevPos, _position) > 0.005f || Quaternion.Dot(prevRot, _rotation) > 0.005f)
                    TrySerialize();
                prevPos = _position;
                prevRot = _rotation;
                return;
            }
            transform.localPosition = getViewPositionInterpolated();
            transform.localRotation = getViewRotationInterpolated();
        }
    }
}