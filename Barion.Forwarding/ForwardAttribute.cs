using System;

namespace Barion.Forwarding;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class ForwardAttribute : Attribute {
    public ForwardAttribute(params string[] memberNames) { }
}