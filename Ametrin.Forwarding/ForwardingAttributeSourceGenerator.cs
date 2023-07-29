using Microsoft.CodeAnalysis;

namespace ForwardingSourceGenerator;

//[Generator]
//internal sealed class ForwardingAttributeSourceGenerator : ISourceGenerator {
//    public void Initialize(GeneratorInitializationContext context) {}
//    public void Execute(GeneratorExecutionContext context) {
//        var @namespace = context.Compilation.AssemblyName;

//        context.AddSource("ForwardAttribute.g.cs", $$"""
//                namespace {{@namespace}};

//                [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
//                public sealed class {{ForwardingHelper.FORWARD_ATTRIBUTE_NAME}} : Attribute
//                {
//                    //public string[] MemberNames { get; }

//                    public {{ForwardingHelper.FORWARD_ATTRIBUTE_NAME}}(string memberNames, string sfds){
//                        //MemberNames = memberNames;
//                    }
//                }
//            """);

//        context.AddSource("ForwardingAttribute.g.cs", $$"""
//                namespace {{@namespace}};

//                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
//                public sealed class {{ForwardingHelper.FORWARDING_ATTRIBUTE_NAME}} : Attribute {}
//            """);
//    }
//}
