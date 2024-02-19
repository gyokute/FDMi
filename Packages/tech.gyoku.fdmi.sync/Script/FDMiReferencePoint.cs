
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiReferencePoint : UdonSharpBehaviour
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
        public Vector3 respawnPoint;
        protected Vector3 prevPos;
        protected Quaternion prevRot;

        #region RelativePosition
        public bool stopUpdate = true;
        public virtual void initReferencePoint()
        {
            if (!parentRefPoint) parentRefPoint = syncManager;
            rootRefPoint = syncManager;
            if (parentRefPoint) ParentIndex = parentRefPoint.index;

            waitUpdate();
            isInit = true;
        }

        [SerializeField] bool careIsKinematic = true;
        bool kinematicFlag = false;
        public virtual void waitUpdate()
        {
            if (!body) return;
            setKinematic();
            turnOnStopUpdate();
            SendCustomEventDelayedFrames(nameof(turnOffStopUpdate), 5);
        }
        public void turnOnStopUpdate()
        {
            stopUpdate = true;
        }

        public void turnOffStopUpdate()
        {
            stopUpdate = false;
            unsetKinematic();
        }
        public void setKinematic()
        {
            kinematicFlag = kinematicFlag || (careIsKinematic && !body.isKinematic);
            body.isKinematic = true;
        }
        public void unsetKinematic()
        {
            if (kinematicFlag)
            {
                body.isKinematic = false;
                kinematicFlag = false;
            }
        }

        public void onChangeRootRefPoint(FDMiReferencePoint refPoint)
        {
            rootRefPoint = refPoint;
            parentIsRoot = (parentRefPoint.index == refPoint.index);
            windupPositionAndRotation();
            if (body && (!isRoot && !parentIsRoot))
                body.isKinematic = true;
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
            SendCustomEventDelayedFrames(nameof(turnOffStopUpdate), 2);
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
            if (Time.time > nextUpdateTime && !Networking.IsClogged)
            {
                syncedPos = _position;
                syncedRot = _rotation;
                syncedKmPos = _kmPosition;
                RequestSerialization();
                nextUpdateTime = Time.time + updateInterval;
            }
        }
        #endregion
    }
}