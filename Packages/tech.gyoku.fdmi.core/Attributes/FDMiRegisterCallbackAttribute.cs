using System;

namespace FDMi.core
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public sealed class FDMiRegisterCallbackAttribute : Attribute
    {
        public readonly string FunctionName;
        public FDMiRegisterCallbackAttribute(string functionName) => FunctionName = functionName;
    }
}
