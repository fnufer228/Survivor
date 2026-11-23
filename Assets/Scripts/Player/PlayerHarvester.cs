using Photon.Pun;
using UnityEngine;

public class PlayerHarvester : MonoBehaviourPun
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float pickupDistance = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;

    [Tooltip("Resource, PickUp and Blueprint layers")]
    [SerializeField] private LayerMask layer;

    private IInteractable _current;
    private Component _currentComponent;

    public string CurrentPrompt => _current != null ? _current.Prompt : string.Empty;

    private void Update()
    {
        if (!photonView.IsMine || playerCamera == null) return;

        if (RecipeBookUI.IsOpen)
        {
            SetCurrent(null);
            return;
        }

        SetCurrent(FindTarget());

        if (_current != null && Input.GetKeyDown(pickupKey))
            _current.Interact();
    }

    private IInteractable FindTarget()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupDistance, layer, QueryTriggerInteraction.Collide))
            return null;

        foreach (IInteractable interactable in hit.collider.GetComponentsInParent<IInteractable>())
        {
            if (interactable.CanInteract) return interactable;
        }

        return null;
    }

    private void SetCurrent(IInteractable target)
    {
        if (_currentComponent == null) _current = null;
        if (target == _current) return;

        if (_currentComponent != null) InteractOutline.Hide(_currentComponent);

        _current = target;
        _currentComponent = target as Component;

        if (_currentComponent != null && !(_current is BuildSite))
            InteractOutline.Show(_currentComponent);
    }

    private void OnDisable()
    {
        if (_currentComponent != null) InteractOutline.Hide(_currentComponent);
        _current = null;
        _currentComponent = null;
    }

    private void OnDrawGizmos()
    {
        if (playerCamera == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.forward * pickupDistance);
    }
}
