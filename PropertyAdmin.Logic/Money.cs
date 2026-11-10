namespace PropertyAdmin.Logic;

public static class Money
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal Vat(decimal amountExclVat, decimal percentage) => Round(amountExclVat * percentage / 100m);
}
