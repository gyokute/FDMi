using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;

namespace FDMi.sync
{
    public class FDMiRelativeExecutableTransform : FDMiReferencePoint
    {
        [UdonSynced] public int[] syncedPosition = new int[3];
        [UdonSynced] public short[] syncedRotation = new short[4];
        [UdonSynced] public short[] syncedChunkPosition = new short[3];

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
                for (int i = 0; i < 3; i++)
                {
                    _chunkPosition[i] = syncedChunkPosition[i];
                    _posMm[i] = syncedPosition[i];
                }
                _position = Vector3.zero;
            }
            return getTotalPositionM();
        }

        public override Quaternion getViewRotation()
        {
            if (!Networking.IsOwner(gameObject)) _rotation = UnpackQuaternion(syncedRotation);
            return _rotation;
        }
        public override Vector3 getViewPositionInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewPosition();
            bool chunkChanged = false;
            for (int i = 0; i < 3; i++)
            {
                if (syncedChunkPosition[i] != _chunkPosition[i]) chunkChanged = true;
            }
            if (chunkChanged) return getViewPosition();
            _position = Vector3.Lerp(_position, MmToM(syncedPosition) - MmToM(_posMm), Time.deltaTime * 10);
            return getTotalPositionM();
        }
        public override Quaternion getViewRotationInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewRotation();
            _rotation = Quaternion.Slerp(_rotation, UnpackQuaternion(syncedRotation), Time.deltaTime * 10);
            return _rotation;
        }

        public override void windupPositionAndRotation()
        {
            prevPos = getViewPosition();
            prevRot = getViewRotation();
        }

        // Why-not: owner 側で synced へ書かなければ遠方に届かない。基底 TrySerialize は no-op のためここで持つ。
        protected override void TrySerialize()
        {
            if (Time.time > nextUpdateTime && !Networking.IsClogged)
            {
                for (int i = 0; i < 3; i++)
                {
                    syncedPosition[i] = _posMm[i] + (int)Mathf.Round(_position[i] * 1000f);
                    syncedChunkPosition[i] = (short)_chunkPosition[i];
                }
                syncedRotation = PackQuaternion(_rotation);
                RequestSerialization();
                nextUpdateTime = Time.time + updateInterval;
            }
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
