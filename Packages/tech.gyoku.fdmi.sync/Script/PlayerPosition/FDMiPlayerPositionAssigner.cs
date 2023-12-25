
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer;

namespace tech.gyoku.FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiPlayerPositionAssigner : UdonSharpBehaviour
    {
        public FDMiPlayerSyncManager playerSyncMan;
        public FDMiPlayerPosition playerPosition;
        [UdonSynced, FieldChangeCallback(nameof(playerId)), OdinSerialize] private int _playerId = -1;
        public int playerId
        {
            get => _playerId;
            set => playerAttached(value);
        }
        VRCPlayerApi assignedPlayer;


        #region event
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Networking.IsMaster) return;
            if (player.playerId == playerId) dettachPlayer();
        }

        public override bool OnOwnershipRequest(VRCPlayerApi p, VRCPlayerApi ro)
        {
            return false;
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            SendCustomEventDelayedSeconds(nameof(delayPlayerIntegrityTest), 0.5f);
        }
        #endregion
        public void delayPlayerIntegrityTest()
        {
            if (!Networking.IsMaster || playerId == -1) return;
            if (playerId != Networking.GetOwner(playerPosition.gameObject).playerId)
                dettachPlayer();
        }

        public void dettachPlayer()
        {
            playerId = -1;
            RequestSerialization();
        }
        public void attachPlayer(VRCPlayerApi player)
        {
            playerId = player.playerId;
            Networking.SetOwner(player, playerPosition.gameObject);
            SendCustomEventDelayedSeconds(nameof(delayCheckSeating), 3f);
            RequestSerialization();
        }

        void delayCheckSeating()
        {
            if (_playerId != Networking.LocalPlayer.playerId) return;
            if (!playerPosition._inVehicle) playerPosition.useSeat();
        }
        void playerAttached(int pId)
        {
            _playerId = pId;
            if (pId == -1)
            {
                assignedPlayer = null;
                playerPosition.attachPlayer(null);
                return;
            }
            assignedPlayer = VRCPlayerApi.GetPlayerById(pId);
            playerPosition.attachPlayer(assignedPlayer);
            TestPlayerIntegrity();
        }

        #region integrity
        public bool isPlayerExisting()
        {
            if (playerId == -1) return true;
            return VRCPlayerApi.GetPlayerById(playerId) != null;
        }

        public void TestPlayerIntegrity()
        {
            if (playerId != Networking.LocalPlayer.playerId) return;
            // if playerId is confused, check all player assign. maybe less-occur accident.
            if (!playerPosition.Player.isLocal)
            {
                playerSyncMan.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "checkPlayerAssign");
                return;
            }
            // If playerPosition Owner is confused, fix automatically.
            if (!Networking.IsOwner(playerPosition.gameObject))
            {
                Debug.Log("FDMi>sync>player: autofix object owner.");
                Networking.SetOwner(Networking.LocalPlayer, playerPosition.gameObject);
            }
            // if player is not sit in any station, sit down.
            // playerPosition.OnStationExited(Networking.LocalPlayer);
            playerPosition.gameObject.SetActive(true);
            SendCustomEventDelayedSeconds(nameof(TestPlayerIntegrity), 5f);
        }
        #endregion

    }
}