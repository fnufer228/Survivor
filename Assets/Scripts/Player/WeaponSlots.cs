using System;
using UnityEngine;

/// <summary>
/// Слоти рук. Слот 0 — просто руки без предмета, слоти 1..N — руки з предметом.
/// Клавіші 1..9 вмикають слот; повторне натискання на активний слот повертає слот 0.
/// Висить на WeaponCamera: у чужих гравців вона вимкнена, тож перемикає лише локальний гравець.
/// </summary>
public class WeaponSlots : MonoBehaviour
{
    [Tooltip("Слот 0 — просто руки")]
    [SerializeField] private GameObject emptyHands;

    [Tooltip("Скільки слотів з предметами (клавіші 1..9)")]
    [SerializeField, Range(1, 9)] private int slotCount = 2;

    [Tooltip("Руки з предметом для кожного слота: елемент 0 — клавіша 1, елемент 1 — клавіша 2 ...")]
    [SerializeField] private GameObject[] slots = new GameObject[2];

    public int CurrentSlot { get; private set; }

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
        for (int i = 1; i <= slotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                PressSlot(i);
                break;
            }
        }
    }

    /// <summary>Як натискання клавіші слота: той самий слот ще раз — повернення до порожніх рук.</summary>
    public void PressSlot(int slot)
    {
        Select(slot == CurrentSlot ? 0 : slot);
    }

    public void Select(int slot)
    {
        slot = Mathf.Clamp(slot, 0, slotCount);
        CurrentSlot = slot;

        GameObject shown = slot > 0 && slot <= slots.Length ? slots[slot - 1] : null;
        if (shown == null) shown = emptyHands; // порожній слот — просто руки

        if (emptyHands != null) emptyHands.SetActive(shown == emptyHands);
        foreach (GameObject hands in slots)
            if (hands != null && hands != emptyHands)
                hands.SetActive(hands == shown);

        SlotChanged?.Invoke(slot);
    }
}
