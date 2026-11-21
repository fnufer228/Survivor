using UnityEngine;

[CreateAssetMenu(menuName = "Game/Pickup Item")]
public class PickupItemType : ScriptableObject
{
    public string itemId = "stick";
    public string displayName = "Stick";

    [Tooltip("Prefab WITHOUT PhotonView, with a collider")]
    public GameObject prefab;

    [Min(1)] public int amount = 1;
}
