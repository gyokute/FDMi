using FDMi.core.Editor.Application.UseCases;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 装置の区間配列を、親の翼の区間数にそろえる。装置は翼の区間ごとに値を出すので、
    // 長さが違うと FDMiWing が読む時に添字が外れる。値は残す (伸びた分は 0 = 効きなし)。
    // 翼の特定は親 (GetComponentInParent) で行う。名前空間の範囲では複数の翼を区別できない。
    public class FDMiControlSurfaceAutoSetupUseCase : IFDMiAutoSetupUseCase
    {
        public void Execute(UnityEngine.Object target)
        {
            FDMiControlSurface device = target as FDMiControlSurface;
            if (device == null)
                return;

            FDMiWing wing = device.GetComponentInParent<FDMiWing>(true);
            if (wing == null || wing.coordPos == null)
                return;

            SerializedObject so = new SerializedObject(device);
            int count = wing.coordPos.Length;
            SetSize(so.FindProperty(nameof(FDMiControlSurface.sectionMul)), count);
            SetSize(so.FindProperty(nameof(FDMiControlSurface.alpha)), count);
            SetSize(so.FindProperty(nameof(FDMiControlSurface.cl)), count);
            SetSize(so.FindProperty(nameof(FDMiControlSurface.cd)), count);
            SetSize(so.FindProperty(nameof(FDMiControlSurface.scale)), count);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void ExecuteAll()
        {
            FDMiControlSurface[] devices = UnityEngine.Object.FindObjectsByType<FDMiControlSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < devices.Length; i++)
                Execute(devices[i]);
        }

        // 長さが同じなら触らない (Inspector を開くたびに配列を作り直さない)。
        static void SetSize(SerializedProperty property, int count)
        {
            if (property.arraySize != count)
                property.arraySize = count;
        }
    }
}
