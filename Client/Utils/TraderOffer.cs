using System;
using EFT.InventoryLogic;

namespace MiyakoCarryService.Client.Utils;

internal sealed class TraderOffer
{
    public string Name { get; private set; }
    public long Price;
    public string CurrencySignal { get; } = "₽";
    public ECurrencyType CurrencyType;

    public TraderOffer(long price, int totalSlot, ECurrencyType currencyType, string name = "Miyako")
    {
        Name = name;
        CurrencyType = currencyType;
        Price = ExchangePrice(currencyType, price) / totalSlot;
    }

    public long ExchangePrice(ECurrencyType currencyType, long price)
    {
        return currencyType switch
        {
            ECurrencyType.USD => Math.Abs(price * 151),
            ECurrencyType.EUR => Math.Abs(price * 166),
            ECurrencyType.GP => Math.Abs(price * 50000),
            _ => Math.Abs(price)
        };
    }

    public TraderOffer()
    {
        Name = "";
        Price = 0;
        CurrencyType = ECurrencyType.RUB;
        CurrencySignal = "₽";
    }
}