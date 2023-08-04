
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiReferencePoint : UdonSharpBehaviour
    {
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
            set { _kmPosition = value; handleChangeKmPosition(); }
        }

        [UdonSynced, FieldChangeCallback(nameof(ParentIndex))] public int _parentIndex;
        public int ParentIndex
        {
            get => _parentIndex;
            set { _parentIndex = value; handleParentIndex(value); }
        }
        public Vector3 velocity;

        public bool _isRoot = false;
        public bool isRoot
        {
            get => _isRoot;
            set
            {
                _isRoot = value;
                if (!onlyIsRoot) return;
                if (value) onlyIsRoot.SetActive(true);
                else onlyIsRoot.SetActive(false);
            }
        }
        public int index;
        public FDMiReferencePoint parentRefPoint, rootRefPoint;
        public GameObject onlyIsRoot;

        #region RelativePosition
        public Vector3 oldPos = Vector3.zero;
        public Quaternion oldRot;
        public Vector3 prevRelativePos;
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
            waitUpdate();
        }

        [SerializeField] bool careIsKinematic;
        public virtual void waitUpdate()
        {
            if (!body) return;
            careIsKinematic = (careIsKinematic || !body.isKinematic);
            body.isKinematic = true;
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
            if (careIsKinematic && Networking.IsOwner(gameObject)) body.isKinematic = false;
            careIsKinematic = false;
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

        public Vector3 getViewPosition()
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
            diff += 1000f * kmDiff;
            return dir * diff;
        }

        public Vector3 getViewPositionInterpolated()
        {
            Vector3 diff = getViewPosition();
            if (!Networking.IsOwner(gameObject)) return diff;
            else oldPos = Vector3.Lerp(oldPos, diff, Time.deltaTime * 10f);
            return diff;
        }
        public Quaternion getViewRotation()
        {
            if (isRoot) return Quaternion.identity;
            if (!rootRefPoint) return direction;
            return (direction * Quaternion.Inverse(rootRefPoint.direction)).normalized;
        }
        public Quaternion getViewRotationInterpolated()
        {
            Quaternion rot = getViewRotation();
            if (Networking.IsOwner(gameObject)) oldRot = rot;
            else oldRot = Quaternion.Slerp(oldRot, rot, Time.deltaTime * 10f);
            return oldRot;
        }
        public void windupPositionAndRotation()
        {
            oldPos = getViewPosition();
            oldRot = getViewRotation();

            transform.position = oldPos;
            transform.rotation = oldRot;

            if (body)
            {
                body.position = transform.position;
                body.rotation = transform.rotation;
            }
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
                    a = s * Mathf.Round(a * 0.001f + 0.5f);
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

        public virtual void handleChangeKmPosition() { }
        #endregion
        public virtual void Update()
        {
            transform.rotation = getViewRotationInterpolated();
            transform.position = getViewPositionInterpolated();
        }
    }
}