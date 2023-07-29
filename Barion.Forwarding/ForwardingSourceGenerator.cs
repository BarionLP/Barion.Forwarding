using Barion.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Barion.Forwarding;

[Generator]
internal sealed class ForwardingSourceGenerator : ISourceGenerator {
    //private static string[] SymbolNameBlacklist = { };
    public const string FORWARDING_ATTRIBUTE_NAME = "ForwardingAttribute";
    public const string FORWARD_ATTRIBUTE_NAME = "ForwardAttribute";

    public void Initialize(GeneratorInitializationContext context) {
        //#if DEBUG
        //if(!Debugger.IsAttached) {
        //    Debugger.Launch();
        //}
        //#endif

        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context) {
        if(context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

        foreach(var typeDeclaration in receiver.CandidateTypes) {
            var model = context.Compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
            if(model.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol classSymbol) continue;
            if(!classSymbol.GetAttributes().Any(attr => attr.AttributeClass!.Name == FORWARDING_ATTRIBUTE_NAME)) continue;

            var builder = new ClassBuilder(classSymbol, classSymbol.ContainingNamespace);


            foreach(var memberSymbol in classSymbol.GetAllMembers().Where(m => m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property)) {
                var forwardAttribute = memberSymbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass!.Name == FORWARD_ATTRIBUTE_NAME);
                if(forwardAttribute is null) continue;
                var specifiedMembers = forwardAttribute.ConstructorArguments[0].Values.Select(value => value.Value as string).ToArray();
                var members = (memberSymbol is IFieldSymbol fieldSymbol ? fieldSymbol.Type : ((IPropertySymbol) memberSymbol).Type).GetMembers().Where(CanSymbolBeForwarded);
                var membersToGenerate = specifiedMembers.Length == 0 ? members/*.Where(member => !SymbolNameBlacklist.Contains(member.Name))*/ : members.Where(member => specifiedMembers.Contains(member.Name));

                foreach(var subMember in membersToGenerate) {
                    builder.Forward(subMember, memberSymbol);
                }
            }
            builder.Close();

            context.AddSource($"{builder.Name}.g.cs", builder.ToString());
        }

        return;

        static bool CanSymbolBeForwarded(ISymbol symbol) {
            return !symbol.IsStatic &&
                symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal &&
                !(symbol.Name.StartsWith("set_") || symbol.Name.StartsWith("get_")) &&
                symbol.Name != ".ctor";
        }
    }

    private class SyntaxReceiver : ISyntaxContextReceiver {
        public List<TypeDeclarationSyntax> CandidateTypes { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context) {
            if(context.Node is ClassDeclarationSyntax or StructDeclarationSyntax) {
                CandidateTypes.Add(context.Node as TypeDeclarationSyntax);
            }
        }
    }
}

