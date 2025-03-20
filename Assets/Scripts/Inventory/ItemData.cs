using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public int itemID;
    public float weight;
    public ItemType type;
}

public enum ItemType { Weapon, Food, Tool }
