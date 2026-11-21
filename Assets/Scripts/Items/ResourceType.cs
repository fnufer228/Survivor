using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum HarvestMode
{
        Hit,
        Pickup
}

[Serializable]
public class DropEntry
{
    public PickupItemType item;
    //public int min = 1;
    //public int max = 1;

}
[CreateAssetMenu(fileName = "NewResource", menuName = "Data")]
public class ResourceType : ScriptableObject
{
    public string displayName;
    public int maxStack = 1;
    public GameObject prefab;
    public float spawnWeight = 1;
    public int maxHealth = 1;
    public float respawnSeconds = 300;
    public HarvestMode harvestMode;
    public DropEntry drop;
    public Sprite icon;
}