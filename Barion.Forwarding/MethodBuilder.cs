using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Barion.SourceGeneration;

public sealed class MethodBuilder(string name, string returnType)
{
    private string Accessibility = "private";
    private readonly string ReturnType = returnType;
    private readonly string Name = name;
    private bool IsStatic = false;
    private bool IsVirtual = false;
    private bool IsOverride = false;
    private readonly List<string> Parameters = [];
    private readonly List<string> GenericParameters = [];
    private readonly StringBuilder MethodBodyBuilder = new();

    public MethodBuilder(IMethodSymbol methodSymbol)
        : this(methodSymbol.Name, methodSymbol.GetReturnString())
    {
        SetAccessibility(methodSymbol.DeclaredAccessibility);
        if (methodSymbol.IsStatic) SetStatic();
        //if(methodSymbol.IsVirtual) SetVirtual(); // not a good idea to set it automatically
    }

    public MethodBuilder SetStatic()
    {
        IsStatic = true;
        return this;
    }

    public MethodBuilder SetVirtual()
    {
        IsVirtual = true;
        return this;
    }

    public MethodBuilder SetOverride()
    {
        IsOverride = true;
        return this;
    }

    public MethodBuilder SetAccessibility(Accessibility accessibility)
    {
        Accessibility = accessibility.ToKeyword();
        return this;
    }

    public MethodBuilder AddParameter(string type, string name)
    {
        Parameters.Add($"{type} {name}");
        return this;
    }

    public MethodBuilder AddParameter(string type, string name, string defaultValue)
    {
        Parameters.Add($"{type} {name} = {defaultValue}");
        return this;
    }

    public MethodBuilder AddParameter(IParameterSymbol parameterSymbol)
    {
        if (parameterSymbol.HasExplicitDefaultValue)
        {
            return AddParameter(parameterSymbol.Type.GetCodeString(), parameterSymbol.Name, parameterSymbol.ExplicitDefaultValue!.ToString()); //might not work always
        }
        return AddParameter(parameterSymbol.Type.GetCodeString(), parameterSymbol.Name);
    }

    public MethodBuilder AddParameters(IEnumerable<IParameterSymbol> parameterSymbols)
    {
        foreach (var parameterSymbol in parameterSymbols)
        {
            AddParameter(parameterSymbol);
        }
        return this;
    }

    public MethodBuilder AddGenericParameter(ITypeSymbol parameterSymbol)
    {
        GenericParameters.Add(parameterSymbol.ToDisplayString());
        return this;
    }
    public MethodBuilder AddGenericParameters(IEnumerable<ITypeSymbol> parameterSymbols)
    {
        foreach (var parameterSymbol in parameterSymbols)
        {
            AddGenericParameter(parameterSymbol);
        }
        return this;
    }

    public MethodBuilder AddOperation(string statement)
    {
        MethodBodyBuilder.AppendLine($"\t{statement};");
        return this;
    }

    public MethodBuilder AddCall(IMethodSymbol methodSymbol, ISymbol from, IEnumerable<string> parameters)
    {
        return AddOperation(GetCallString(methodSymbol, from, parameters));
    }
    private static string GetCallString(IMethodSymbol methodSymbol, ISymbol from, IEnumerable<string> parameters)
    {
        return $"{from.Name}.{methodSymbol.Name}({string.Join(", ", parameters)})";
    }

    public string Return(string statement)
    {
        MethodBodyBuilder.Append("    ");
        MethodBodyBuilder.Append("return ");
        MethodBodyBuilder.Append(statement);
        MethodBodyBuilder.AppendLine(";");
        return ToString();
    }

    public string ReturnCall(IMethodSymbol methodSymbol, ISymbol from, IEnumerable<string> parameters)
    {
        return Return(GetCallString(methodSymbol, from, parameters));
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Accessibility);
        if (IsOverride)
        {
            builder.Append(" override");
        }
        else
        {
            if (IsStatic) builder.Append(" static");
            if (IsVirtual) builder.Append(" virtual");
        }
        builder.Append($" {ReturnType} {Name}");
        if (GenericParameters.Count > 0)
        {
            builder.Append($"<{string.Join(", ", GenericParameters)}>");
        }
        builder.Append('(');
        builder.Append(string.Join(", ", Parameters));
        builder.AppendLine(")\n{");
        builder.Append(MethodBodyBuilder.ToString());
        builder.Append("}");
        return builder.ToString();
    }
}
