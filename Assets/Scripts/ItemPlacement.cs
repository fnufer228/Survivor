
using System.Collections.Generic;
using UnityEngine;

public class ItemPlacement : MonoBehaviour
{
    [SerializeField] private Transform centerPoint;
    [SerializeField] private Camera inventoryCamera;
    [SerializeField] private float maxHorizontalOffset;
    [SerializeField] private float maxVerticalOffset;

    private readonly List<ResourceType> placedItems = new List<ResourceType>();
    private readonly List<GameObject> placedObjects = new List<GameObject>();

    public IReadOnlyList<ResourceType> PlacedItems => placedItems;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (inventoryCamera == null || InventoryManager.Instance == null)
            return;

        Ray ray = inventoryCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        GameObject clickedObject = hit.collider.gameObject;

        int placedIndex = placedObjects.FindIndex(obj =>
            obj != null &&
            (clickedObject == obj ||
             hit.collider.transform.IsChildOf(obj.transform)));

        if (placedIndex >= 0)
        {
            InventoryManager.Instance.AddItem(placedItems[placedIndex]);

            if (placedObjects[placedIndex] != null)
                Destroy(placedObjects[placedIndex]);

            placedItems.RemoveAt(placedIndex);
            placedObjects.RemoveAt(placedIndex);

            return;
        }

        ResourceNode resourceNode = hit.collider.GetComponentInParent<ResourceNode>();

        if (resourceNode == null)
            return;

        ResourceType resourceType = resourceNode.GetItemData();

        if (resourceType == null || resourceType.prefab == null)
            return;

        if (!InventoryManager.Instance.RemoveItem(resourceType))
            return;

        GameObject item = Instantiate(resourceType.prefab);

        if (!PlaceItem(item))
        {
            Destroy(item);
            InventoryManager.Instance.AddItem(resourceType);
            return;
        }

        placedItems.Add(resourceType);
        placedObjects.Add(item);
        
        if (CraftingManager.Instance != null)
            CraftingManager.Instance.CheckCraft();
    }

    private bool PlaceItem(GameObject item)
    {
        if (item == null || centerPoint == null)
            return false;

        item.transform.SetParent(centerPoint, false);

        item.transform.localPosition = new Vector3(
            Random.Range(-maxHorizontalOffset, maxHorizontalOffset),
            0.1f,
            Random.Range(-maxVerticalOffset, maxVerticalOffset)
        );

        item.transform.localRotation = Quaternion.identity;

        return true;
    }

    public void ConsumePlacedItems()
    {
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        placedItems.Clear();
        placedObjects.Clear();
    }

    public void ReturnItems()
    {
        if (InventoryManager.Instance == null)
            return;

        for (int i = 0; i < placedItems.Count; i++)
        {
            InventoryManager.Instance.AddItem(placedItems[i]);

            if (placedObjects[i] != null)
                Destroy(placedObjects[i]);
        }

        placedItems.Clear();
        placedObjects.Clear();
    }
}
