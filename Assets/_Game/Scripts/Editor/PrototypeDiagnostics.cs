using BossNoTakeAnyMore.UI;
using UnityEditor;
using UnityEngine;

namespace BossNoTakeAnyMore.EditorTools
{
    public static class PrototypeDiagnostics
    {
        [MenuItem("Warehouse/Inspect Runtime Surface")]
        private static void Inspect()
        {
            CargoSurface surface = Object.FindObjectOfType<CargoSurface>();
            if (surface == null) { Debug.Log("Prototype surface not running"); return; }
            Canvas.ForceUpdateCanvases();
            Mesh mesh = surface.canvasRenderer.GetMesh();
            Debug.Log($"Surface active={surface.isActiveAndEnabled}, rect={surface.rectTransform.rect}, vertices={mesh.vertexCount}, culled={surface.canvasRenderer.cull}, color={surface.canvasRenderer.GetColor()}, material={surface.material}");
            Selection.activeGameObject = surface.gameObject;
        }
    }
}
