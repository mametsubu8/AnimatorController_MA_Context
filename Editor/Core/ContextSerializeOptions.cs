namespace AnimatorControllerMAContext.Editor
{
    public class ContextSerializeOptions
    {
        public bool IncludeHierarchy { get; set; } = true;
        public bool IncludeAvatarDescriptor { get; set; } = true;
        public bool IncludeExpressionParameters { get; set; } = true;
        public bool IncludeExpressionMenu { get; set; } = true;
        public bool IncludeMAComponents { get; set; } = true;
        public bool IncludeVRCComponents { get; set; } = true;
        public bool IncludeRenderers { get; set; } = true;
        public bool IncludeAnimatorControllers { get; set; } = true;
        public bool IncludeAnimationClips { get; set; } = true;

        public static ContextSerializeOptions Default => new ContextSerializeOptions();
    }
}
