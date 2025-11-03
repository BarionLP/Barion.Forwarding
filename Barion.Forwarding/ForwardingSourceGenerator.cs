using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Barion.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Barion.Forwarding;

[Generator]
internal sealed class ForwardingSourceGenerator : IIncrementalGenerator
{
    private static readonly string[] SymbolNameBlacklist = ["GetType"];
    public const string FORWARDING_ATTRIBUTE_NAME = "ForwardingAttribute";
    public const string FORWARD_ATTRIBUTE_NAME = "ForwardAttribute";
    public const string FORWARD_METHODS_ATTRIBUTE_NAME = "ForwardMethodsAttribute";
    public const string FORWARD_PROPERTIES_ATTRIBUTE_NAME = "ForwardPropertiesAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //#if DEBUG
        //        if (!Debugger.IsAttached)
        //        {
        //            Debugger.Launch();
        //        }
        //#endif

        var nodes = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
            static (ctx, token) => (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, token) as INamedTypeSymbol)!
        ).Where(c => c.HasAttribute(FORWARDING_ATTRIBUTE_NAME))
        .Select((c, _) => (type: c, members: c.GetMembers(SymbolKind.Field, SymbolKind.Property).Where(m => m.GetAttributes().Any(a => a.AttributeClass!.Name is FORWARD_ATTRIBUTE_NAME or FORWARD_METHODS_ATTRIBUTE_NAME or FORWARD_PROPERTIES_ATTRIBUTE_NAME))))
        .Where(p => p.members.Any());

        context.RegisterSourceOutput(nodes, Execute);
    }

    public void Execute(SourceProductionContext output, (INamedTypeSymbol type, IEnumerable<ISymbol> members) node)
    {
        var (classSymbol, members) = node;

        var classSubMembers = classSymbol.BaseType!.GetAllMembers();
        var builder = new ClassBuilder(classSymbol, classSymbol.ContainingNamespace);

        foreach (var classMemberSymbol in members)
        {
            var type = classMemberSymbol is IFieldSymbol fieldSymbol ? fieldSymbol.Type : ((IPropertySymbol)classMemberSymbol).Type;
            var allMembers = type.GetAllMembers().Where(CanSymbolBeForwarded);
            var allMethods = allMembers.OfType<IMethodSymbol>();
            var allProperties = allMembers.OfType<IPropertySymbol>();

            var forwardPropertiesAttribute = classMemberSymbol.GetAttribute(FORWARD_PROPERTIES_ATTRIBUTE_NAME);
            if (forwardPropertiesAttribute is not null) ForwardProperties(forwardPropertiesAttribute);

            var forwardMethodsAttribute = classMemberSymbol.GetAttribute(FORWARD_METHODS_ATTRIBUTE_NAME);
            if (forwardMethodsAttribute is not null) ForwardMethods(forwardMethodsAttribute);

            var forwardAttribute = classMemberSymbol.GetAttribute(FORWARD_ATTRIBUTE_NAME);
            if (forwardAttribute is not null) Forward(forwardAttribute);

            void Forward(AttributeData attribute)
            {
                var specifiedMemberNames = attribute.ConstructorArguments[0].Values.Select(value => (string)value.Value!).ToImmutableArray();
                ForwardPropertiesInternal(GetProperties(specifiedMemberNames));
                ForwardMethodsInternal(GetMethods(specifiedMemberNames));
            }

            void ForwardMethods(AttributeData attribute)
            {
                var specifiedMemberNames = attribute.ConstructorArguments[0].Values.Select(value => (string)value.Value!).ToImmutableArray();
                ForwardMethodsInternal(GetMethods(specifiedMemberNames));
            }

            void ForwardProperties(AttributeData attribute)
            {
                var includeSetter = (bool)attribute.ConstructorArguments[0].Value!;
                var specifiedMemberNames = attribute.ConstructorArguments[1].Values.Select(value => (string)value.Value!).ToImmutableArray();

                ForwardPropertiesInternal(GetProperties(specifiedMemberNames), includeSetter);
            }

            IEnumerable<IMethodSymbol> GetMethods(ImmutableArray<string> whitelist)
            {
                if (whitelist.Length == 0) return allMethods.Where(member => !SymbolNameBlacklist.Contains(member.Name));

                return allMethods.Where(member => whitelist.Contains(member.Name));
            }

            IEnumerable<IPropertySymbol> GetProperties(ImmutableArray<string> whitelist)
            {
                if (whitelist.Length == 0) return allProperties.Where(member => !SymbolNameBlacklist.Contains(member.Name));

                return allProperties.Where(member => whitelist.Contains(member.Name));
            }

            void ForwardMethodsInternal(IEnumerable<IMethodSymbol> methodSymbols)
            {
                var forwarded = new List<IMethodSymbol>();
                foreach (var methodSymbol in methodSymbols)
                {
                    var ignoredSymbol = classSubMembers.FirstOrDefault(symbol => symbol.Name == methodSymbol.Name);
                    var shouldOverride = ignoredSymbol is not null;

                    if (forwarded.Any(other => methodSymbol.Name == other.Name && methodSymbol.MatchParameters(other) && methodSymbol.ReturnType.Equals(other.ReturnType, SymbolEqualityComparer.Default))) continue;

                    if (shouldOverride && classMemberSymbol.IsStatic)
                    {
                        shouldOverride = false;
                    }

                    if (shouldOverride && ignoredSymbol is not IMethodSymbol)
                    {
                        builder.Add($"// cannot override because base.{ignoredSymbol!.Name} is not a method");
                        shouldOverride = false;
                    }

                    if (shouldOverride && !(ignoredSymbol!.IsVirtual || ignoredSymbol.IsOverride))
                    {
                        builder.Add($"// cannot override because base.{ignoredSymbol.Name} is not virtual");
                        shouldOverride = false;
                    }

                    if (shouldOverride && ignoredSymbol is IMethodSymbol ignoredMethod && !methodSymbol.MatchParameters(ignoredMethod))
                    {
                        builder.Add($"// cannot override base.{ignoredSymbol!.Name} because of differend signatures");
                        shouldOverride = false;
                    }


                    builder.ForwardMethod(methodSymbol, classMemberSymbol, shouldOverride);
                    forwarded.Add(methodSymbol);
                }
            }

            void ForwardPropertiesInternal(IEnumerable<IPropertySymbol> propertySymbols, bool includeSetter = false)
            {
                foreach (var propertySymbol in propertySymbols)
                {
                    var mightIgnoreMember = classSubMembers.FirstOrDefault(symbol => symbol.Name == propertySymbol.Name);
                    var shouldOverride = mightIgnoreMember is not null;

                    if (shouldOverride && classMemberSymbol.IsStatic)
                    {
                        shouldOverride = false;
                    }

                    if (shouldOverride && mightIgnoreMember is not IPropertySymbol)
                    {
                        builder.Add($"// cannot override because base.{mightIgnoreMember!.Name} is not a property");
                        shouldOverride = false;
                    }

                    if (shouldOverride && !mightIgnoreMember!.IsVirtual)
                    {
                        builder.Add($"// cannot override because base.{mightIgnoreMember.Name} is not virtual");
                        shouldOverride = false;
                    }

                    builder.ForwardProperty(propertySymbol, classMemberSymbol, includeSetter, shouldOverride);
                }
            }
        }
        builder.Close();

        output.AddSource($"{builder.Name}.g.cs", builder.ToString());

        static bool CanSymbolBeForwarded(ISymbol symbol)
        {
            if (symbol.IsStatic) return false;
            if (symbol.DeclaredAccessibility is not Accessibility.Public) return false;
            if(symbol is IMethodSymbol method)
            {
                if (method.MethodKind is not MethodKind.Ordinary) return false;
            }

            return true;
        }
    }
}
