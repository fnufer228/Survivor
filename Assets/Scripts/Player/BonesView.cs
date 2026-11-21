using UnityEngine;

/// <summary>
/// Показує кості рук у вікні Scene (як у Blender) і дає вибрати кістку кліком по суглобу —
/// далі її можна крутити звичайним інструментом Rotate. У грі нічого не робить.
/// </summary>
public class BonesView : MonoBehaviour
{
    [Tooltip("Корінь скелета (об'єкт *_Rig)")]
    public Transform rigRoot;
    public bool showBones = true;
    public Color boneColor = new Color(0.25f, 0.85f, 1f, 0.9f);
}
