using Microsoft.CodeAnalysis;

namespace Barion.Forwarding;

[Generator]
internal sealed class ForwardingAttributeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource("ForwardingAttributes.g.cs", """
            using System;

            namespace Barion.Forwarding;

            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
            internal sealed class ForwardingAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
            internal sealed class ForwardAttribute : Attribute
            {
                internal ForwardAttribute(params string[] memberNames) { }
            }

            [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
            internal sealed class ForwardMethodsAttribute : Attribute
            {
                internal ForwardMethodsAttribute(params string[] methodNames) { }
            }

            [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
            internal sealed class ForwardPropertiesAttribute : Attribute
            {
                internal ForwardPropertiesAttribute(bool includeSetter, params string[] propertyNames) { }
            }
            """);
        });
    }
}
