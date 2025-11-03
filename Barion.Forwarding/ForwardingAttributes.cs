// using System;

// namespace Barion.Forwarding;

// [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
// public sealed class ForwardingAttribute : Attribute { }

// [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
// public sealed class ForwardAttribute : Attribute
// {
//     public ForwardAttribute(params string[] memberNames) { }
// }

// [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
// public sealed class ForwardMethodsAttribute : Attribute
// {
//     public ForwardMethodsAttribute(params string[] methodNames) { }
// }

// [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
// public sealed class ForwardPropertiesAttribute : Attribute
// {
//     public ForwardPropertiesAttribute(bool includeSetter, params string[] propertyNames) { }
// }