using UnityEngine;

[RequireComponent(typeof(Item))]
public class DragAndDrop : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private Rigidbody rb;
    private bool isSnapped = false;
    private InventorySystem inventorySystem;
    private Item item;

    private void Awake()
    {
        item = GetComponent<Item>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();

        inventorySystem = InventorySystem.Instance;
    }

    private void OnMouseDown()
    {
        if (!isSnapped)
        {
            isDragging = true;
            rb.isKinematic = true;
            offset = transform.position - GetMouseWorldPosition();
        }
    }

    private void OnMouseDrag()
    {
        if (isDragging)
        {
            transform.position = GetMouseWorldPosition() + offset;
        }
    }

    private void OnMouseUp()
    {
        bool nearBackpack = IsNearBackpack(transform.position);

        if (isDragging && !nearBackpack && isSnapped)
        {
            inventorySystem.RemoveItem(item.itemData, gameObject);
            isSnapped = false;
        }
        if (nearBackpack)
        {
            inventorySystem.AddItem(item.itemData, gameObject);
            isSnapped = true;
        }
        if (!isSnapped)
        {
            isDragging = false;
            rb.isKinematic = false;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }

    private bool IsNearBackpack(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.2f);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Backpack"))
            {
                return true;
            }
        }
        return false;
    }
}
