using ContainerFather.Core.Enums.SiteEnums;
using DocumentFormat.OpenXml.Presentation;

namespace ContainerFather.Bot.Helpers;

public static class DescriptionHelper
{
    public static string GenerateDescription(ConditionEnum condition, CurrencyEnum currency, PriceType priceType, decimal price, string city)
    {
        //Продаётся новый контейнер high cube 40 футов в Санкт-Петербурге. Цена 92 000 рублей без НДС.
        string conditionString = condition == ConditionEnum.New ? "новый" : "Б/У";
        string typeString = "";
        string priceString = price.ToString("0.00");
        string currencyString = currency switch
        {
            CurrencyEnum.Ruble  => "рублей",
            CurrencyEnum.Dollar => "долларов",
            CurrencyEnum.Euro   => "евро",
            CurrencyEnum.Tenge  => "тенге",
            CurrencyEnum.BRuble => "белорусских рублей",
            _ => throw new ArgumentOutOfRangeException(
                nameof(currency), 
                $"Неподдерживаемое значение валюты: {(int)currency}")
        };
        string priceTypeString = priceType == PriceType.WithoutTax ? "без НДС" : "с НДС";
        string result = $"Продается {conditionString} контейнер {typeString} в {city}. Цена {priceString} {currencyString} {priceTypeString}.";
        return result;
    }
}