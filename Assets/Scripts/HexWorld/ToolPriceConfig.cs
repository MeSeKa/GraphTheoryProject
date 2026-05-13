using UnityEngine;

[CreateAssetMenu(fileName = "ToolPriceConfig", menuName = "HexWorld/Tool Price Config")]
public class ToolPriceConfig : ScriptableObject
{
    public int axePrice        = 80;
    public int pickaxePrice    = 120;
    public int ironShearsPrice = 200;
    public int bombPrice       = 350;
}
