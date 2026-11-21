using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkPlayer : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        info.Sender.TagObject = gameObject;
    }

    public static bool IsNear(Player player, Vector3 position, float maxDistance)
    {
        if (player != null && player.TagObject is GameObject go && go != null)
            return (go.transform.position - position).sqrMagnitude <= maxDistance * maxDistance;
        return true;
    }
}
