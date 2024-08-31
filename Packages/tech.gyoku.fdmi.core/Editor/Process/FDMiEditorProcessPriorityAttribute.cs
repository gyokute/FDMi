
using System;
using UnityEngine;
using UnityEditor;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FDMiEditorProcessPriorityAttribute : System.Attribute
    {
        public int priority;
        public FDMiEditorProcessPriorityAttribute(int priority) { this.priority = priority; }
    }
}