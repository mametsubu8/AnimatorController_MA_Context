using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AnimatorControllerMAContext.Editor
{
    public static class RendererSerializer
    {
        public static void Serialize(StringBuilder sb, GameObject avatarRoot)
        {
            var renderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0) return;

            sb.AppendLine("--- Renderers ---");
            sb.AppendLine();

            foreach (var smr in renderers)
            {
                string path = SerializeContext.GetRelativePath(avatarRoot.transform, smr.transform);
                string inactive = smr.gameObject.activeSelf ? "" : " [inactive]";
                string enabled = smr.enabled ? "" : " [disabled]";

                sb.AppendLine($"  [SkinnedMeshRenderer] @ {path}{inactive}{enabled}");

                // Mesh info
                if (smr.sharedMesh != null)
                {
                    sb.AppendLine($"    Mesh: {smr.sharedMesh.name}");

                    // Blend shapes
                    int blendShapeCount = smr.sharedMesh.blendShapeCount;
                    if (blendShapeCount > 0)
                    {
                        sb.AppendLine($"    BlendShapes ({blendShapeCount}):");
                        for (int i = 0; i < blendShapeCount; i++)
                        {
                            string shapeName = smr.sharedMesh.GetBlendShapeName(i);
                            float weight = smr.GetBlendShapeWeight(i);

                            if (weight != 0)
                                sb.AppendLine($"      {i}: {shapeName} = {weight:G}");
                            else
                                sb.AppendLine($"      {i}: {shapeName}");
                        }
                    }
                }

                // Materials
                if (smr.sharedMaterials != null && smr.sharedMaterials.Length > 0)
                {
                    var matNames = new List<string>();
                    foreach (var mat in smr.sharedMaterials)
                    {
                        matNames.Add(mat != null ? mat.name : "(none)");
                    }
                    sb.AppendLine($"    Materials: {string.Join(", ", matNames)}");
                }

                sb.AppendLine();
            }
        }
    }
}
