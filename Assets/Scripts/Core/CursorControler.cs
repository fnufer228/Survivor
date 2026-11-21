using System.Collections.Generic;
using UnityEngine;

public static class CursorController
{
    static readonly HashSet<object> owners = new HashSet<object>();

    public static bool IsVisible => owners.Count > 0;

    public static event System.Action<bool> VisibilityChanged;

    public static void Show(object owner)
    {
        if (owner != null && owners.Add(owner)) Apply();
    }

    public static void Hide(object owner)
    {
        if (owner != null && owners.Remove(owner)) Apply();
    }

    public static void Set(object owner, bool visible)
    {
        if (visible) Show(owner);
        else Hide(owner);
    }

    public static void Clear()
    {
        owners.Clear();
        Apply();
    }

    static void Apply()
    {
        bool wasVisible = Cursor.visible;

        owners.RemoveWhere(o => o is Object unityObject && unityObject == null);

        bool visible = owners.Count > 0;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;

        if (wasVisible != visible) VisibilityChanged?.Invoke(visible);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        owners.Clear();
        VisibilityChanged = null;
        Application.focusChanged -= OnFocusChanged;
        Application.focusChanged += OnFocusChanged;
    }

    static void OnFocusChanged(bool focused)
    {
        if (focused) Apply();
    }
}
