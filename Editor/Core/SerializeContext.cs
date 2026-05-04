using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace AnimatorControllerMAContext.Editor
{
    public class SerializeContext
    {
        public ContextSerializeOptions Options { get; }
        public HashSet<AnimatorController> Controllers { get; } = new HashSet<AnimatorController>();
        public HashSet<Object> VisitedMenus { get; } = new HashSet<Object>();

        public SerializeContext(ContextSerializeOptions options)
        {
            Options = options ?? ContextSerializeOptions.Default;
        }

        public void CollectController(Object obj)
        {
            if (obj is AnimatorController ac)
                Controllers.Add(ac);
        }

        public static string GetRelativePath(Transform root, Transform target)
        {
            if (target == root) return root.name;

            var parts = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        public static string GetComponentDisplayName(Component comp)
        {
            if (comp == null) return "(Missing Script)";

            string name = comp.GetType().Name;
            if (name.StartsWith("ModularAvatar") && name.Length > "ModularAvatar".Length)
                return "MA" + name.Substring("ModularAvatar".Length);
            return name;
        }
    }
}
