using System.Collections.Generic;
using FDMi.core.Editor.Application.UseCases;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // FDMiWing の配線を機体 (名前空間ルート) から自動で解決する。
    //   body           : 機体の Rigidbody (階層パス順の先頭)
    //   bodyAero       : 機体の FDMiBodyAero
    //   controlSurface : この翼の子の FDMiControlSurface
    // 手で並べた controlSurface は Inspector を開いた時に上書きされる (core の自動解決と同じ仕様)。
    // 併せて装置の区間配列もそろえる。装置の配列が空のまま実行されると翼が読む時に添字が外れるため、
    // 翼を選べば翼の装置が全部使える状態にする。
    //
    // sourceWing は入れない: どの翼が上流かは設計判断で、機体の構成からは決まらない
    // (垂直尾翼には入れてはならない。FDMiWing の地面効果と吹き下ろしは水平翼が前提)。
    public class FDMiWingAutoSetupUseCase : IFDMiAutoSetupUseCase
    {
        public void Execute(UnityEngine.Object target)
        {
            FDMiWing wing = target as FDMiWing;
            if (wing == null)
                return;

            SerializedObject so = new SerializedObject(wing);
            List<Rigidbody> bodies = FDMiAircraftScope.FindAll<Rigidbody>(wing);
            if (bodies.Count > 0)
                so.FindProperty(nameof(FDMiWing.body)).objectReferenceValue = bodies[0];
            List<FDMiBodyAero> bodyAeros = FDMiAircraftScope.FindAll<FDMiBodyAero>(wing);
            if (bodyAeros.Count > 0)
                so.FindProperty(nameof(FDMiWing.bodyAero)).objectReferenceValue = bodyAeros[0];

            FDMiControlSurface[] devices = wing.GetComponentsInChildren<FDMiControlSurface>(true);
            SerializedProperty list = so.FindProperty(nameof(FDMiWing.controlSurface));
            list.arraySize = devices.Length;
            for (int i = 0; i < devices.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = devices[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            FDMiControlSurfaceAutoSetupUseCase deviceUseCase = new FDMiControlSurfaceAutoSetupUseCase();
            for (int i = 0; i < devices.Length; i++)
                deviceUseCase.Execute(devices[i]);
        }

        public void ExecuteAll()
        {
            FDMiWing[] wings = UnityEngine.Object.FindObjectsByType<FDMiWing>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < wings.Length; i++)
                Execute(wings[i]);
        }
    }
}
