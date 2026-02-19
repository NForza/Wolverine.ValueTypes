using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NForza.Wolverine.ValueTypes.Generators.Roslyn;

internal static class SyntaxNodeExtensions
{
    public static bool IsRecordWithAttribute(this SyntaxNode syntaxNode, string attributeName)
    {
        return syntaxNode is RecordDeclarationSyntax recordDeclaration &&
               recordDeclaration.AttributeLists
                   .SelectMany(al => al.Attributes)
                   .Any(a => a.Name.ToString() == attributeName || a.Name.ToString() == attributeName + "Attribute");
    }

    public static bool IsRecordWithGuidValueAttribute(this SyntaxNode syntaxNode)
        => IsRecordWithAttribute(syntaxNode, "GuidValue");

    public static bool IsRecordWithIntValueAttribute(this SyntaxNode syntaxNode)
        => IsRecordWithAttribute(syntaxNode, "IntValue");

    public static bool IsRecordWithDoubleValueAttribute(this SyntaxNode syntaxNode)
        => IsRecordWithAttribute(syntaxNode, "DoubleValue");

    public static bool IsRecordWithStringValueAttribute(this SyntaxNode syntaxNode)
        => IsRecordWithAttribute(syntaxNode, "StringValue");
}
