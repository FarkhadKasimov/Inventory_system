using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemData itemData;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (itemData != null)
        {
            ApplyItemData();
        }
    }

    private void ApplyItemData()
    {
        gameObject.name = itemData.itemName;

        if (rb != null)
        {
            rb.mass = itemData.weight;
        }
    }
}