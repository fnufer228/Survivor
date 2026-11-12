using UnityEngine;
using UnityEngine.Serialization;

public class InventoryZone : MonoBehaviour
{
    [FormerlySerializedAs("itemData")] [SerializeField] private ResourceType resourceType;
    [SerializeField] private GameObject[] itemVisuals;

    public void Refresh()
    {
        if (resourceType == null || InventoryManager.Instance == null)
            return;

        int amount = InventoryManager.Instance.GetItemAmount(resourceType);

        for (int i = 0; i < itemVisuals.Length; i++)
        {
            if (itemVisuals[i] != null)
                itemVisuals[i].SetActive(i < amount);
        }
    }
}