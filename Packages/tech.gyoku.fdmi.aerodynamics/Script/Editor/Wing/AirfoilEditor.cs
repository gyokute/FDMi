using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using tech.gyoku.FDMi.v2.aerodynamics;

namespace tech.gyoku.FDMi.v2.aerodynamics.Editor
{
    [CustomEditor(typeof(AirFoil), true)]
    public class AirfoilEditor : UnityEditor.Editor
    {

    }
}