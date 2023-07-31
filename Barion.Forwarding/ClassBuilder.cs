using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Barion.SourceGeneration; 
public sealed class ClassBuilder {
    private readonly StringBuilder StringBuilder = new();
    public readonly string Name;

    public ClassBuilder(INamedTypeSymbol typeSymbol, INamespaceSymbol? namespaceSymbol = null) : this(typeSymbol.Name, typeSymbol.GetKindString(), true, namespaceSymbol?.ToDisplayString()) {}
    
    public ClassBuilder(string name, string kind, bool isPartial = false, string? @namespace = null){
        Name = name;
        StringBuilder.AppendLine("#nullable enable");

        if(!string.IsNullOrWhiteSpace(@namespace)) {
            StringBuilder.AppendLine($"namespace {@namespace};");
        }

        if(isPartial) StringBuilder.Append("partial ");
        StringBuilder.AppendLine($$"""{{kind}} {{name}}{""");
    }

    public ClassBuilder AddMethod(MethodBuilder builder) {
        StringBuilder.AppendLine(builder.ToString());
        return this;
    }

    public ClassBuilder ForwardMethod(IMethodSymbol forward, ISymbol from, bool shouldOverride = false) {
        var builder = new MethodBuilder(forward);

        if(from.IsStatic) builder.SetStatic();
        if(shouldOverride) builder.SetOverride();
        if(forward.Parameters.Length > 0) builder.AddParameters(forward.Parameters);
        if(forward.TypeParameters.Length > 0) builder.AddGenericParameters(forward.TypeParameters);

        var result = forward.ReturnsVoid ? builder.AddCall(forward, from, forward.Parameters.Select(parameter => parameter.Name)).ToString() : builder.ReturnCall(forward, from, forward.Parameters.Select(parameter => parameter.Name));

        return Add(result);
    }

    public ClassBuilder ForwardProperty(IPropertySymbol forward, ISymbol from, bool includeSetter = false, bool shouldOverride = false, bool isStatic = false) {        
        if(forward.IsIndexer) {
            return Add("//Indexers not yet supported");
        } 
        if(includeSetter && forward.SetMethod?.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal) {
            return Add($$"""
                        {{forward.DeclaredAccessibility.ToKeyword()}} {{(from.IsStatic ? "static " : string.Empty)}}{{(shouldOverride ? "override " : string.Empty)}}{{forward.Type.GetCodeString()}} {{forward.Name}} {
                            get => {{from.Name}}.{{forward.Name}};
                            set => {{from.Name}}.{{forward.Name}} = value;
                        }
                        """);
        }
            
        return Add($"{forward.DeclaredAccessibility.ToKeyword()} {(from.IsStatic ? "static " : string.Empty)}{(shouldOverride ? "override " : string.Empty)}{forward.Type.GetCodeString()} {forward.Name} => {from.Name}.{forward.Name};");
    }

    public ClassBuilder Add(string code) {
        StringBuilder.Append('\t');
        StringBuilder.AppendLine(code.Replace("\n", "\n\t"));
        return this;
    }

    public void Close() {
        StringBuilder.Append('}');
    }
    public override string ToString() => StringBuilder.ToString();
    public override int GetHashCode() => StringBuilder.GetHashCode();
}