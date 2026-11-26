using UnityEngine;

public class RandomVariant : MonoBehaviour
{
    [SerializeField] private GameObject[] variants;

    private void Start()
    {
        if (variants == null || variants.Length == 0) return;

        Vector3 p = transform.position;
        int hash = Mathf.RoundToInt(p.x * 73.1f) * 92821 ^ Mathf.RoundToInt(p.z * 51.7f) * 68917 ^ Mathf.RoundToInt(transform.eulerAngles.y * 13f);
        int pick = Mathf.Abs(hash) % variants.Length;

        for (int i = 0; i < variants.Length; i++)
            if (variants[i] != null) variants[i].SetActive(i == pick);
    }
}
