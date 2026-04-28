using System.Text;
using UnityEditor.Animations;
using UnityEngine;
using AnimatorControllerContext.Editor;

namespace AnimatorControllerMAContext.Editor
{
    public static class AvatarContextSerializer
    {
        public static string Serialize(GameObject avatarRoot)
        {
            return Serialize(avatarRoot, ContextSerializeOptions.Default);
        }

        public static string Serialize(GameObject avatarRoot, ContextSerializeOptions options)
        {
            if (avatarRoot == null)
                return "[Error] Avatar root is null.";

            if (options == null)
                options = ContextSerializeOptions.Default;

            var sb = new StringBuilder();
            var ctx = new SerializeContext(options);

            // Header
            sb.AppendLine($"=== Avatar Context: {avatarRoot.name} ===");
            sb.AppendLine();

            // Collect controller from Animator component
            var animator = avatarRoot.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController is AnimatorController rootAC)
            {
                ctx.CollectController(rootAC);
            }

            // 1. Avatar Descriptor
            if (options.IncludeAvatarDescriptor)
                VRCComponentSerializer.SerializeAvatarDescriptor(sb, avatarRoot, ctx);

            // 2. Expression Parameters
            if (options.IncludeExpressionParameters)
                VRCComponentSerializer.SerializeExpressionParameters(sb, avatarRoot, ctx);

            // 3. Expression Menu
            if (options.IncludeExpressionMenu)
                VRCComponentSerializer.SerializeExpressionMenu(sb, avatarRoot, ctx);

            // 4. Hierarchy
            if (options.IncludeHierarchy)
                HierarchySerializer.Serialize(sb, avatarRoot.transform);

            // 5. Modular Avatar Components
            if (options.IncludeMAComponents)
                MAComponentSerializer.Serialize(sb, avatarRoot, ctx);

            // 6. VRC Components (PhysBone, Contacts)
            if (options.IncludeVRCComponents)
                VRCComponentSerializer.SerializePhysComponents(sb, avatarRoot, ctx);

            // 7. Renderers (SkinnedMeshRenderer with blend shapes)
            if (options.IncludeRenderers)
                RendererSerializer.Serialize(sb, avatarRoot);

            // 8. AnimatorControllers (last, can be very long)
            if (options.IncludeAnimatorControllers)
                SerializeCollectedControllers(sb, ctx);

            return sb.ToString();
        }

        private static void SerializeCollectedControllers(StringBuilder sb, SerializeContext ctx)
        {
            if (ctx.Controllers.Count == 0) return;

            sb.AppendLine("--- AnimatorControllers ---");
            sb.AppendLine();

            var serializeOptions = new SerializeOptions
            {
                IncludeAnimationClips = ctx.Options.IncludeAnimationClips
            };

            foreach (var controller in ctx.Controllers)
            {
                if (controller == null) continue;
                sb.AppendLine(AnimatorControllerSerializer.Serialize(controller, serializeOptions));
            }
        }
    }
}
