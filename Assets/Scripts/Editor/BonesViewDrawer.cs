using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BonesViewDrawer
{
    static BonesViewDrawer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView view)
    {
        if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout
            && Event.current.type != EventType.MouseDown && Event.current.type != EventType.MouseUp
            && Event.current.type != EventType.MouseMove)
            return;

        foreach (BonesView bones in StageUtility.GetCurrentStageHandle().FindComponentsOfType<BonesView>())
        {
            if (bones == null || !bones.showBones || bones.rigRoot == null || !bones.isActiveAndEnabled)
                continue;
            Draw(bones);
        }
    }

    private static void Draw(BonesView bones)
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        foreach (Transform bone in bones.rigRoot.GetComponentsInChildren<Transform>())
        {
            if (bone == bones.rigRoot) continue;
            if (bone.GetComponent<Renderer>() != null) continue; // предмет у руці — не кістка

            bool selected = Selection.Contains(bone.gameObject);
            Handles.color = selected ? Color.yellow : bones.boneColor;

            Transform parent = bone.parent;
            if (parent != null && parent != bones.rigRoot)
                Handles.DrawLine(parent.position, bone.position, 2f);

            float size = HandleUtility.GetHandleSize(bone.position) * 0.035f;
            if (Handles.Button(bone.position, Quaternion.identity, size, size * 1.6f, Handles.SphereHandleCap))
                Selection.activeTransform = bone;
        }
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
    }
}
