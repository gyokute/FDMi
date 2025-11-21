using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiReferencePoint : FDMiBehaviour
    {
        [HideInInspector] public int index; // use in FDMiRelativeObjectSyncManager.Identify FDMiReferencePoint.
        public bool parentIsRoot;// use in FDMiRelativeObjectSyncManager.
        public FDMiRelativeObjectSyncManager syncManager;
        public Rigidbody body;
        [UdonSynced, FieldChangeCallback(nameof(ParentIndex))] protected int _parentIndex;
        public int ParentIndex
        {
            get => _parentIndex;
            set { handleParentIndex(value); }
        }

        public Vector3 _position, _velocity, _kmPosition;
        public Quaternion _rotation = Quaternion.identity;

        public bool _isRoot = false;
        public bool isRoot
        {
            get => _isRoot;
            set
            {
                _isRoot = value;
            }
        }

        public FDMiReferencePoint parentRefPoint, rootRefPoint;
        protected bool isInit = false;
        public Transform respawnPoint;
        [HideInInspector] public Vector3 respawnPos = Vector3.zero;
        [HideInInspector] public Quaternion respawnRot = Quaternion.identity;
        private
        protected Vector3 prevPos;
        protected Quaternion prevRot;

        #region RelativePosition
        public bool careIsKinematic = true;
        [HideInInspector] public bool stopUpdate = true;
        bool kinematicFlag = false;
        public virtual void initReferencePoint()
        {
            if (!parentRefPoint) parentRefPoint = syncManager;
            rootRefPoint = syncManager;
            if (parentRefPoint) ParentIndex = parentRefPoint.index;
            if (Utilities.IsValid(respawnPoint))
            {
                respawnPos = transform.InverseTransformPoint(respawnPoint.position);
                respawnRot = Quaternion.Inverse(transform.rotation) * respawnPoint.rotation;
            }
            if (body) kinematicFlag = body.isKinematic;
            stopUpdate = false;
            waitUpdate();
            isInit = true;
        }
        public virtual void waitUpdate()
        {
            if (!body) return;
            turnOnStopUpdate();
        }
        public void turnOnStopUpdate()
        {
            if (stopUpdate) return;
            setKinematic();
            SendCustomEventDelayedFrames(nameof(turnOffStopUpdate), 5);
            stopUpdate = true;
        }

        public void turnOffStopUpdate()
        {
            unsetKinematic();
            stopUpdate = false;
        }
        public void setKinematic()
        {
            if (!body || !careIsKinematic || !isInit) return;
            body.isKinematic = true;
        }
        public void unsetKinematic()
        {
            if (!body || !careIsKinematic) return;
            body.isKinematic = body.isKinematic && (kinematicFlag || (!isRoot && !parentIsRoot));
        }

        public void onChangeRootRefPoint(FDMiReferencePoint refPoint)
        {
            rootRefPoint = refPoint;
            parentIsRoot = (parentRefPoint.index == refPoint.index);
            waitUpdate();
            windupPositionAndRotation();
        }
        public void changeParentRefPoint(FDMiReferencePoint refPoint)
        {
            if (!isRoot)
                ParentIndex = refPoint.index;
            RequestSerialization();
        }

        public virtual void handleParentIndex(int value)
        {
            turnOnStopUpdate();
            _parentIndex = value;
            if (value >= 0)
            {
                parentRefPoint = syncManager.refPoints[value];
                transform.SetParent(parentRefPoint.transform);
            }
            else
            {
                transform.SetParent(null);
            }
        }

        public virtual Vector3 getViewPosition()
        {
            if (isRoot) return (syncManager.localPlayerPosition ? -1000f * syncManager.localPlayerPosition._kmPosition : Vector3.zero);
            Vector3 kmDiff = _kmPosition;
            Vector3 diff = _position;
            Quaternion dir = Quaternion.identity;
            if (rootRefPoint)
            {
                kmDiff -= rootRefPoint._kmPosition;
                diff -= rootRefPoint._position;
                dir = Quaternion.Inverse(rootRefPoint._rotation);
            }
            diff += 1000f * (kmDiff - (syncManager.localPlayerPosition ? syncManager.localPlayerPosition._kmPosition : Vector3.zero));
            // diff += 1000f * kmDiff;
            return dir * diff;
        }
        public virtual Vector3 getViewPositionInterpolated()
        {
            // if (Networking.IsOwner(gameObject)) return getViewPosition();
            // prevPos = Vector3.Lerp(prevPos, getViewPosition(), Time.deltaTime * 10);
            return getViewPosition();
        }
        public virtual Quaternion getViewRotation()
        {
            if (isRoot) return Quaternion.identity;
            if (!rootRefPoint) return _rotation;
            return (Quaternion.Inverse(rootRefPoint._rotation) * _rotation).normalized;
        }
        public virtual Quaternion getViewRotationInterpolated()
        {
            // if (Networking.IsOwner(gameObject)) return getViewRotation();
            // prevRot = Quaternion.Slerp(prevRot, getViewRotation(), Time.deltaTime * 10);
            return getViewRotation();
        }
        public virtual void windupPositionAndRotation()
        {
            prevPos = getViewPosition();
            prevRot = getViewRotation();
            transform.position = prevPos;
            transform.rotation = prevRot;
        }

        public virtual bool setPosition(Vector3 pos)
        {
            Vector3 v = pos, k = _kmPosition;
            bool updateKmPos = false;
            for (int i = 0; i < 3; i++)
            {
                float a = Mathf.Abs(v[i]);
                if (a >= 750f)
                {
                    updateKmPos = true;
                    float s = Mathf.Sign(v[i]);
                    s *= Mathf.Round(a * 0.001f + 0.25000001f);
                    k[i] += s;
                    v[i] -= s * 1000f;
                }
            }
            if (updateKmPos) _kmPosition = k;
            _position = v;
            return updateKmPos;
        }
        public virtual void setRotation(Quaternion rot)
        {
            _rotation = rot;
        }

        public virtual void handleChangeKmPosition(Vector3 value) { }
        #endregion
        #region Serialize
        [SerializeField] protected float updateInterval = 0.25f;
        [Tooltip("If vehicle moves less than this distance since it's last update, it'll be considered to be idle, may need to be increased for vehicles that want to be idle on water. If the vehicle floats away sometimes, this value is probably too big")]
        public float IdleMovementRange = .35f;

        [Tooltip("If vehicle rotates less than this many degrees since it's last update, it'll be considered to be idle")]
        public float IdleRotationRange = 5f;
        protected double nextUpdateTime, ServerTimeDiff;
        protected void ResetSyncTime()
        {
            ServerTimeDiff = Networking.GetServerTimeInSeconds() - Time.time;
            nextUpdateTime = Time.time + (double)updateInterval;
        }
        protected virtual void TrySerialize()
        {
            // Try Serialize.
            // if (Time.time > nextUpdateTime && !Networking.IsClogged)
            // {
            //     syncedPos = _position;
            //     syncedRot = _rotation;
            //     syncedKmPos = _kmPosition;
            //     RequestSerialization();
            //     nextUpdateTime = Time.time + updateInterval;
            // }
        }
        #endregion

        #region Quaternion Unility
        public static short[] PackQuaternion(Quaternion q)
        {
            Quaternion qin = q.normalized;
            short[] q_array = new short[4];
            q_array[0] = (short)(qin.x * 32767f);
            q_array[1] = (short)(qin.y * 32767f);
            q_array[2] = (short)(qin.z * 32767f);
            q_array[3] = (short)(qin.w * 32767f);
            return q_array;
        }

        public static Quaternion UnpackQuaternion(short[] data)
        {
            return new Quaternion(data[0] / 32767f, data[1] / 32767f, data[2] / 32767f, data[3] / 32767f);
        }
        #endregion
    }
}