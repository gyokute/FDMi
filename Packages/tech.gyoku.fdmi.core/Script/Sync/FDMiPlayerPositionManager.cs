
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiPlayerPositionManager : UdonSharpBehaviour
    {
        FDMiPlayerPositionStation localStation;

        #region lifecycle
        void Start()
        {
        }
        void Update()
        {

        }
        #endregion
        #region event
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) init(player);
            if (Networking.IsMaster) allocateStation(player);

        }
        public override void OnPlayerLeft(VRCPlayerApi player) { }

        public override void OnPlayerRespawn(VRCPlayerApi player) { }
        public override void OnOwnershipTransferred(VRCPlayerApi player) { }
        public override void OnStationEntered(VRCPlayerApi player) { }
        public override void OnStationExited(VRCPlayerApi player) { }
        #endregion

        #region Initialize
        void init(VRCPlayerApi player)
        {
            player.SetRunSpeed(0);
            player.SetWalkSpeed(0);
            player.SetRunSpeed(0);
            player.SetStrafeSpeed(0);
            player.SetJumpImpulse(0);
            player.SetGravityStrength(0);
            initStationManagement();
        }
        #endregion
        #region station management
        public FDMiPlayerPositionStation[] stationPool;

        void initStationManagement()
        {
            if (Networking.IsMaster)
            {
                StationAllocation = new int[stationPool.Length];
            }
        }
        bool allocStationReqSerScheduled = false;
        void allocateStation(VRCPlayerApi player)
        {
            // if station is not allocated(allocated player id is 0), assign new player 
            assignStation(player, findEmptyStation());
            // After 90 frame, syncronize seated player
            if (!allocStationReqSerScheduled)
                SendCustomEventDelayedFrames(nameof(SerializeAllocateStatus), 90);
            allocStationReqSerScheduled = true;
        }
        void SerializeAllocateStatus()
        {
            allocStationReqSerScheduled = false;
            RequestSerialization();
        }

        void releaseStation(int releaseStationId)
        {
            _stationAllocation[releaseStationId] = 0;
        }

        public void checkStationCoherence()
        {
            for (int i = 0; i < stationPool.Length; i++)
                stationPool[i].maintainSeatedPlayer(_stationAllocation[i]);
        }

        int assignStation(VRCPlayerApi player, int assignIndex)
        {
            if (assignIndex < 0) return -1;
            if (stationPool[emptyStationIndex].seatedPlayer == null)
            {
                stationPool[assignIndex].registrate(player);
                _stationAllocation[assignIndex] = player.playerId;
            }
            return assignIndex;
        }
        int emptyStationIndex = 0;
        int findEmptyStation()
        {
            int startIndex = emptyStationIndex;
            int len = stationPool.Length;
            while (_stationAllocation[emptyStationIndex] > 0)
            {
                emptyStationIndex = (emptyStationIndex + 1) % len;
                if (emptyStationIndex == startIndex)
                {
                    error(-100);
                    return -1;
                }
            }
            return emptyStationIndex;
        }

        #endregion

        #region local Status
        #endregion

        #region errorHandling
        int prevError;
        void error(int errIndex)
        {
            if (errIndex == -100)
            {
                // station index error
                Debug.Log("FDMi>PlayerPositionManager> [ERR] Station Buffer Overrun. Resync station after 2 sec.");
            }
            prevError = errIndex;
        }
        #endregion
    }
}