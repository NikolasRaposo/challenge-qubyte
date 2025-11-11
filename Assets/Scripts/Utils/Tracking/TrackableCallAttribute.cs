using System;

namespace Qubyte.Tracking
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class TrackableCallAttribute : Attribute
    {
        public string DisplayName { get; }
        public TrackableCallAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}