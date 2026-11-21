using Photon.Pun;
using UnityEngine;

public class PlayerCombat : MonoBehaviourPun
{
    [SerializeField] Camera cam;
    [SerializeField] float reach = 3f;
    [SerializeField] int damage = 1;
    [SerializeField] float cooldown = 0.5f;
    [SerializeField] LayerMask hitMask;

    float nextAttack;

    void Update()
    {
        if (!photonView.IsMine || Time.time < nextAttack || !Input.GetMouseButtonDown(0)) return;
        nextAttack = Time.time + cooldown;

        Transform origin = cam.transform;
        if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, reach, hitMask, QueryTriggerInteraction.Ignore))
            return;

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        target?.TakeHit(damage);
    }
}
