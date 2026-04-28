using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AnimatorControllerMAContext.Editor
{
    public static class MAComponentSerializer
    {
        public static void Serialize(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
        {
            var allComponents = avatarRoot.GetComponentsInChildren<Component>(true);
            var maComponents = new List<(Component comp, string path)>();

            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                if (!comp.GetType().Name.StartsWith("ModularAvatar")) continue;

                string path = SerializeContext.GetRelativePath(avatarRoot.transform, comp.transform);
                maComponents.Add((comp, path));
            }

            if (maComponents.Count == 0) return;

            sb.AppendLine("--- Modular Avatar Components ---");
            sb.AppendLine();

            foreach (var (comp, path) in maComponents)
            {
                string shortName = SerializeContext.GetComponentDisplayName(comp);
                string typeName = comp.GetType().Name;

                sb.AppendLine($"  [{shortName}] @ {path}");
                SerializeMAComponent(sb, comp, typeName, ctx, "    ");
                sb.AppendLine();
            }
        }

        private static void SerializeMAComponent(StringBuilder sb, Component comp, string typeName, SerializeContext ctx, string indent)
        {
            switch (typeName)
            {
                case "ModularAvatarMergeAnimator":
                    SerializeMergeAnimator(sb, comp, ctx, indent);
                    break;
                case "ModularAvatarParameters":
                    SerializeParameters(sb, comp, indent);
                    break;
                case "ModularAvatarMenuItem":
                    SerializeMenuItem(sb, comp, indent);
                    break;
                case "ModularAvatarMenuInstaller":
                    SerializeMenuInstaller(sb, comp, indent);
                    break;
                case "ModularAvatarBlendshapeSync":
                    SerializeBlendshapeSync(sb, comp, indent);
                    break;
                case "ModularAvatarBoneProxy":
                    SerializeBoneProxy(sb, comp, indent);
                    break;
                case "ModularAvatarObjectToggle":
                    SerializeObjectToggle(sb, comp, indent);
                    break;
                case "ModularAvatarMeshSettings":
                    SerializeMeshSettings(sb, comp, indent);
                    break;
                case "ModularAvatarScaleAdjuster":
                    SerializeScaleAdjuster(sb, comp, indent);
                    break;
                case "ModularAvatarMergeBlendTree":
                    SerializeMergeBlendTree(sb, comp, ctx, indent);
                    break;
                case "ModularAvatarWorldFixedObject":
                    sb.AppendLine($"{indent}(World Fixed)");
                    break;
                case "ModularAvatarVisibleHeadAccessory":
                    sb.AppendLine($"{indent}(Visible in First Person)");
                    break;
                case "ModularAvatarMenuGroup":
                    sb.AppendLine($"{indent}(Menu Group)");
                    break;
                default:
                    // Generic fallback for unknown MA components
                    ComponentSerializer.Serialize(sb, comp, indent);
                    break;
            }
        }

        private static void SerializeMergeAnimator(StringBuilder sb, Component comp, SerializeContext ctx, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var animator = so.FindProperty("animator");
                var layerType = so.FindProperty("layerType");
                var deleteAttached = so.FindProperty("deleteAttachedAnimator");
                var pathMode = so.FindProperty("pathMode");
                var matchWD = so.FindProperty("matchAvatarWriteDefaults");
                var relativePathRoot = so.FindProperty("relativePathRoot");

                string controllerName = animator?.objectReferenceValue != null
                    ? animator.objectReferenceValue.name
                    : "(none)";
                sb.AppendLine($"{indent}Animator: {controllerName}");

                if (layerType != null)
                    sb.AppendLine($"{indent}Layer Type: {ComponentSerializer.GetEnumValue(layerType)}");
                if (deleteAttached != null)
                    sb.AppendLine($"{indent}Delete Attached Animator: {(deleteAttached.boolValue ? "true" : "false")}");
                if (pathMode != null)
                    sb.AppendLine($"{indent}Path Mode: {ComponentSerializer.GetEnumValue(pathMode)}");
                if (matchWD != null)
                    sb.AppendLine($"{indent}Match Avatar Write Defaults: {(matchWD.boolValue ? "true" : "false")}");

                // Relative path root
                if (relativePathRoot != null)
                {
                    var refPath = relativePathRoot.FindPropertyRelative("referencePath");
                    if (refPath != null && !string.IsNullOrEmpty(refPath.stringValue))
                        sb.AppendLine($"{indent}Relative Path Root: {refPath.stringValue}");
                }

                // Collect controller
                if (animator?.objectReferenceValue != null)
                    ctx.CollectController(animator.objectReferenceValue);
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeParameters(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var parameters = so.FindProperty("parameters");

                if (parameters == null || !parameters.isArray || parameters.arraySize == 0)
                {
                    sb.AppendLine($"{indent}Parameters: (none)");
                    return;
                }

                sb.AppendLine($"{indent}Parameters:");
                for (int i = 0; i < parameters.arraySize; i++)
                {
                    var param = parameters.GetArrayElementAtIndex(i);
                    var nameOrPrefix = param.FindPropertyRelative("nameOrPrefix");
                    var remapTo = param.FindPropertyRelative("remapTo");
                    var internalParameter = param.FindPropertyRelative("internalParameter");
                    var isPrefix = param.FindPropertyRelative("isPrefix");
                    var syncType = param.FindPropertyRelative("syncType");
                    var localOnly = param.FindPropertyRelative("localOnly");
                    var defaultValue = param.FindPropertyRelative("defaultValue");
                    var saved = param.FindPropertyRelative("saved");
                    var hasExplicitDefault = param.FindPropertyRelative("hasExplicitDefaultValue");

                    string name = nameOrPrefix?.stringValue ?? "(unnamed)";
                    string sync = syncType != null ? ComponentSerializer.GetEnumValue(syncType) : "?";

                    var line = new StringBuilder();
                    line.Append($"{indent}  {name} [{sync}]");

                    var extras = new List<string>();
                    if (hasExplicitDefault != null && hasExplicitDefault.boolValue && defaultValue != null)
                        extras.Add($"Default: {defaultValue.floatValue:G}");
                    if (saved != null && saved.boolValue)
                        extras.Add("Saved");
                    if (localOnly != null && localOnly.boolValue)
                        extras.Add("LocalOnly");
                    if (internalParameter != null && internalParameter.boolValue)
                        extras.Add("Internal");
                    if (isPrefix != null && isPrefix.boolValue)
                        extras.Add("Prefix");

                    if (extras.Count > 0)
                        line.Append($" ({string.Join(", ", extras)})");

                    sb.AppendLine(line.ToString());

                    if (remapTo != null && !string.IsNullOrEmpty(remapTo.stringValue))
                        sb.AppendLine($"{indent}    Remap To: {remapTo.stringValue}");
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeMenuItem(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var control = so.FindProperty("Control");
                if (control == null)
                {
                    ComponentSerializer.Serialize(sb, comp, indent);
                    return;
                }

                var type = control.FindPropertyRelative("type");
                var parameter = control.FindPropertyRelative("parameter");
                var value = control.FindPropertyRelative("value");
                var subParameters = control.FindPropertyRelative("subParameters");

                if (type != null)
                    sb.AppendLine($"{indent}Type: {ComponentSerializer.GetEnumValue(type)}");

                if (parameter != null)
                {
                    var paramName = parameter.FindPropertyRelative("name");
                    if (paramName != null && !string.IsNullOrEmpty(paramName.stringValue))
                    {
                        float val = value != null ? value.floatValue : 0;
                        sb.AppendLine($"{indent}Parameter: {paramName.stringValue} = {val:G}");
                    }
                }

                if (subParameters != null && subParameters.isArray && subParameters.arraySize > 0)
                {
                    sb.AppendLine($"{indent}SubParameters:");
                    for (int i = 0; i < subParameters.arraySize; i++)
                    {
                        var subParam = subParameters.GetArrayElementAtIndex(i);
                        var subParamName = subParam?.FindPropertyRelative("name");
                        if (subParamName != null && !string.IsNullOrEmpty(subParamName.stringValue))
                            sb.AppendLine($"{indent}  {subParamName.stringValue}");
                    }
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeMenuInstaller(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var menuToAppend = so.FindProperty("menuToAppend");
                if (menuToAppend?.objectReferenceValue != null)
                    sb.AppendLine($"{indent}Menu To Append: {menuToAppend.objectReferenceValue.name}");

                var installTarget = so.FindProperty("installTargetMenu");
                if (installTarget?.objectReferenceValue != null)
                    sb.AppendLine($"{indent}Install Target: {installTarget.objectReferenceValue.name}");
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeBlendshapeSync(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var bindings = so.FindProperty("Bindings");

                if (bindings == null || !bindings.isArray || bindings.arraySize == 0)
                {
                    sb.AppendLine($"{indent}Bindings: (none)");
                    return;
                }

                sb.AppendLine($"{indent}Bindings:");
                for (int i = 0; i < bindings.arraySize; i++)
                {
                    var binding = bindings.GetArrayElementAtIndex(i);
                    var refMesh = binding.FindPropertyRelative("ReferenceMesh");
                    var blendshape = binding.FindPropertyRelative("Blendshape");
                    var localBlendshape = binding.FindPropertyRelative("LocalBlendshape");

                    string meshPath = "";
                    if (refMesh != null)
                    {
                        var refPath = refMesh.FindPropertyRelative("referencePath");
                        if (refPath != null)
                            meshPath = refPath.stringValue;
                    }

                    string srcShape = blendshape?.stringValue ?? "";
                    string dstShape = localBlendshape?.stringValue ?? "";

                    if (string.IsNullOrEmpty(dstShape) || dstShape == srcShape)
                        sb.AppendLine($"{indent}  {meshPath} :: {srcShape}");
                    else
                        sb.AppendLine($"{indent}  {meshPath} :: {srcShape} -> {dstShape}");
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeBoneProxy(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var target = so.FindProperty("target");
                if (target != null)
                {
                    var refPath = target.FindPropertyRelative("referencePath");
                    if (refPath != null && !string.IsNullOrEmpty(refPath.stringValue))
                        sb.AppendLine($"{indent}Target: {refPath.stringValue}");
                }

                var attachmentMode = so.FindProperty("attachmentMode");
                if (attachmentMode != null)
                    sb.AppendLine($"{indent}Attachment Mode: {ComponentSerializer.GetEnumValue(attachmentMode)}");
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeObjectToggle(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var objects = so.FindProperty("Objects");

                if (objects == null || !objects.isArray || objects.arraySize == 0)
                {
                    // Try alternative property name
                    objects = so.FindProperty("m_objects");
                    if (objects == null || !objects.isArray || objects.arraySize == 0)
                    {
                        sb.AppendLine($"{indent}Objects: (none)");
                        return;
                    }
                }

                sb.AppendLine($"{indent}Objects:");
                for (int i = 0; i < objects.arraySize; i++)
                {
                    var entry = objects.GetArrayElementAtIndex(i);

                    // Try different property structures
                    var objRef = entry.FindPropertyRelative("Object")
                              ?? entry.FindPropertyRelative("target");
                    var active = entry.FindPropertyRelative("Active")
                              ?? entry.FindPropertyRelative("active");

                    string path = "(unknown)";
                    if (objRef != null)
                    {
                        var refPath = objRef.FindPropertyRelative("referencePath");
                        if (refPath != null && !string.IsNullOrEmpty(refPath.stringValue))
                            path = refPath.stringValue;
                        else if (objRef.objectReferenceValue != null)
                            path = objRef.objectReferenceValue.name;
                    }

                    string state = active != null ? (active.boolValue ? "ON" : "OFF") : "?";
                    sb.AppendLine($"{indent}  {path} -> {state}");
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeMeshSettings(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var inheritBounds = so.FindProperty("InheritBounds");
                if (inheritBounds != null)
                {
                    sb.AppendLine($"{indent}Inherit Bounds: {ComponentSerializer.GetEnumValue(inheritBounds)}");
                }

                var inheritProbeAnchor = so.FindProperty("InheritProbeAnchor");
                if (inheritProbeAnchor != null)
                {
                    sb.AppendLine($"{indent}Inherit Probe Anchor: {ComponentSerializer.GetEnumValue(inheritProbeAnchor)}");
                }

                var probeAnchor = so.FindProperty("ProbeAnchor");
                if (probeAnchor != null)
                {
                    var refPath = probeAnchor.FindPropertyRelative("referencePath");
                    if (refPath != null && !string.IsNullOrEmpty(refPath.stringValue))
                        sb.AppendLine($"{indent}Probe Anchor: {refPath.stringValue}");
                }

                var setBounds = so.FindProperty("Bounds");
                if (setBounds != null)
                {
                    var center = setBounds.FindPropertyRelative("m_Center");
                    var extent = setBounds.FindPropertyRelative("m_Extent");
                    if (center != null && extent != null)
                    {
                        var c = center.vector3Value;
                        var e = extent.vector3Value;
                        if (c != Vector3.zero || e != Vector3.zero)
                            sb.AppendLine($"{indent}Bounds: Center({c.x:G}, {c.y:G}, {c.z:G}) Extent({e.x:G}, {e.y:G}, {e.z:G})");
                    }
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeScaleAdjuster(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var scale = so.FindProperty("m_Scale");
                if (scale != null)
                {
                    var v = scale.vector3Value;
                    sb.AppendLine($"{indent}Scale: ({v.x:G}, {v.y:G}, {v.z:G})");
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeMergeBlendTree(StringBuilder sb, Component comp, SerializeContext ctx, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                var blendTree = so.FindProperty("BlendTree");
                if (blendTree == null)
                    blendTree = so.FindProperty("m_blendTree");

                if (blendTree?.objectReferenceValue != null)
                {
                    sb.AppendLine($"{indent}BlendTree: {blendTree.objectReferenceValue.name}");
                }

                var pathMode = so.FindProperty("pathMode");
                if (pathMode != null)
                    sb.AppendLine($"{indent}Path Mode: {ComponentSerializer.GetEnumValue(pathMode)}");

                var layerType = so.FindProperty("layerType");
                if (layerType != null)
                    sb.AppendLine($"{indent}Layer Type: {ComponentSerializer.GetEnumValue(layerType)}");
            }
            finally
            {
                so.Dispose();
            }
        }
    }
}
