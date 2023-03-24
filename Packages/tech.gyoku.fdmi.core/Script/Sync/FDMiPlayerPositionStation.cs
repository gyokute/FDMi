
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    /// <summary>
    /// To solve where each player is. 
    /// Automatically sit in seat and solve position in every frame.   
    /// </summary>

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiPlayerPositionStation : UdonSharpBehaviour
    {
        [UdonSynced, FieldChangeCallback(nameof(inVehicle))] public bool _inVehicle;
        [UdonSynced, FieldChangeCallback(nameof(assigned))] public bool _assigned;
        public bool inVehicle
        {
            get => inVehicle;
            set => inVehicle = value;
        }
        public bool assigned
        {
            get => assigned;
            set => assigned = value;
        }

        public VRCStation station;
        public FDMiPlayerPositionManager posManager;
        VRCPlayerApi seatedPlayer;
        int playerId;

        #region lifecycle
        void Start()
        {
            gameObject.SetActive(false);
        }
        void Update()
        {

        }
        #endregion
        #region event
        public override void OnPlayerRespawn(VRCPlayerApi player) { }
        public override void OnOwnershipTransferred(VRCPlayerApi player) { }
        public override void OnStationEntered(VRCPlayerApi player) { }
        public override void OnStationExited(VRCPlayerApi player) { }


        #endregion
        #region registration
        public void registrate(VRCPlayerApi player)
        {
            Networking.SetOwner(player, gameObject);
            seatedPlayer = player;
            station.UseStation(player);
            playerId = player.playerId;
            gameObject.SetActive(true);
        }
        public void unregistrate()
        {
            gameObject.SetActive(false);
            if (seatedPlayer != null)
                station.ExitStation(seatedPlayer);
            seatedPlayer = null;
            playerId = 0;
        }
        #endregion
        #region coherency maintainance
        public void maintainSeatedPlayer(int playerId)
        {
            if (seatedPlayer.playerId != playerId)
            {
                unregistrate();
                if (playerId > 0) registrate(VRCPlayerApi.GetPlayerById(playerId));
            }
        }
        #endregion 
    }

}
