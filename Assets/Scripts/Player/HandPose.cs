using System;
using UnityEngine;

public class HandPose : MonoBehaviour
{
    [Serializable]
    public struct Bone
    {
        public string path;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    [Tooltip("Hands object that holds the rig (Hands_Empty)")]
    [SerializeField] private Transform handsRoot;
    [Tooltip("Rig root under the hands object (Hands_Rig)")]
    [SerializeField] private Transform rigRoot;
    [SerializeField] private Bone[] bones = Array.Empty<Bone>();

    private Transform[] resolved;

    public void Setup(Transform hands, Transform rig, Bone[] pose)
    {
        handsRoot = hands;
        rigRoot = rig;
        bones = pose;
        resolved = null;
    }

    public void Apply()
    {
        if (handsRoot == null || bones == null) return;
        Resolve();

        for (int i = 0; i < bones.Length; i++)
        {
            Transform t = resolved[i];
            if (t == null) continue;
            t.localPosition = bones[i].position;
            t.localRotation = bones[i].rotation;
            t.localScale = bones[i].scale;
        }
    }

    [ContextMenu("Save current pose")]
    private void SaveCurrentPose()
    {
        if (handsRoot == null || rigRoot == null) return;

        var list = new System.Collections.Generic.List<Bone>();
        foreach (Transform t in rigRoot.GetComponentsInChildren<Transform>(true))
        {
            if (IsWeapon(t)) continue;
            list.Add(new Bone
            {
                path = PathOf(t),
                position = t.localPosition,
                rotation = t.localRotation,
                scale = t.localScale
            });
        }
        bones = list.ToArray();
        resolved = null;
    }

    [ContextMenu("Apply pose")]
    private void ApplyFromMenu() => Apply();

    private void Resolve()
    {
        if (resolved != null && resolved.Length == bones.Length) return;
        resolved = new Transform[bones.Length];
        for (int i = 0; i < bones.Length; i++)
            resolved[i] = handsRoot.Find(bones[i].path);
    }

    private bool IsWeapon(Transform t)
    {
        for (Transform p = t; p != null && p != rigRoot; p = p.parent)
            if (p.GetComponent<Renderer>() != null || p.GetComponent<HandPose>() != null) return true;
        return false;
    }

    private string PathOf(Transform t)
    {
        string path = t.name;
        while (t.parent != null && t.parent != handsRoot)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
