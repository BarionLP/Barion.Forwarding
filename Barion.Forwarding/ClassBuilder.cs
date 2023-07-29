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

    public ClassBuilder Forward(ISymbol forward, ISymbol from) {
        //if(from is not IFieldSymbol or IPropertySymbol) throw new InvalidOperationException();
        //if(forward is not IFieldSymbol or IPropertySymbol or IMethodSymbol) throw new InvalidOperationException();
        if(forward is IMethodSymbol methodSymbol) ForwardMethod(methodSymbol, from);
        else if(forward is IPropertySymbol propertySymbol) ForwardProperty(propertySymbol, from);
        return this;
    }

    private ClassBuilder ForwardMethod(IMethodSymbol forward, ISymbol from) {
        var builder = new MethodBuilder(forward);

        foreach(var parameter in forward.Parameters) {
            builder.AddParameter(parameter);
        }
        
        foreach(var parameter in forward.TypeParameters) {
            builder.AddGenericParameter(parameter);
        }

        var result = forward.ReturnsVoid ? builder.AddCall(forward, from, forward.Parameters.Select(parameter => parameter.Name)).ToString() : builder.ReturnCall(forward, from, forward.Parameters.Select(parameter => parameter.Name));

        StringBuilder.AppendLine($"\t{result.Replace("\n", "\n\t")}");

        return this;
    }

    private ClassBuilder ForwardProperty(IPropertySymbol forward, ISymbol from) {
        if(forward.IsIndexer) {

        } else {
            StringBuilder.AppendLine($"\t{forward.DeclaredAccessibility.ToKeyword()} {forward.Type.GetCodeString()} {forward.Name} => {from.Name}.{forward.Name};");
        }
        return this;
    }

    public void Close() {
        StringBuilder.Append('}');
    }
    public override string ToString() => StringBuilder.ToString();
    public override int GetHashCode() => StringBuilder.GetHashCode();
}