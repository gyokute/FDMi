
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    [DefaultExecutionOrder(-100)]
    public class FDMiRelativeObjectSyncManager : FDMiReferencePoint
    {
        public FDMiReferencePoint[] refPoints;
        public FDMiReferencePoint localRootRefPoint;
        public FDMiPlayerPosition localPlayerPosition;
        void Start()
        {
            isRoot = true;
            _position = Vector3.zero;
            _posMm[0] = 0;
            _posMm[1] = 0;
            _posMm[2] = 0;
            _chunkPosition = Vector3.zero;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            _rotation = Quaternion.identity;
            localCell = Vector3.zero;
            localRootRefPoint = this;
            ParentIndex = -1;
            for (int i = 0; i < refPoints.Length; i++)
                refPoints[i].index = i;
            for (int i = 0; i < refPoints.Length; i++)
                refPoints[i].initReferencePoint();
        }
        public Quaternion localRotation;
        public Vector3 localPosition;
        [HideInInspector] public Vector3 localCell = Vector3.zero;

        public void FixedUpdate()
        {
            if (!isInit) return;
            localRotation = getViewRotation();
            localPosition = getViewPosition();
            transform.rotation = localRotation;
            transform.position = localPosition;
        }

        public void onChangeLocalPlayerChunkPosition()
        {
            for (int i = 0; i < refPoints.Length; i++)
            {
                refPoints[i].waitUpdate();
                refPoints[i].windupPositionAndRotation();
            }
        }

        public bool changeRootRefPoint(FDMiReferencePoint target)
        {
            if (!localPlayerPosition) return false;
            // Why-not: index 比較では未配線の衝突を見逃すため、参照等価で同一 root を判定する。
            if (localRootRefPoint == target) return true;
            FDMiReferencePoint prevRoot = localRootRefPoint;
            Transform tgtTransform = target.transform;
            Transform prevTransform = prevRoot.transform;

            prevRoot.waitUpdate();
            target.waitUpdate();

            prevRoot.isRoot = false;
            if (prevRoot.index != index)
                prevTransform.SetParent(prevRoot.parentRefPoint.transform);

            target.isRoot = true;
            if (target.index != index)
                tgtTransform.SetParent(null);

            localRootRefPoint = target;
            // Why-not: ParentIndex より先に新 root で求める。handleParentIndex が新 C を読むため。
            localCell = coarseCellFor(localPlayerPosition, target);
            localPlayerPosition.transform.SetParent(target.transform);
            localPlayerPosition.ParentIndex = target.index;

            // Why-not: 旧実装は target/prevRoot を明示的に windup した後に全 refPoints を再度 windup
            // していた (二重)。windup は下のループ 1 回に統一する。
            for (int i = 0; i < refPoints.Length; i++)
                refPoints[i].onChangeRootRefPoint(target);
            return true;
        }
    }
}