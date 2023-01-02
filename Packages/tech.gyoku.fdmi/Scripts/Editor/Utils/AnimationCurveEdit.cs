using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace Saccflight_FDMi.Editor
{
    public static class AnimationCurveUtils
    {
        public static AnimationCurve LoadCSVToAnimationCurve()
        {
            List<Keyframe> keys = new List<Keyframe>();
            var path = EditorUtility.OpenFilePanel("Select CSV", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(path))
                return new AnimationCurve();
            string[] list = File.ReadAllLines(path);
            foreach (string li in list)
            {
                string[] col = li.Split(',');
                if (col.Length > 1)
                {
                    keys.Add(new Keyframe(float.Parse(col[0]), float.Parse(col[1])));
                }
            }
            return new AnimationCurve(keys.ToArray());
        }
    }
}
