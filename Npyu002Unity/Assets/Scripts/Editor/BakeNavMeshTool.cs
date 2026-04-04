using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;

public static class BakeNavMeshTool
{
    [MenuItem("Tools/ActionGame/Bake NavMesh")]
    public static void BakeNavMesh()
    {
        var ground = GameObject.Find("Ground");
        if (ground == null)
        {
            Debug.LogError("[BakeNavMesh] 'Ground' not found");
            return;
        }

        // NavMeshSurface を追加してベイク（Unity 6 / AI Navigation package）
        var surface = ground.GetComponent<NavMeshSurface>() ?? ground.AddComponent<NavMeshSurface>();
        surface.BuildNavMesh();

        EditorUtility.SetDirty(ground);
        Debug.Log("[BakeNavMesh] Done!");
    }
}
