using Barion.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Barion.Forwarding;

[Generator]
internal sealed class ForwardingSourceGenerator : ISourceGenerator {
    private static string[] SymbolNameBlacklist = { "GetType" };
    public const string FORWARDING_ATTRIBUTE_NAME = "ForwardingAttribute";
    public const string FORWARD_ATTRIBUTE_NAME = "ForwardAttribute";
    public const string FORWARD_METHODS_ATTRIBUTE_NAME = "ForwardMethodsAttribute";
    public const string FORWARD_PROPERTIES_ATTRIBUTE_NAME = "ForwardPropertiesAttribute";

    public void Initialize(GeneratorInitializationContext context) {
//#if DEBUG
//        if(!Debugger.IsAttached) {
//            Debugger.Launch();
//        }
//#endif

        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context) {
        if(context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

        foreach(var typeDeclaration in receiver.CandidateTypes) {
            var model = context.Compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
            if(model.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol classSymbol) continue;
            if(!classSymbol.HasAttribute(FORWARDING_ATTRIBUTE_NAME)) continue;

            var classSubMembers = classSymbol.BaseType!.GetAllMembers();
            var builder = new ClassBuilder(classSymbol, classSymbol.ContainingNamespace);


            foreach(var classMemberSymbol in classSymbol.GetMembers(SymbolKind.Field, SymbolKind.Property)) {
                var type = classMemberSymbol is IFieldSymbol fieldSymbol ? fieldSymbol.Type : ((IPropertySymbol) classMemberSymbol).Type;
                var allMembers = type.GetAllMembers().Where(CanSymbolBeForwarded);
                var allMethods = allMembers.Where(symbol => symbol is IMethodSymbol).Cast<IMethodSymbol>();
                var allProperties = allMembers.Where(symbol => symbol is IPropertySymbol).Cast<IPropertySymbol>();
                
                var forwardPropertiesAttribute = classMemberSymbol.GetAttribute(FORWARD_PROPERTIES_ATTRIBUTE_NAME);
                if(forwardPropertiesAttribute is not null) ForwardProperties(forwardPropertiesAttribute);

                var forwardMethodsAttribute = classMemberSymbol.GetAttribute(FORWARD_METHODS_ATTRIBUTE_NAME);
                if(forwardMethodsAttribute is not null) ForwardMethods(forwardMethodsAttribute);
                
                var forwardAttribute = classMemberSymbol.GetAttribute(FORWARD_ATTRIBUTE_NAME);
                if(forwardAttribute is not null) Forward(forwardAttribute);                

                void Forward(AttributeData attribute) {
                    var specifiedMemberNames = attribute.ConstructorArguments[0].Values.Select(value => (string)value.Value!).ToArray();
                    ForwardPropertiesInternal(GetProperties(specifiedMemberNames));
                    ForwardMethodsInternal(GetMethods(specifiedMemberNames));
                }
                
                void ForwardMethods(AttributeData attribute) {
                    var specifiedMemberNames = attribute.ConstructorArguments[0].Values.Select(value => (string)value.Value!).ToArray();
                    ForwardMethodsInternal(GetMethods(specifiedMemberNames));
                }
                
                void ForwardProperties(AttributeData attribute) {
                    var includeSetter = (bool) attribute.ConstructorArguments[0].Value!;
                    var specifiedMemberNames = attribute.ConstructorArguments[1].Values.Select(value => (string)value.Value!).ToArray();

                    ForwardPropertiesInternal(GetProperties(specifiedMemberNames), includeSetter);
                }

                IEnumerable<IMethodSymbol> GetMethods(string[] whitelist) {
                    if(whitelist.Length == 0) return allMethods.Where(member => !SymbolNameBlacklist.Contains(member.Name));

                    return allMethods.Where(member => whitelist.Contains(member.Name));
                }
                
                IEnumerable<IPropertySymbol> GetProperties(string[] whitelist) {
                    if(whitelist.Length == 0) return allProperties.Where(member => !SymbolNameBlacklist.Contains(member.Name));

                    return allProperties.Where(member => whitelist.Contains(member.Name));
                }

                void ForwardMethodsInternal(IEnumerable<IMethodSymbol> methodSymbols) {
                    var forwarded = new List<IMethodSymbol>();
                    foreach(var methodSymbol in methodSymbols) {
                        var ignoredSymbol = classSubMembers.FirstOrDefault(symbol => symbol.Name == methodSymbol.Name);
                        var shouldOverride = ignoredSymbol is not null;

                        if(forwarded.Any(other => methodSymbol.Name == other.Name && methodSymbol.MatchParameters(other) && methodSymbol.ReturnType.Equals(other.ReturnType, SymbolEqualityComparer.Default))) continue;

                        if(shouldOverride && ignoredSymbol is not IMethodSymbol) {
                            builder.Add($"//cannot override because base.{ignoredSymbol!.Name} is not a method");
                            shouldOverride = false;
                        }

                        if(shouldOverride && !(ignoredSymbol!.IsVirtual || ignoredSymbol.IsOverride)) {
                            builder.Add($"//cannot override because base.{ignoredSymbol.Name} is not virtual");
                            shouldOverride = false;
                        }

                        if(shouldOverride && ignoredSymbol is IMethodSymbol ignoredMethod && !methodSymbol.MatchParameters(ignoredMethod)) {
                            builder.Add($"//cannot override base.{ignoredSymbol!.Name} because of differend signatures");
                            shouldOverride = false;
                        }


                        builder.ForwardMethod(methodSymbol, classMemberSymbol, shouldOverride);
                        forwarded.Add(methodSymbol);
                    }
                }
                
                void ForwardPropertiesInternal(IEnumerable<IPropertySymbol> propertySymbols, bool includeSetter = false) {
                    foreach(var propertySymbol in propertySymbols) {
                        var mightIgnoreMember = classSubMembers.FirstOrDefault(symbol => symbol.Name == propertySymbol.Name);
                        var shouldOverride = mightIgnoreMember is not null;

                        if(shouldOverride && mightIgnoreMember is not IPropertySymbol) {
                            builder.Add($"//cannot override because base.{mightIgnoreMember!.Name} is not a property");
                            shouldOverride = false;
                        }
                        
                        if(shouldOverride && !mightIgnoreMember!.IsVirtual) {
                            builder.Add($"//cannot override because base.{mightIgnoreMember.Name} is not virtual");
                            shouldOverride = false;
                        }

                        builder.ForwardProperty(propertySymbol, classMemberSymbol, includeSetter, shouldOverride);
                    }
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

