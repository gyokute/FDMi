using FDMi.core;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    /// <summary>マスターが全プレイヤーに FDMiGeodeticPlayerPositionAssigner を割り当てる。</summary>
    public class FDMiGeodeticPlayerSyncManager : FDMiBehaviour
    {
        public FDMiGeodeticPlayerPositionAssigner[] assigners;
        VRCPlayerApi[] players = new VRCPlayerApi[80];
        public bool isMaster = false;

        #region lifecycle
        void Start()
        {
            assigners = gameObject.GetComponentsInChildren<FDMiGeodeticPlayerPositionAssigner>();
        }
        #endregion

        #region events
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) return;
            checkMasterSwap();
            allocate(player);
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return false;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            SendCustomEventDelayedSeconds(nameof(checkMasterSwap), 1f);
        }
        #endregion

        private void checkMasterSwap()
        {
            // 真偽を毎回代入する(譲渡で master を失った後も再取得で検証が再開する)
            isMaster = Networking.IsMaster;
            if (isMaster)
                SendCustomEventDelayedFrames(nameof(checkPlayerAssign), 5);
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
