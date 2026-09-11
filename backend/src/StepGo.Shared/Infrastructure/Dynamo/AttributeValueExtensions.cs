using Amazon.DynamoDBv2.Model;
using StepGo.Shared.Domain;

namespace StepGo.Shared.Infrastructure.Dynamo;

/// <summary>
/// Low-level AttributeValue mapping helpers. Repositories build/read plain
/// Dictionary&lt;string,AttributeValue&gt; items directly rather than going through DynamoDBv2's
/// dynamic Document model, which keeps every code path Native-AOT friendly (design.md decision 2).
/// </summary>
public static class AttributeValueExtensions
{
    public static AttributeValue S(string value) => new(value);
    public static AttributeValue N(long value) => new() { N = value.ToString() };
    public static AttributeValue Bool(bool value) => new() { BOOL = value };
    public static AttributeValue NullableS(string? value) => value is null ? new AttributeValue { NULL = true } : new AttributeValue(value);

    public static string GetS(this Dictionary<string, AttributeValue> item, string key) => item[key].S;
    public static string? GetNullableS(this Dictionary<string, AttributeValue> item, string key)
        => item.TryGetValue(key, out var v) && v.NULL != true ? v.S : null;

    public static long GetN(this Dictionary<string, AttributeValue> item, string key) => long.Parse(item[key].N);
    public static long? GetNullableN(this Dictionary<string, AttributeValue> item, string key)
        => item.TryGetValue(key, out var v) && v.NULL != true ? long.Parse(v.N) : null;

    public static bool GetBool(this Dictionary<string, AttributeValue> item, string key) => item[key].BOOL ?? false;

    public static Guid GetGuid(this Dictionary<string, AttributeValue> item, string key) => Guid.Parse(item[key].S);
    public static Guid? GetNullableGuid(this Dictionary<string, AttributeValue> item, string key)
        => item.TryGetValue(key, out var v) && v.NULL != true ? Guid.Parse(v.S) : null;

    public static DateTimeOffset GetDateTimeOffset(this Dictionary<string, AttributeValue> item, string key) => DateTimeOffset.Parse(item[key].S);
    public static DateTimeOffset? GetNullableDateTimeOffset(this Dictionary<string, AttributeValue> item, string key)
        => item.TryGetValue(key, out var v) && v.NULL != true ? DateTimeOffset.Parse(v.S) : null;

    public static Money GetMoney(this Dictionary<string, AttributeValue> item, string key) => Money.FromWholeDollars(item.GetN(key));
    public static Money? GetNullableMoney(this Dictionary<string, AttributeValue> item, string key)
    {
        var n = item.GetNullableN(key);
        return n is null ? null : Money.FromWholeDollars(n.Value);
    }

    public static TEnum GetEnum<TEnum>(this Dictionary<string, AttributeValue> item, string key) where TEnum : struct, Enum
        => Enum.Parse<TEnum>(item.GetS(key));
    public static TEnum? GetNullableEnum<TEnum>(this Dictionary<string, AttributeValue> item, string key) where TEnum : struct, Enum
    {
        var s = item.GetNullableS(key);
        return s is null ? null : Enum.Parse<TEnum>(s);
    }
}
