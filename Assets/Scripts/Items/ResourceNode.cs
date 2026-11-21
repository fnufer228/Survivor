using System.Collections;
using UnityEngine;

public class ResourceNode : MonoBehaviour, IDamageable, IInteractable
{
    public int Id { get; private set; }
    public ResourceType Type { get; private set; }
    public string Prompt => Type != null ? Type.displayName : string.Empty;
    public bool CanInteract => Type != null && Type.harvestMode == HarvestMode.Pickup;
 
    [SerializeField] private ResourceType resourceType;
    
    Vector3 baseScale;
    Quaternion baseRotation;
    
    public ResourceType GetItemData()
    {
        return resourceType;
    }

    void Start()
    {
        if (Type == null)
        {
            Type = resourceType;
        }
    }

    public void Init(int id, ResourceType type)
    {
        Id = id;
        Type = type;
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        if(resourceType == null)
            resourceType = type;
    }
 
    public void Hide()
    {
        StopAllCoroutines();
        transform.localScale = baseScale;
        transform.localRotation = baseRotation;
        gameObject.SetActive(false);
        Debug.Log(resourceType.displayName);
    }
 
    public void PlayHit()
    {
        if (gameObject.activeInHierarchy) StartCoroutine(Shake());
    }
 
    public void PlayRespawn()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        transform.localRotation = baseRotation;
        StartCoroutine(Grow());
    }
 
    IEnumerator Grow()
    {
        const float duration = 1.5f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            transform.localScale = baseScale * Mathf.SmoothStep(0f, 1f, t / duration);
            yield return null;
        }
        transform.localScale = baseScale;
    }
    IEnumerator Shake()
    {
        const float duration = 0.25f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float angle = Mathf.Sin(t * 60f) * 3f * (1f - t / duration);
            transform.localRotation = baseRotation * Quaternion.Euler(angle, 0, angle);
            yield return null;
        }
        transform.localRotation = baseRotation;
    }
    public void TakeHit(int damage)
    {
        if (Type == null || Type.harvestMode != HarvestMode.Hit) return;
        if (ResourceManager.Instance != null) ResourceManager.Instance.Hit(this, damage);
    }
 
    public void Interact()
    {
        if (!CanInteract) return;
        if (ResourceManager.Instance != null) ResourceManager.Instance.Pickup(this);
    }
}