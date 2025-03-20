using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;

[RequireComponent(typeof(InventoryServer))]
public class InventorySystem : MonoBehaviour
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();
    [SerializeField] private CanvasGroup inventoryCanvasGroup;
    [SerializeField] private TextMeshProUGUI inventoryText;
    [SerializeField] private UnityEvent<string> onItemAdded;
    [SerializeField] private UnityEvent<string> onItemRemoved;
    private InventoryServer server;

    [System.Serializable]
    public class SnapPointGroup
    {
        public ItemType itemType;
        public Transform[] snapPoints;
    }

    [SerializeField] private List<SnapPointGroup> snapPointGroups;
    private HashSet<Transform> occupiedSnapPoints = new HashSet<Transform>();
    private Dictionary<ItemType, Queue<Transform>> snapPoints = new Dictionary<ItemType, Queue<Transform>>();
    private Dictionary<GameObject, Transform> itemSnapPoints = new Dictionary<GameObject, Transform>();

    public static InventorySystem Instance { get; private set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetInventoryUIVisible(false);
        server = GetComponent<InventoryServer>();
        InitializeSnapPoints();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && IsMouseOverBackpack())
        {
            SetInventoryUIVisible(true);
            UpdateInventoryUI();
        }
        if (Input.GetMouseButtonUp(0))
        {
            SetInventoryUIVisible(false);
        }
    }

    private void SetInventoryUIVisible(bool visible)
    {
        inventoryCanvasGroup.alpha = visible ? 1 : 0;
        inventoryCanvasGroup.blocksRaycasts = visible;
        inventoryCanvasGroup.interactable = visible;
    }

    private bool IsMouseOverBackpack()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider.CompareTag("Backpack");
        }
        return false;
    }

    private void InitializeSnapPoints()
    {
        foreach (var group in snapPointGroups)
        {
            Queue<Transform> pointsQueue = new Queue<Transform>(group.snapPoints);
            snapPoints[group.itemType] = pointsQueue;
        }
    }

    private Transform GetSnapPoint(ItemType type)
    {
        if (snapPoints.ContainsKey(type) && snapPoints[type].Count == 0)
        {
            Debug.LogWarning($"Очередь snapPoints[{type}] пуста! Перезагружаем точки...");
            ReloadSnapPoints(type);
        }

        if (!snapPoints.ContainsKey(type) || snapPoints[type].Count == 0)
        {
            Debug.LogWarning($"Нет доступных точек для {type}");
            return null;
        }

        foreach (Transform snapPoint in snapPoints[type])
        {
            if (!occupiedSnapPoints.Contains(snapPoint))
            {
                occupiedSnapPoints.Add(snapPoint);
                return snapPoint;
            }
        }

        Debug.LogWarning($"Все точки для {type} заняты!");
        return null;
    }


    private void ReloadSnapPoints(ItemType type)
    {
        foreach (var group in snapPointGroups)
        {
            if (group.itemType == type)
            {
                snapPoints[type] = new Queue<Transform>(group.snapPoints);
                return;
            }
        }
        Debug.LogError($"Не удалось перезагрузить точки снаппинга для {type}");
    }

    public void SnapToTarget(Transform snapPoint, GameObject itemObject, Rigidbody rb)
    {
        if (snapPoint == null)
        {
            Debug.LogError($"Ошибка: SnapToTarget получил null в качестве точки для {itemObject.name}!");
            return;
        }

        rb.isKinematic = true; // Отключаем физику, чтобы предмет двигался плавно
        StartCoroutine(SmoothSnap(itemObject, snapPoint));
    }

    private IEnumerator SmoothSnap(GameObject itemObject, Transform snapPoint)
    {
        float duration = 0.5f;
        float elapsedTime = 0f;
        Vector3 startPosition = itemObject.transform.position;
        Quaternion startRotation = itemObject.transform.rotation;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = t * t * (3f - 2f * t);

            itemObject.transform.position = Vector3.Lerp(startPosition, snapPoint.position, t);
            itemObject.transform.rotation = Quaternion.Slerp(startRotation, snapPoint.rotation, t);

            yield return null;
        }

        itemObject.transform.position = snapPoint.position;
        itemObject.transform.rotation = snapPoint.rotation;
        itemObject.transform.SetParent(snapPoint);
    }



    public void AddItem(ItemData item, GameObject itemObject)
    {
        if (items.Exists(i => i.itemID == item.itemID))
        {
            Debug.LogWarning($"Предмет с ID {item.itemID} уже есть в инвентаре.");
            SnapToTarget(itemSnapPoints[itemObject], itemObject, itemObject.GetComponent<Rigidbody>());
            return;
        }

        items.Add(item);
        Transform snapPoint = GetSnapPoint(item.type);

        if (snapPoint != null)
        {
            itemSnapPoints[itemObject] = snapPoint;
            SnapToTarget(snapPoint, itemObject, itemObject.GetComponent<Rigidbody>());
        }
        else
        {
            Debug.LogWarning($"Не удалось найти точку снаппинга для {item.itemName}!");
        }
        StartCoroutine(server.SendItemEvent(item.itemID, "added"));
        onItemAdded.Invoke(item.itemName);
        UpdateInventoryUI();
    }

    public void RemoveItem(ItemData item, GameObject itemObject)
    {
        items.Remove(item);
        itemObject.transform.SetParent(null);
        itemObject.GetComponent<Rigidbody>().isKinematic = false;

        if (snapPoints.ContainsKey(item.type))
        {
            if (itemSnapPoints.ContainsKey(itemObject))
            {
                Transform originalSnapPoint = itemSnapPoints[itemObject];
                itemSnapPoints.Remove(itemObject);
                occupiedSnapPoints.Remove(originalSnapPoint);
                snapPoints[item.type].Enqueue(originalSnapPoint);
            }
            else
            {
                Debug.LogWarning($"Попытка удалить предмет {item.itemName}, но его точка снаппинга не найдена!");
            }
        }
        StartCoroutine(server.SendItemEvent(item.itemID, "removed"));
        onItemRemoved.Invoke(item.itemName);
        UpdateInventoryUI();
    }


    private void UpdateInventoryUI()
    {
        inventoryText.text = "Inventory:\n";
        foreach (var item in items)
        {
            inventoryText.text += $"{item.itemName} ({item.type})\n";
        }
    }
}
