using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Barion.SourceGeneration; 

public static class SymbolExtensions {
    public static string GetKindString(this ITypeSymbol typeSymbol) {
        if(typeSymbol.TypeKind == TypeKind.Struct) {
            return "struct";
        } else if(typeSymbol.IsRecord) {
            return "record";
        } else {
            return "class";
        }
    }

    public static void AppendReturnString(this IMethodSymbol methodSymbol, StringBuilder stringBuilder) {
        if(methodSymbol.ReturnsVoid) {
            stringBuilder.Append("void");
            return;
        }

        methodSymbol.ReturnType.AppendCodeString(stringBuilder);
    }

    public static string GetReturnString(this IMethodSymbol methodSymbol) {
        if(methodSymbol.ReturnsVoid) {
            return "void";
        }

        return methodSymbol.ReturnType.GetCodeString();
    }
    

    public static string GetCodeString(this ITypeSymbol typeSymbol) {
        var sb = new StringBuilder();
        typeSymbol.AppendCodeString(sb);
        return sb.ToString();
    }

    public static void AppendCodeString(this ITypeSymbol typeSymbol, StringBuilder stringBuilder) {
        if(typeSymbol is IArrayTypeSymbol arrayTypeSymbol) {
            InternalAppendToStringBuilder(arrayTypeSymbol.ElementType);
            stringBuilder.Append("[]");
        } else {
            InternalAppendToStringBuilder(typeSymbol);
        }

        void InternalAppendToStringBuilder(ITypeSymbol typeSymbol) {
            stringBuilder.Append(typeSymbol.ToDisplayString());
        }
    }

    public static string ToKeyword(this Accessibility accessibility) {
        if(accessibility is Accessibility.Public or Accessibility.Private or Accessibility.Internal) {
            return accessibility.ToString().ToLower();
        }

        return "protected"; //is this ok?
    }

    public static bool HasAttribute(this ISymbol symbol, AttributeData data) {
        return symbol.GetAttributes().Contains(data);
    }
    
    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attributeSymbol) {
        return symbol.GetAttributes().Any(att => att.AttributeClass!.Equals(attributeSymbol, SymbolEqualityComparer.Default));
    }
    
    public static bool HasAttribute(this ISymbol symbol, string name) {
        return symbol.GetAttributes().Any(att => att.AttributeClass!.Name == name);
    }

    public static AttributeData? GetAttribute(this ISymbol symbol, string name) {
        return symbol.GetAttributes().FirstOrDefault(att => att.AttributeClass!.Name == name);
    }

    public static IEnumerable<ISymbol> GetMembers(this INamespaceOrTypeSymbol symbol, params SymbolKind[] kinds) {
        foreach(var member in symbol.GetMembers()) {
            if(kinds.Contains(member.Kind)) {
                yield return member;
            }
        }
    }
    
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol, params SymbolKind[] kinds) {
        foreach(var member in symbol.GetMembers()) {
            if(kinds.Contains(member.Kind)) {
                yield return member;
            }
        }

        if(symbol.BaseType is null) yield break;

        foreach(var baseMember in symbol.BaseType.GetAllMembers(kinds)) {
            yield return baseMember;
        }
    }
    
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol) {
        foreach(var member in symbol.GetMembers()) {
            yield return member;
        }

        if(symbol.BaseType is null) yield break;

        foreach(var baseMember in symbol.BaseType.GetAllMembers()) {
            yield return baseMember;
        }
    }

    public static bool MatchParameters(this IMethodSymbol mainSymbol, IMethodSymbol compareTo) {
        if(mainSymbol.Equals(compareTo, SymbolEqualityComparer.Default)) return true;
        if(mainSymbol.Parameters.Equals(compareTo.Parameters)) return true;
        return true;
    }
}