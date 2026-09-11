namespace StepGo.Shared.Domain;

/// <summary>Whole-dollar TWD amount. Business rounds every fee line to the nearest dollar.</summary>
public readonly record struct Money
{
    public long Cents { get; }

    private Money(long cents) => Cents = cents;

    public static Money FromWholeDollars(long dollars) => new(dollars);

    public static Money Zero => new(0);

    /// <summary>Applies a rate (e.g. 0.0289 for 2.89%) and rounds to the nearest whole dollar, away from zero.</summary>
    public Money ApplyRate(decimal rate)
    {
        var result = Math.Round(Cents * rate, 0, MidpointRounding.AwayFromZero);
        return new Money((long)result);
    }

    public static Money operator +(Money a, Money b) => new(a.Cents + b.Cents);
    public static Money operator -(Money a, Money b) => new(a.Cents - b.Cents);
    public static bool operator >(Money a, Money b) => a.Cents > b.Cents;
    public static bool operator <(Money a, Money b) => a.Cents < b.Cents;
    public static bool operator >=(Money a, Money b) => a.Cents >= b.Cents;
    public static bool operator <=(Money a, Money b) => a.Cents <= b.Cents;

    public override string ToString() => $"NT${Cents:N0}";
}
