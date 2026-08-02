using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    /// <summary>
    /// VRCContactReceiverの設定を補助する。
    /// - Root TransformをFDMiTransformRefから取得する。
    /// - Position, Rotationを、Rootと設定用Transformの位置の差分より自動的に求める。
    /// </summary>
    [RequireComponent(typeof(VRCContactReceiver))]
    public class FDMiContactReceiverHelper : MonoBehaviour
    {
        public string transformRefPath;

        [FDMiDataPath(nameof(transformRefPath))]
        public FDMiTransformRef rootTransformRef;
    }
}
