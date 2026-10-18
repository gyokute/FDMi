using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor
{
    public class FDMiSyncAutoSetup : IFDMiSyncAutoSetup
    {
        public void Execute(Object target)
        {
            AutoWire(target as Component);
        }

        public void ExecuteAll()
        {
            FDMiEcefOrigin[] origins = Object.FindObjectsOfType<FDMiEcefOrigin>(true);
            for (int i = 0; i < origins.Length; i++)
                AutoWire(origins[i] as Component);
            FDMiGeodeticSyncManager[] managers = Object.FindObjectsOfType<FDMiGeodeticSyncManager>(true);
            for (int i = 0; i < managers.Length; i++)
                AutoWire(managers[i] as Component);
            FDMiGeodeticObjectSync[] syncs = Object.FindObjectsOfType<FDMiGeodeticObjectSync>(true);
            for (int i = 0; i < syncs.Length; i++)
                AutoWire(syncs[i]);
            FDMiGeodeticPlayerPosition[] players = Object.FindObjectsOfType<FDMiGeodeticPlayerPosition>(true);
            for (int i = 0; i < players.Length; i++)
                AutoWire(players[i]);
            FDMiGeodeticPlayerPositionAssigner[] assigners = Object.FindObjectsOfType<FDMiGeodeticPlayerPositionAssigner>(true);
            for (int i = 0; i < assigners.Length; i++)
                AutoWire(assigners[i]);
            FDMiSeatAnchor[] seats = Object.FindObjectsOfType<FDMiSeatAnchor>(true);
            for (int i = 0; i < seats.Length; i++)
                AutoWire(seats[i]);
            FDMiSaccBridge[] bridges = Object.FindObjectsOfType<FDMiSaccBridge>(true);
            for (int i = 0; i < bridges.Length; i++)
                AutoWire(bridges[i]);
        }

        static void AutoWire(Component component)
        {
            if (!component)
                return;
            FDMiEcefOrigin origin = component as FDMiEcefOrigin;
            if (origin)
            {
                origin.Bake();
                EditorUtility.SetDirty(origin);
                return;
            }
            FDMiEcefOrigin shared = FirstOrigin();
            FDMiGeodeticSyncManager manager = component as FDMiGeodeticSyncManager;
            if (manager)
            {
                // 自身が参照点として syncManager を持つ(V2 RelativeSyncManagerEditor と同じ自己配線)。
                // ここが null のままだと root の windup/getViewPosition が NRE で落ちる。
                if (!manager.syncManager)
                    manager.syncManager = manager;
                if (!manager.origin)
                    manager.origin = shared;
                // 参照点の追加・削除に追従するため毎回再収集する(条件付きだと増設が漏れる)
                manager.refPoints = Object.FindObjectsOfType<FDMiGeodeticReferencePoint>(true);
                EditorUtility.SetDirty(manager);
                return;
            }
            FDMiGeodeticObjectSync sync = component as FDMiGeodeticObjectSync;
            if (sync)
            {
                if (!sync.syncManager)
                    sync.syncManager = FirstManager();
                if (!sync.body)
                {
                    sync.body = component.GetComponent<Rigidbody>();
                    if (!sync.body)
                        sync.body = component.GetComponentInChildren<Rigidbody>(true);
                }
                // target は物理根に追従させる(旧配線で root 固定になっていた場合も修正する)
                sync.target = sync.body ? sync.body.transform : sync.transform;
                EditorUtility.SetDirty(sync);
                return;
            }
            FDMiGeodeticPlayerPosition player = component as FDMiGeodeticPlayerPosition;
            if (player)
            {
                if (!player.syncManager)
                    player.syncManager = FirstManager();
                if (!player.station)
                    player.station = component.GetComponent<VRC.SDK3.Components.VRCStation>();
                EditorUtility.SetDirty(player);
                return;
            }
            FDMiGeodeticPlayerPositionAssigner assigner = component as FDMiGeodeticPlayerPositionAssigner;
            if (assigner)
            {
                if (!assigner.playerSyncMan)
                    assigner.playerSyncMan = component.GetComponentInParent<FDMiGeodeticPlayerSyncManager>();
                if (!assigner.playerPosition)
                    assigner.playerPosition = component.GetComponent<FDMiGeodeticPlayerPosition>();
                EditorUtility.SetDirty(assigner);
                return;
            }
            FDMiSeatAnchor seat = component as FDMiSeatAnchor;
            if (seat)
            {
                if (!seat.vehicleSync)
                    seat.vehicleSync = component.GetComponentInParent<FDMiGeodeticObjectSync>();
                if (!seat.syncManager)
                    seat.syncManager = FirstManager();
                if (!seat.seatView)
                {
                    VRC.SDK3.Components.VRCStation station = component.GetComponent<VRC.SDK3.Components.VRCStation>();
                    if (station)
                        seat.seatView = station.stationEnterPlayerLocation;
                }
                EditorUtility.SetDirty(seat);
                return;
            }
            FDMiSaccBridge bridge = component as FDMiSaccBridge;
            if (bridge)
            {
                if (!bridge.vehicleSync)
                    bridge.vehicleSync = component.GetComponent<FDMiGeodeticObjectSync>();
                if (!bridge.EntityControl)
                {
                    UdonSharp.UdonSharpBehaviour entity = component.GetComponent<UdonSharp.UdonSharpBehaviour>();
                    if (entity && entity.GetUdonTypeName() == "SaccFlightAndVehicles.SaccEntity")
                        bridge.EntityControl = entity;
                }
                EditorUtility.SetDirty(bridge);
            }
        }

        static FDMiEcefOrigin FirstOrigin()
        {
            FDMiEcefOrigin[] origins = Object.FindObjectsOfType<FDMiEcefOrigin>(true);
            if (origins.Length > 0)
                return origins[0];
            return null;
        }

        static FDMiGeodeticSyncManager FirstManager()
        {
            FDMiGeodeticSyncManager[] managers = Object.FindObjectsOfType<FDMiGeodeticSyncManager>(true);
            if (managers.Length > 0)
                return managers[0];
            return null;
        }
    }
}
