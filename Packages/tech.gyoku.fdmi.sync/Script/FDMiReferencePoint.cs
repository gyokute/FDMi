
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
        [UdonSynced, FieldChangeCallback(nameof(Position))] public Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set { _position = value; }
        }
        [UdonSynced, FieldChangeCallback(nameof(direction))] public Quaternion _direction;
        public Quaternion direction
        {
            get => _direction;
            set { _direction = value; }
        }

        [UdonSynced, FieldChangeCallback(nameof(kmPosition))] public Vector3 _kmPosition;
        public Vector3 kmPosition
        {
            get => _kmPosition;
            set { handleChangeKmPosition(value); }
        }
        [UdonSynced, FieldChangeCallback(nameof(kmPosition))] public Vector3 velocity;
        public Vector3 Velocity
        {
            get => velocity;
            set { velocity = value; }
        }

        [UdonSynced, FieldChangeCallback(nameof(ParentIndex))] private int _parentIndex;
        public int ParentIndex
        {
            get => _parentIndex;
            set { handleParentIndex(value); }
        }

        public bool _isRoot = false;
        public bool isRoot
        {
            get => _isRoot;
            set
            {
                _isRoot = value;
                if (onlyIsRoot) onlyIsRoot.SetActive(value);
                if (onlyNotRoot) onlyNotRoot.SetActive(!value);
            }
        }

        public FDMiReferencePoint parentRefPoint, rootRefPoint;
        public GameObject onlyIsRoot, onlyNotRoot;

        #region RelativePosition
        public bool stopUpdate = true;
        public virtual void initReferencePoint()
        {
            if (!parentRefPoint) parentRefPoint = syncManager;
            rootRefPoint = syncManager;
            if (parentRefPoint) ParentIndex = parentRefPoint.index;

            if (onlyIsRoot)
            {
                onlyIsRoot.SetActive(false);
                onlyIsRoot.transform.position = Vector3.zero;
                onlyIsRoot.transform.rotation = Quaternion.identity;
            }
            if (onlyNotRoot) onlyIsRoot.SetActive(true);
            waitUpdate();
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
            if (isRoot) return Vector3.zero;
            Vector3 kmDiff = kmPosition;
            Vector3 diff = _position;
            Quaternion dir = Quaternion.identity;
            if (rootRefPoint)
            {
                kmDiff -= rootRefPoint.kmPosition;
                diff -= rootRefPoint.Position;
                dir = Quaternion.Inverse(rootRefPoint.direction);
            }
            diff += 1000f * (kmDiff - syncManager.localPlayerPosition.kmPosition);
            return dir * diff;
        }

        public Vector3 getViewPositionInterpolated()
        {
            Vector3 diff = getViewPosition();
            if (!Networking.IsOwner(gameObject)) return diff;
            return diff;
        }
        public virtual Quaternion getViewRotation()
        {
            if (isRoot) return Quaternion.identity;
            if (!rootRefPoint) return direction;
            return (Quaternion.Inverse(rootRefPoint.direction) * direction).normalized;
        }
        public Quaternion getViewRotationInterpolated()
        {
            Quaternion rot = getViewRotation();
            return rot;
        }
        public virtual void windupPositionAndRotation()
        {
            transform.position = getViewPosition();
            transform.rotation = getViewRotation();
        }

        public virtual bool setPosition(Vector3 pos)
        {
            Vector3 v = pos, k = kmPosition;
            bool updateKmPos = false;
            for (int i = 0; i < 3; i++)
            {
                float a = Mathf.Abs(v[i]);
                if (a >= 750f)
                {
                    updateKmPos = true;
                    float s = Mathf.Sign(v[i]);
                    a = s * Mathf.Round(a * 0.001f + 0.25000001f);
                    k[i] += a;
                    v[i] -= a * 1000f;
                }
            }
            if (updateKmPos) kmPosition = k;
            Position = v;
            return updateKmPos;
        }
        public virtual void setRotation(Quaternion rot)
        {
            direction = rot;
        }

        public virtual void handleChangeKmPosition(Vector3 value) { _kmPosition = value; }
        #endregion
    }
}