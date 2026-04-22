using ContainerFather.Core.Enums.SiteEnums;
using DocumentFormat.OpenXml.Presentation;

namespace ContainerFather.Bot.Helpers;

public static class DescriptionHelper
{
    public static string GenerateDescription(ConditionEnum condition, CurrencyEnum currency, PriceType priceType, decimal price, string city, CategoryEnum categoryId)
    {
        //Продаётся новый контейнер high cube 40 футов в Санкт-Петербурге. Цена 92 000 рублей без НДС.
        string conditionString = condition == ConditionEnum.New ? "новый" : "Б/У";
        string typeString = categoryId switch
        {
            CategoryEnum.МорскиеИЖДКонтейнеры => "морской контейнер",
            CategoryEnum.Контейнеры10Футов => "контейнер 10 футов",
            CategoryEnum.Контейнеры20Футов => "контейнер 20 футов",
            CategoryEnum.Контейнеры40Футов   => "контейнер 40 футов",
            CategoryEnum.Контейнеры45Футов  => "контейнер 45 футов",
            CategoryEnum.Контейнеры3Тонны => "контейнер 3 тонны",
            
            CategoryEnum.HighCubeКонтейнеры => "контейнер High cube",
            CategoryEnum.HighCubeКонтейнеры20Футов => "контейнер High cube 20 футов",
            CategoryEnum.HighCubeКонтейнеры40Футов => "контейнер High cube 40 футов",
            
            CategoryEnum.OpenTopКонтейнеры => "контейнер Open top",
            CategoryEnum.OpenTopКонтейнеры20Футов => "контейнер Open top 20 футов",
            CategoryEnum.OpenTopКонтейнеры40Футов => "контейнер Open top 40 футов",
            
            CategoryEnum.FlatRackКонтейнеры => "контейнер Flat rack",
            CategoryEnum.FlatRackКонтейнеры20Футов => "контейнер Flat rack 20 футов",
            CategoryEnum.FlatRackКонтейнеры40Футов => "контейнер Flat rack 40 футов",
            
            CategoryEnum.РефрижераторныеКонтейнеры => "рефрижераторный контейнер",
            CategoryEnum.Рефконтейнеры20Футов => "рефрижераторный контейнер 20 футов",
            CategoryEnum.Рефконтейнеры40Футов => "рефрижераторный контейнер 40 футов",
            CategoryEnum.Рефконтейнеры45Футов => "рефрижераторный контейнер 45 футов",

            CategoryEnum.ТанкКонтейнеры => "танк контейнер",
            CategoryEnum.ТанкКонтейнеры20Футов => "танк контейнер 20 футов",
            CategoryEnum.ТанкКонтейнеры40Футов => "танк контейнер 40 футов",

            _ => throw new ArgumentOutOfRangeException(
                nameof(currency), 
                $"Неподдерживаемое значение валюты: {(int)currency}")
        };;
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
        string result = $"Продается {conditionString} {typeString} в {city}. Цена {priceString} {currencyString} {priceTypeString}.";
        return result;
    }
}