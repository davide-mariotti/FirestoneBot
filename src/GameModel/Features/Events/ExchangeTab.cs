using System;
using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Events;

/// <summary>An event shop's Exchange tab. Every event shop has the same one; only the paths differ.</summary>
public sealed class ExchangeTab
{
    /// <summary>
    ///     What event currency is worth, best first: the F2P guide's "buy" list (Dragon Blood,
    ///     Meteorites), then Beer - on its "avoid" list, but better than letting the currency expire
    ///     with the event. A shop that doesn't sell a name just skips it. Buying stops at each item's
    ///     cap or when the currency runs out, so leftovers roll down to the next name.
    /// </summary>
    private static readonly string[] DefaultPriorityItems = { "Dragon blood", "Meteorite", "Beer" };

    // A ceiling only: the button goes unclickable at the item's cap.
    private const int MaxBuysPerItem = 100;

    private readonly string _logTag;
    private readonly string _quantityBtn;
    private readonly string _quantityTxt;
    private readonly string _itemsRoot;
    private readonly string _itemNameTxt;
    private readonly string _itemBuyBtn;
    private readonly string[] _priorityItems;

    public ExchangeTab(string logTag, string quantityBtn, string quantityTxt, string itemsRoot, string itemNameTxt,
        string itemBuyBtn, string[] priorityItems = null)
    {
        _priorityItems = priorityItems ?? DefaultPriorityItems;
        _logTag = logTag;
        _quantityBtn = quantityBtn;
        _quantityTxt = quantityTxt;
        _itemsRoot = itemsRoot;
        _itemNameTxt = itemNameTxt;
        _itemBuyBtn = itemBuyBtn;
    }

    /// <summary>
    ///     Selects x1, then buys PriorityItems in order. Not x10: currency trickles in, and a bulk buy
    ///     waits for 10 prices' worth - 15,000 pumpkins for Halloween's meteorites, 5 days of drops,
    ///     so up to that much would expire with the event.
    /// </summary>
    public IEnumerator BuyPriorityItems()
    {
        yield return QuantityToggle.CycleUntil(new GameButton(_quantityBtn), new GameText(_quantityTxt),
            QuantityToggle.IsOne);

        foreach (var item in _priorityItems)
            yield return BuyItem(item);
    }

    /// <summary>
    ///     Buys the first item whose name contains itemName (case-insensitive) until it's capped or
    ///     unaffordable. The buy button stays clickable when unaffordable, so the CurrencyMissing
    ///     popup is what actually stops the loop.
    /// </summary>
    private IEnumerator BuyItem(string itemName)
    {
        var items = new GameElement(_itemsRoot).GetChildren()
            .Select(item => (Item: item, Name: new GameText(_itemNameTxt, item).GetParsedText())).ToList();
        Logger.Debug($"[{_logTag}] BuyItem('{itemName}'): {items.Count} item(s) scanned: " +
                     string.Join(", ", items.Select(i => i.Name)));

        foreach (var (item, name) in items)
        {
            if (!name.Contains(itemName, StringComparison.OrdinalIgnoreCase)) continue;

            var buyBtn = new GameButton(_itemBuyBtn, item);
            var bought = 0;
            for (var i = 0; i < MaxBuysPerItem && buyBtn.IsClickable(); i++)
            {
                yield return buyBtn.Click();

                if (CurrencyMissingPopup.IsShowing)
                {
                    Logger.Debug($"[{_logTag}] BuyItem: currency ran out after {bought} purchase(s).");
                    yield return CurrencyMissingPopup.Close;
                    break;
                }

                bought++;
            }

            Logger.Debug($"[{_logTag}] BuyItem: matched '{name}', bought {bought}x.");
            yield break;
        }

        Logger.Debug($"[{_logTag}] BuyItem: no item matching '{itemName}' found.");
    }
}
