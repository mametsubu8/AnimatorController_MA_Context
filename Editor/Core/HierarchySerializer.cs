using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AnimatorControllerMAContext.Editor
{
    public static class HierarchySerializer
    {
        public static void Serialize(StringBuilder sb, Transform root)
        {
            sb.AppendLine("--- Hierarchy ---");
            SerializeTransform(sb, root, "  ");
            sb.AppendLine();
        }

        private static void SerializeTransform(StringBuilder sb, Transform transform, string indent)
        {
            string name = transform.name;
            string components = GetComponentAnnotation(transform.gameObject);
            string inactive = transform.gameObject.activeSelf ? "" : " [inactive]";

            if (!string.IsNullOrEmpty(components))
                sb.AppendLine($"{indent}{name}{inactive} [{components}]");
            else
                sb.AppendLine($"{indent}{name}{inactive}");

            for (int i = 0; i < transform.childCount; i++)
            {
                SerializeTransform(sb, transform.GetChild(i), indent + "  ");
            }
        }

        private static string GetComponentAnnotation(GameObject go)
        {
            var components = go.GetComponents<Component>();
            var names = new List<string>();

            foreach (var comp in components)
            {
                if (comp == null)
                {
                    names.Add("(Missing Script)");
                    continue;
                }

                if (comp is Transform) continue;

                string displayName = SerializeContext.GetComponentDisplayName(comp);

                if (comp is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                {
                    displayName += $" ({smr.sharedMesh.blendShapeCount})";
                }

                names.Add(displayName);
            }

            return names.Count > 0 ? string.Join(", ", names) : "";
        }
    }
}
