using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [SerializeField] private List<ResourceType> items = new List<ResourceType>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public ResourceType GetItem(string Name)
    {
        foreach (ResourceType item in items)
        {
            if (item.displayName == name)
                return item;
        }

        return null;
    }
}