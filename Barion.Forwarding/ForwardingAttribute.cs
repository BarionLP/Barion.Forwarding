using System;

namespace Barion.Forwarding;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ForwardingAttribute : Attribute { }