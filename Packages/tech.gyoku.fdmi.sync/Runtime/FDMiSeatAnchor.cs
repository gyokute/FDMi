using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    [RequireComponent(typeof(VRCStation))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiSeatAnchor : FDMiBehaviour
    {
        [Tooltip("Sync body owned by the seated pilot. Same GameObject as the FDMi or Sacc vehicle root.")]
        public FDMiGeodeticObjectSync vehicleSync;
        [Tooltip("World coordinate resolver. Governs the seated player's km-cell state.")]
        public FDMiGeodeticSyncManager syncManager;
        [Tooltip("Where the seated avatar is drawn. Defaults to stationEnterPlayerLocation.")]
        public Transform seatView;
        [Tooltip("While seated, copy this heading each frame. Empty = follow the vehicle root orientation.")]
        public Transform headingSource;
        [Tooltip("Offset from the seat in vehicle-local meters (x=right, y=up, z=forward). Set the seat view's local position to the same value so remote clients agree.")]
        public Vector3 seatOffsetM = new Vector3(0f, 0.55f, 0f);

        VRCStation station;
        bool seated;
        Transform vehicleRoot;

        void Start()
        {
            station = GetComponent<VRCStation>();
            if (!seatView)
                seatView = station.stationEnterPlayerLocation;
            if (vehicleSync)
                vehicleRoot = vehicleSync.target ? vehicleSync.target : vehicleSync.transform;
        }

        public override void Interact()
        {
            if (!vehicleSync || !vehicleRoot)
                return;
            // 着席時に所有権を取るのはここだけ(同期の送信関数に SetOwner を混ぜない)
            Networking.SetOwner(Networking.LocalPlayer, vehicleSync.gameObject);
            vehicleSync.CaptureAndSend(true);
            Networking.LocalPlayer.UseAttachedStation();
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            seated = true;
            // 着席中は座標系の主役が車両になる(root 切替)。
            // 車両が Unity 原点付近に固定され、ワールド側が車両の論理位置に合わせて動く。
            if (syncManager)
            {
                if (vehicleSync)
                    syncManager.changeRootRefPoint(vehicleSync);
                // inVehicle はフレーム切替の後に立てる(この間に参照点が休止しても、切替時の
                // 位置読み直しは座席状態に依存しない)
                if (syncManager.localPlayerPosition)
                    syncManager.localPlayerPosition.inVehicle = true;
            }
            if (vehicleSync && Networking.IsOwner(vehicleSync.gameObject))
                vehicleSync.CaptureAndSend(true);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            seated = false;
            if (syncManager)
            {
                if (syncManager.localPlayerPosition)
                    syncManager.localPlayerPosition.inVehicle = false;
                syncManager.changeRootRefPoint(syncManager);
            }
        }

        void Update()
        {
            if (!seated || !vehicleRoot || !seatView)
                return;
            Vector3 anchor = vehicleRoot.TransformPoint(seatOffsetM);
            Quaternion heading = headingSource ? headingSource.rotation : vehicleRoot.rotation;
            seatView.position = anchor;
            seatView.rotation = heading;
        }
    }
}
