using System.Linq;
using Xunit;

namespace NForza.Wolverine.ValueTypes.Tests;

public class ValueTypeGeneratorTests
{
    [Fact]
    public void GuidValue_GeneratesRecordStruct()
    {
        var source = @"
using NForza.Wolverine.ValueTypes;

namespace TestApp;

[GuidValue]
public partial record struct CustomerId;
";
        var (diagnostics, generated) = GeneratorTestHelper.RunGenerator(source);

        var recordSource = generated.FirstOrDefault(s => s.Contains("public partial record struct CustomerId"));
        Assert.NotNull(recordSource);
        Assert.Contains("IGuidValueType", recordSource);
        Assert.Contains("IComparable<CustomerId>", recordSource);
        Assert.Contains("Guid.NewGuid()", recordSource);
        Assert.Contains("Guid.Empty", recordSource);
        Assert.Contains("implicit operator Guid", recordSource);
        Assert.Contains("explicit operator CustomerId", recordSource);
    }

    [Fact]
    public void GuidValue_GeneratesTryParse()
    {
        var source = @"
using NForza.Wolverine.ValueTypes;

namespace TestApp;

[GuidValue]
public partial record struct OrderId;
";
        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        var recordSource = generated.FirstOrDefault(s => s.Contains("public partial record struct OrderId"));
        Assert.NotNull(recordSource);
        Assert.Contains("static bool TryParse(string? s, out OrderId result)", recordSource);
        Assert.Contains("Guid.TryParse(s, out var guid)", recordSource);
    }

    [Fact]
    public void GuidValue_GeneratesJsonConverter()
    {
        var source = @"
using NForza.Wolverine.ValueTypes;

namespace TestApp;

[GuidValue]
public partial record struct CustomerId;
";
        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        var converterSource = generated.FirstOrDefault(s => s.Contains("class CustomerIdJsonConverter"));
        Assert.NotNull(converterSource);
        Assert.Contains("JsonConverter<CustomerId>", converterSource);
        Assert.Contains("Guid.TryParse", converterSource);
    }

    [Fact]
    public void StringValue_GeneratesRecordStruct()
    {
        var source = @"
using NForza.Wolverine.ValueTypes;

namespace TestApp;

[StringValue(1, 50)]
public partial record struct Name;
";
        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        var recordSource = generated.FirstOrDefault(s => s.Contains("public partial record struct Name(string Value)"));
        Assert.NotNull(recordSource);
        Assert.Contains("IStringValueType", recordSource);
        Assert.Contains("Value.Length >= 1", recordSource);
        Assert.Contains("Value.Length <= 50", recordSource);
        Assert.Contains("static bool TryParse", recordSource);
    }

    [Fact]
    public void StringValue_WithRegex_GeneratesValidation()
    {
        var source = @"
using NForza.Wolverine.ValueTypes;

namespace TestApp;

[StringValue(1, 50, ""^[A-Za-z ]*$"")]
public partial record struct PersonName;
";
        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        var recordSource = generated.FirstOrDefault(s => s.Contains("public partial record struct PersonName"));
        Assert.NotNull(recordSource);
        Assert.Contains("Regex.IsMatch", recordSource);
    }

    [Fact]
    public void IntValue_GeneratesRecordStruct()
    {
        var source = @"
using NForza.Wolverine.ValueTypes;

namespace TestApp;

[IntValue(0, 100)]
public partial record struct Amount;
";
        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        var recordSource = generated.FirstOrDefault(s => s.Contains("public partial record struct Amount(int Value)"));
        Assert.NotNull(recordSource);
        Assert.Contains("IIntValueType", recordSource);
        Assert.Contains("Value >= 0", recordSource);
        Assert.Contains("Value <= 100", recordSource);
        Assert.Contains("operator <(", recordSource);
        Assert.Contains("operator >(", recordSource);
        Assert.Contains("static bool TryParse", recordSource);
    }

    [Fact]
    public void DoubleValue_GeneratesRecordStruct()
    {
        var source = @"
using NForza.Wolverine.ValueTypes;

namespace TestApp;

[DoubleValue(0.0, 99.9)]
public partial record struct Price;
";
        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        var recordSource = generated.FirstOrDefault(s => s.Contains("public partial record struct Price(double Value)"));
        Assert.NotNull(recordSource);
        Assert.Contains("IDoubleValueType", recordSource);
        Assert.Contains("static bool TryParse", recordSource);
    }

    [Fact]
    public void MultipleValueTypes_GeneratesWolverineExtension()
    {
        var source = @"
using NForza.Wolverine.ValueTypes;

namespace TestApp;

[GuidValue]
public partial record struct CustomerId;

[StringValue(1, 100)]
public partial record struct CustomerName;
";
        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        var extensionSource = generated.FirstOrDefault(s => s.Contains("WolverineValueTypeExtension"));
        Assert.NotNull(extensionSource);
        Assert.Contains("IWolverineExtension", extensionSource);
        Assert.Contains("CustomerIdJsonConverter", extensionSource);
        Assert.Contains("CustomerNameJsonConverter", extensionSource);
    }

    [Fact]
    public void NoNamespace_GeneratesWithoutNamespace()
    {
        var source = @"
using NForza.Wolverine.ValueTypes;

[GuidValue]
public partial record struct GlobalId;
";
        var (_, generated) = GeneratorTestHelper.RunGenerator(source);

        var recordSource = generated.FirstOrDefault(s => s.Contains("public partial record struct GlobalId"));
        Assert.NotNull(recordSource);
        Assert.DoesNotContain("namespace", recordSource.Split(new[] { "public partial record" }, System.StringSplitOptions.None)[0].Substring(recordSource.IndexOf("using NForza") + 1));
    }
}
