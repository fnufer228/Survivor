using System;
using UnityEngine;

public class WeaponSlots : MonoBehaviour
{
    [Tooltip("Слот 0 — просто руки")]
    [SerializeField] private GameObject emptyHands;

    [Tooltip("Скільки слотів з предметами (клавіші 1..9)")]
    [SerializeField, Range(1, 9)] private int slotCount = 2;

    [Tooltip("Руки з предметом для кожного слота: елемент 0 — клавіша 1, елемент 1 — клавіша 2 ...")]
    [SerializeField] private GameObject[] slots = new GameObject[2];

    public int CurrentSlot { get; private set; }

    public bool InputEnabled { get; set; } = true;

    public event Action<int> SlotChanged;

    private void OnValidate()
    {
        if (slots == null) slots = new GameObject[slotCount];
        if (slots.Length != slotCount) Array.Resize(ref slots, slotCount);
    }

    private void Awake()
    {
        Select(0);
    }

    private void Update()
    {
        if (!InputEnabled) return;

        for (int i = 1; i <= slotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                PressSlot(i);
                break;
            }
        }
    }

    public void PressSlot(int slot)
    {
        Select(slot == CurrentSlot ? 0 : slot);
    }

    public void Select(int slot)
    {
        slot = Mathf.Clamp(slot, 0, slotCount);
        CurrentSlot = slot;

        GameObject shown = slot > 0 && slot <= slots.Length ? slots[slot - 1] : null;

        if (emptyHands != null) emptyHands.SetActive(true);
        foreach (GameObject item in slots)
            if (item != null && item != emptyHands)
                item.SetActive(item == shown);

        HandPose pose = shown != null ? shown.GetComponent<HandPose>() : null;
        if (pose == null && emptyHands != null) pose = emptyHands.GetComponent<HandPose>();
        if (pose != null) pose.Apply();

        SlotChanged?.Invoke(slot);
    }
}
