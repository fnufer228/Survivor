using UnityEngine;

public class BonesView : MonoBehaviour
{
    [Tooltip("Корінь скелета (об'єкт *_Rig)")]
    public Transform rigRoot;
    public bool showBones = true;
    public Color boneColor = new Color(0.25f, 0.85f, 1f, 0.9f);
}
