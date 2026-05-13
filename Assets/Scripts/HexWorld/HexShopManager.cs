using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HexShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] ToolPriceConfig priceConfig;
    [SerializeField] HexToolManager  toolManager;

    [Header("Shop Panel")]
    [SerializeField] GameObject shopPanel;
    [SerializeField] Button     shopOpenButton;
    [SerializeField] Button     shopCloseButton;

    [Header("Gold UI")]
    [SerializeField] TMP_Text goldText;

    [Header("Buy Buttons")]
    [SerializeField] Button    buyAxeButton;
    [SerializeField] Button    buyPickaxeButton;
    [SerializeField] Button    buyIronShearsButton;
    [SerializeField] Button    buyBombButton;

    [Header("Price Labels")]
    [SerializeField] TMP_Text axePriceText;
    [SerializeField] TMP_Text pickaxePriceText;
    [SerializeField] TMP_Text ironShearsPriceText;
    [SerializeField] TMP_Text bombPriceText;

    [Header("Discount Badges  (optional)")]
    [SerializeField] TMP_Text axeDiscountText;
    [SerializeField] TMP_Text pickaxeDiscountText;
    [SerializeField] TMP_Text ironShearsDiscountText;
    [SerializeField] TMP_Text bombDiscountText;

    private int _gold;

    // Effective prices after discount
    private int _axePrice;
    private int _pickaxePrice;
    private int _ironShearsPrice;
    private int _bombPrice;

    public int Gold => _gold;

    private void Start()
    {
        shopOpenButton?.onClick.AddListener(OpenShop);
        shopCloseButton?.onClick.AddListener(CloseShop);

        buyAxeButton?.onClick.AddListener(()        => BuyTool(ToolType.Axe));
        buyPickaxeButton?.onClick.AddListener(()    => BuyTool(ToolType.Pickaxe));
        buyIronShearsButton?.onClick.AddListener(() => BuyTool(ToolType.IronShears));
        buyBombButton?.onClick.AddListener(()       => BuyTool(ToolType.Bomb));

        shopPanel?.SetActive(false);
    }

    public void LoadLevel(HexLevelData data)
    {
        _gold = data.startingGold;

        _axePrice        = ApplyDiscount(priceConfig.axePrice,        data.axeDiscount);
        _pickaxePrice    = ApplyDiscount(priceConfig.pickaxePrice,     data.pickaxeDiscount);
        _ironShearsPrice = ApplyDiscount(priceConfig.ironShearsPrice,  data.ironShearsDiscount);
        _bombPrice       = ApplyDiscount(priceConfig.bombPrice,        data.bombDiscount);

        SetPriceLabel(axePriceText,        _axePrice,        priceConfig.axePrice,        axeDiscountText,        data.axeDiscount);
        SetPriceLabel(pickaxePriceText,    _pickaxePrice,    priceConfig.pickaxePrice,    pickaxeDiscountText,    data.pickaxeDiscount);
        SetPriceLabel(ironShearsPriceText, _ironShearsPrice, priceConfig.ironShearsPrice, ironShearsDiscountText, data.ironShearsDiscount);
        SetPriceLabel(bombPriceText,       _bombPrice,       priceConfig.bombPrice,       bombDiscountText,       data.bombDiscount);

        RefreshGoldUI();
        RefreshBuyButtons();
        shopPanel?.SetActive(false);
    }

    private void BuyTool(ToolType tool)
    {
        int price = PriceOf(tool);
        if (_gold < price) return;

        _gold -= price;
        toolManager.AddTool(tool);
        RefreshGoldUI();
        RefreshBuyButtons();
    }

    public void OpenShop()
    {
        RefreshBuyButtons();
        shopPanel?.SetActive(true);
    }

    public void CloseShop() => shopPanel?.SetActive(false);

    // ── Helpers ──

    private static int ApplyDiscount(int basePrice, int discountPct) =>
        discountPct <= 0 ? basePrice : Mathf.RoundToInt(basePrice * (1f - discountPct / 100f));

    private int PriceOf(ToolType tool) => tool switch
    {
        ToolType.Axe        => _axePrice,
        ToolType.Pickaxe    => _pickaxePrice,
        ToolType.IronShears => _ironShearsPrice,
        ToolType.Bomb       => _bombPrice,
        _                   => int.MaxValue
    };

    private void RefreshGoldUI()
    {
        if (goldText) goldText.text = $"{_gold}g";
    }

    private void RefreshBuyButtons()
    {
        SetBuyInteractable(buyAxeButton,        _gold >= _axePrice);
        SetBuyInteractable(buyPickaxeButton,    _gold >= _pickaxePrice);
        SetBuyInteractable(buyIronShearsButton, _gold >= _ironShearsPrice);
        SetBuyInteractable(buyBombButton,       _gold >= _bombPrice);
    }

    private static void SetBuyInteractable(Button btn, bool on)
    {
        if (btn) btn.interactable = on;
    }

    private static void SetPriceLabel(TMP_Text priceLabel, int effectivePrice, int basePrice,
        TMP_Text discountLabel, int discountPct)
    {
        if (priceLabel) priceLabel.text = $"{effectivePrice}g";

        if (discountLabel)
        {
            bool hasDiscount = discountPct > 0 && effectivePrice < basePrice;
            discountLabel.gameObject.SetActive(hasDiscount);
            if (hasDiscount) discountLabel.text = $"%{discountPct} İNDİRİM";
        }
    }
}
