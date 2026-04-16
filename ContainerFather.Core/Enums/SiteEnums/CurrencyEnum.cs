using System.ComponentModel;

namespace ContainerFather.Core.Enums.SiteEnums;

public enum CurrencyEnum
{
    [Description("RUB")]
    Ruble = 7,
    [Description("USD")]
    Dollar = 8,
    [Description("EU")]
    Euro = 9,
    [Description("KZT")]
    Tenge = 10,
    [Description("UAH")]
    Grivna = 11,
    [Description("BYN")]
    BRuble = 12,
}