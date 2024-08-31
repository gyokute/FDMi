
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiPlayerSyncManager : FDMiBehaviour
    {
        public FDMiPlayerPositionAssigner[] assigners;
        VRCPlayerApi[] players = new VRCPlayerApi[80];
        public bool isMaster = false;
        private const float delayNewMasterVerificationDuration = 1f;


        #region lifecycle
        void Start()
        {
            assigners = gameObject.GetComponentsInChildren<FDMiPlayerPositionAssigner>();
        }
        #endregion

        #region events
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) return;
            int temp = player.playerId;
            checkMasterSwap();
            allocate(player);

        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return false;
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            Debug.LogWarning("OnOwnershipTransferred Called");
            SendCustomEventDelayedSeconds(nameof(checkMasterSwap), 1f);
            // checkMasterSwap();
        }

        #endregion

        private void checkMasterSwap()
        {
            if (!isMaster && Networking.IsMaster)
            {
                Debug.LogWarning("checkMasterSwap Called");
                isMaster = true;
                SendCustomEventDelayedFrames(nameof(checkPlayerAssign), 5);
            }
        }


        #region allocation
        public void allocate(VRCPlayerApi player)
        {
            for (int x = 0; x < assigners.Length; x++)
            {
                if (assigners[x].playerId == -1)
                {
                    assigners[x].attachPlayer(player);
                    break;
                }
            }
        }

        public void checkPlayerAssign()
        {
            if (!Networking.IsMaster) return;
            players = VRCPlayerApi.GetPlayers(players);
            for (int x = 0; x < players.Length; x++)
            {
                if (players[x] != null && players[x].displayName != null)
                {
                    bool found = false;
                    for (int y = 0; y < assigners.Length; y++)
                    {
                        if (assigners[y].playerId == players[x].playerId)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) allocate(players[x]);
                }
            }

        }
        #endregion

    }
}