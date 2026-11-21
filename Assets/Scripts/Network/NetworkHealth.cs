using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class NetworkHealth : MonoBehaviourPun, IDamageable, IPunObservable
{
    [SerializeField] int maxHealth = 100;
    [SerializeField] float maxHitDistance = 6f;
    [Tooltip("Seconds before the object is destroyed after death")]
    [SerializeField] float destroyDelay = 0f;

    public int MaxHealth => maxHealth;
    public int Health { get; private set; }
    public bool IsDead => Health <= 0;

    public event System.Action<int> HealthChanged;
    public event System.Action Died;

    bool diedRaised;

    void Awake() => Health = maxHealth;

    public void TakeHit(int damage)
    {
        if (IsDead) return;

        Player target = photonView.Owner ?? PhotonNetwork.MasterClient;
        photonView.RPC(nameof(RPC_RequestDamage), target, damage);
    }

    [PunRPC]
    void RPC_RequestDamage(int damage, PhotonMessageInfo info)
    {
        if (!photonView.IsMine || IsDead) return;
        if (!NetworkPlayer.IsNear(info.Sender, transform.position, maxHitDistance)) return;

        SetHealth(Health - Mathf.Clamp(damage, 1, 1000));

        if (IsDead)
            photonView.RPC(nameof(RPC_Died), RpcTarget.All);
    }

    [PunRPC]
    void RPC_Died()
    {
        if (diedRaised) return;
        diedRaised = true;

        SetHealth(0);
        Died?.Invoke();

        if (photonView.IsMine)
            StartCoroutine(DestroyAfterDelay());
    }

    IEnumerator DestroyAfterDelay()
    {
        if (destroyDelay > 0f) yield return new WaitForSeconds(destroyDelay);
        PhotonNetwork.Destroy(gameObject);
    }

    void SetHealth(int value)
    {
        value = Mathf.Clamp(value, 0, maxHealth);
        if (value == Health) return;
        Health = value;
        HealthChanged?.Invoke(Health);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
            stream.SendNext(Health);
        else
            SetHealth((int)stream.ReceiveNext());
    }
}
