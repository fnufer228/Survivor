using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;

public class LocalBodyHider : MonoBehaviourPun
{
    [SerializeField] private Transform bodyRoot;

    private void Start()
    {
        if (!photonView.IsMine || bodyRoot == null) return;
        foreach (var r in bodyRoot.GetComponentsInChildren<Renderer>(true))
            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }
}
