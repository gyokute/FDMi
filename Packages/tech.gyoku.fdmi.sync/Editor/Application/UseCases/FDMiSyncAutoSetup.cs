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
            FDMiEcefSync[] syncs = Object.FindObjectsOfType<FDMiEcefSync>(true);
            for (int i = 0; i < syncs.Length; i++)
                AutoWire(syncs[i]);
            FDMiSeatAnchor[] seats = Object.FindObjectsOfType<FDMiSeatAnchor>(true);
            for (int i = 0; i < seats.Length; i++)
                AutoWire(seats[i]);
            FDMiSaccBridge[] bridges = Object.FindObjectsOfType<FDMiSaccBridge>(true);
            for (int i = 0; i < bridges.Length; i++)
                AutoWire(bridges[i]);
            FDMiPlayerAnchor[] anchors = Object.FindObjectsOfType<FDMiPlayerAnchor>(true);
            for (int i = 0; i < anchors.Length; i++)
                AutoWire(anchors[i]);
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
            FDMiEcefOrigin shared = FirstOrigin(component);
            FDMiEcefSync sync = component as FDMiEcefSync;
            if (sync)
            {
                if (!sync.origin)
                    sync.origin = shared;
                if (!sync.target)
                    sync.target = sync.transform;
                EditorUtility.SetDirty(sync);
                return;
            }
            FDMiSeatAnchor seat = component as FDMiSeatAnchor;
            if (seat)
            {
                if (!seat.vehicleSync)
                    seat.vehicleSync = component.GetComponentInParent<FDMiEcefSync>();
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
                    bridge.vehicleSync = component.GetComponent<FDMiEcefSync>();
                if (!bridge.EntityControl)
                {
                    UdonSharp.UdonSharpBehaviour entity = component.GetComponent<UdonSharp.UdonSharpBehaviour>();
                    if (entity && entity.GetUdonTypeName() == "SaccFlightAndVehicles.SaccEntity")
                        bridge.EntityControl = entity;
                }
                EditorUtility.SetDirty(bridge);
                return;
            }
            FDMiPlayerAnchor anchor = component as FDMiPlayerAnchor;
            if (anchor)
            {
                if (!anchor.origin)
                    anchor.origin = shared;
                EditorUtility.SetDirty(anchor);
            }
        }

        static FDMiEcefOrigin FirstOrigin(Component component)
        {
            FDMiEcefOrigin[] origins = Object.FindObjectsOfType<FDMiEcefOrigin>(true);
            if (origins.Length > 0)
                return origins[0];
            return null;
        }
    }
}
