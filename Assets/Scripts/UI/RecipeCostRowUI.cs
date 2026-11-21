using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeCostRowUI : MonoBehaviour
{
    [Tooltip("Resource icon. If empty, the Image on this object is used")]
    [SerializeField] Image icon;

    [Tooltip("Amount text. If empty, the first text among the children is used")]
    [SerializeField] TMP_Text amountText;
    [SerializeField] Text amountLegacyText;

    void Resolve()
    {
        if (icon == null) icon = GetComponent<Image>();

        if (amountText == null && amountLegacyText == null)
        {
            amountText = GetComponentInChildren<TMP_Text>(true);
            if (amountText == null) amountLegacyText = GetComponentInChildren<Text>(true);
        }
    }

    public void Set(RecipeCost cost)
    {
        Resolve();

        if (icon != null)
        {
            icon.sprite = cost.resource != null ? cost.resource.icon : null;
            icon.enabled = icon.sprite != null;
        }

        string amount = cost.amount.ToString();
        if (amountText != null) amountText.text = amount;
        if (amountLegacyText != null) amountLegacyText.text = amount;
    }
}
