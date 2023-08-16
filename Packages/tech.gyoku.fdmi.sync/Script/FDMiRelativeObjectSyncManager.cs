
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeObjectSyncManager : FDMiReferencePoint
    {
        public Vector3 respawnPoint = Vector3.zero;
        public FDMiReferencePoint[] refPoints;
        public FDMiReferencePoint localRootRefPoint;
        public FDMiPlayerPosition localPlayerPosition;
        void Start()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            localRootRefPoint = this;
            ParentIndex = -1;
            for (int i = 0; i < refPoints.Length; i++)
                refPoints[i].index = i;
            for (int i = 0; i < refPoints.Length; i++)
                refPoints[i].initReferencePoint();
        }

        public void LateUpdate()
        {
            transform.rotation = getViewRotation();
            transform.position = getViewPosition();
        }

        public void onChangeLocalPlayerKMPosition()
        {
            for (int i = 0; i < refPoints.Length; i++)
            {
                refPoints[i].waitUpdate();
                refPoints[i].windupPositionAndRotation();
            }
        }

        public void changeRootRefPoint(FDMiReferencePoint target)
        {
            if (localRootRefPoint.index != index && target.index != index)
                changeRootRefPoint(syncManager);
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
            {
                tgtTransform.SetParent(null);
            }
            localRootRefPoint = target;
            localPlayerPosition.ParentIndex = target.index;
            localPlayerPosition.transform.SetParent(target.transform);

            for (int i = 0; i < refPoints.Length; i++)
            {
                refPoints[i].onChangeRootRefPoint(target);
            }
            target.windupPositionAndRotation();
            prevRoot.windupPositionAndRotation();
        }

    }
}