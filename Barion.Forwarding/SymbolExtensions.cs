using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ametrin.SourceGeneration; 

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

    public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol symbol) {
        foreach(var member in symbol.GetMembers()) {
            yield return member;
        }

        if(symbol.BaseType is null) yield break;

        foreach(var baseMember in symbol.BaseType.GetAllMembers()) {
            yield return baseMember;
        }
    }
}