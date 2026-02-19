using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using NForza.Wolverine.ValueTypes.Generators.CodeGeneration;
using NForza.Wolverine.ValueTypes.Generators.Roslyn;

namespace NForza.Wolverine.ValueTypes.Generators;

[Generator]
public class WolverineValueTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var guidValues = CreateProvider(context, Roslyn.SyntaxNodeExtensions.IsRecordWithGuidValueAttribute, ValueTypeKind.Guid);
        var stringValues = CreateProvider(context, Roslyn.SyntaxNodeExtensions.IsRecordWithStringValueAttribute, ValueTypeKind.String);
        var intValues = CreateProvider(context, Roslyn.SyntaxNodeExtensions.IsRecordWithIntValueAttribute, ValueTypeKind.Int);
        var doubleValues = CreateProvider(context, Roslyn.SyntaxNodeExtensions.IsRecordWithDoubleValueAttribute, ValueTypeKind.Double);

        var allValues = guidValues
            .Combine(stringValues)
            .Combine(intValues)
            .Combine(doubleValues)
            .Select((combined, _) =>
            {
                var (((guids, strings), ints), doubles) = combined;
                return guids.AddRange(strings).AddRange(ints).AddRange(doubles);
            });

        context.RegisterSourceOutput(allValues, GenerateAllSources);
    }

    private IncrementalValueProvider<ImmutableArray<ValueTypeInfo>> CreateProvider(
        IncrementalGeneratorInitializationContext context,
        Func<SyntaxNode, bool> predicate,
        ValueTypeKind kind)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => predicate(node),
                transform: (ctx, _) => ExtractValueTypeInfo(ctx, kind))
            .Where(x => x is not null)
            .Select((x, _) => x!)
            .Collect();
    }

    private ValueTypeInfo? ExtractValueTypeInfo(GeneratorSyntaxContext context, ValueTypeKind kind)
    {
        var symbol = context.GetRecordSymbolFromContext();
        if (symbol is null) return null;

        var info = new ValueTypeInfo
        {
            Name = symbol.Name,
            Namespace = symbol.ContainingNamespace.GetNameOrEmpty(),
            Kind = kind
        };

        switch (kind)
        {
            case ValueTypeKind.String:
                ExtractStringValidation(symbol, info);
                break;
            case ValueTypeKind.Int:
                ExtractIntValidation(symbol, info);
                break;
            case ValueTypeKind.Double:
                ExtractDoubleValidation(symbol, info);
                break;
        }

        return info;
    }

    private void ExtractStringValidation(INamedTypeSymbol symbol, ValueTypeInfo info)
    {
        var attribute = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "StringValueAttribute");
        if (attribute is null) return;

        var args = attribute.ConstructorArguments.Select(a => a.Value?.ToString()).ToList();
        if (args.Count > 0 && int.TryParse(args[0], out var min) && min >= 0) info.MinLength = min;
        if (args.Count > 1 && int.TryParse(args[1], out var max) && max >= 0) info.MaxLength = max;
        if (args.Count > 2) info.ValidationRegex = args[2];
    }

    private void ExtractIntValidation(INamedTypeSymbol symbol, ValueTypeInfo info)
    {
        var attribute = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "IntValueAttribute");
        if (attribute is null) return;

        var args = attribute.ConstructorArguments.Select(a => a.Value?.ToString()).ToList();
        if (args.Count > 0 && int.TryParse(args[0], out var min) && min != int.MinValue) info.IntMinimum = min;
        if (args.Count > 1 && int.TryParse(args[1], out var max) && max != int.MaxValue) info.IntMaximum = max;
    }

    private void ExtractDoubleValidation(INamedTypeSymbol symbol, ValueTypeInfo info)
    {
        var attribute = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "DoubleValueAttribute");
        if (attribute is null) return;

        var args = attribute.ConstructorArguments.Select(a => a.Value?.ToString()).ToList();
        if (args.Count > 0 && double.TryParse(args[0], out var min) && !double.IsNaN(min)) info.DoubleMinimum = min;
        if (args.Count > 1 && double.TryParse(args[1], out var max) && !double.IsNaN(max)) info.DoubleMaximum = max;
    }

    private void GenerateAllSources(SourceProductionContext spc, ImmutableArray<ValueTypeInfo> valueTypes)
    {
        foreach (var vt in valueTypes)
        {
            var recordSource = GenerateRecordStruct(vt);
            spc.AddSource($"{vt.Name}.g.cs", recordSource);

            var jsonConverterSource = GenerateJsonConverter(vt);
            spc.AddSource($"{vt.Name}JsonConverter.g.cs", jsonConverterSource);
        }

        if (valueTypes.Length > 0)
        {
            var extensionSource = WolverineExtensionTemplates.Generate(valueTypes);
            spc.AddSource("WolverineValueTypeExtension.g.cs", extensionSource);
        }
    }

    private string GenerateRecordStruct(ValueTypeInfo vt)
    {
        return vt.Kind switch
        {
            ValueTypeKind.Guid => GuidValueTemplates.GenerateRecordStruct(vt),
            ValueTypeKind.String => StringValueTemplates.GenerateRecordStruct(vt),
            ValueTypeKind.Int => IntValueTemplates.GenerateRecordStruct(vt),
            ValueTypeKind.Double => DoubleValueTemplates.GenerateRecordStruct(vt),
            _ => throw new InvalidOperationException($"Unknown value type kind: {vt.Kind}")
        };
    }

    private string GenerateJsonConverter(ValueTypeInfo vt)
    {
        return vt.Kind switch
        {
            ValueTypeKind.Guid => JsonConverterTemplates.GenerateGuid(vt),
            ValueTypeKind.String => JsonConverterTemplates.GenerateString(vt),
            ValueTypeKind.Int => JsonConverterTemplates.GenerateInt(vt),
            ValueTypeKind.Double => JsonConverterTemplates.GenerateDouble(vt),
            _ => throw new InvalidOperationException($"Unknown value type kind: {vt.Kind}")
        };
    }

}
