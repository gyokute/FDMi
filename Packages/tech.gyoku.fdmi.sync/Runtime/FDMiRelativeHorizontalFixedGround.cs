using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace FDMi.sync
{
    public class FDMiRelativeHorizontalFixedGround : FDMiReferencePoint
    {
        void Start()
        {
            setPosition(transform.position);
            _rotation = transform.rotation;
        }
        public void Update()
        {
            transform.position = getViewPosition();

        }
        public override Vector3 getViewPosition()
        {
            if (isRoot) return Vector3.zero;
            Vector3 rootPos = rootRefPoint ? rootRefPoint._position : Vector3.zero;
            Vector3 rootChunkPos = rootRefPoint ? rootRefPoint._chunkPosition : Vector3.zero;
            int[] rootMm = rootRefPoint ? rootRefPoint._posMm : _posMm;
            Vector3 chunkDiff = _chunkPosition - rootChunkPos;
            Quaternion dir = Quaternion.identity;
            if (rootRefPoint)
            {
                dir = Quaternion.Inverse(rootRefPoint._rotation);
            }
            Vector3 diff = _position - rootPos;
            diff += new Vector3(_posMm[0] - rootMm[0], _posMm[1] - rootMm[1], _posMm[2] - rootMm[2]) * 0.001f;
            Vector3 localTotalM = syncManager.localPlayerPosition ? syncManager.localPlayerPosition.getTotalPositionM() : Vector3.zero;
            diff += chunkDiff * M_PER_CHUNK - localTotalM;
            // Why-not: 接線近似のまま x/z を落とす。動径射影への置換は地形系と併せて別途行う。
            diff.x = 0f;
            diff.z = 0f;
            return dir * diff;
        }
    }
}
