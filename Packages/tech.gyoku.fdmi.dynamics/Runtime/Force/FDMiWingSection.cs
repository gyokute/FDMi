using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 翼断面の定義。コード長のみ持つ。Editor が両端断面から平面形を焼き付ける。
    public class FDMiWingSection : FDMiBehaviour
    {
        [Min(0.01f)]
        public float chordLength = 2f; // コード長 [m]
    }
}
