using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AnimatorControllerMAContext.Editor
{
    public static class VRCComponentSerializer
    {
        private const int VrcIntParameterCost = 8;
        private const int VrcFloatParameterCost = 8;
        private const int VrcBoolParameterCost = 1;
        private const int VrcMaxParameterCost = 256;

        public static void SerializeAvatarDescriptor(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
        {
            var descriptor = FindComponentByTypeName(avatarRoot, "VRCAvatarDescriptor");
            if (descriptor == null) return;

            var so = new SerializedObject(descriptor);
            try
            {
                sb.AppendLine("--- Avatar Descriptor ---");

                // View Position
                var viewPos = so.FindProperty("ViewPosition");
                if (viewPos != null)
                {
                    var v = viewPos.vector3Value;
                    sb.AppendLine($"  View Position: ({v.x:G}, {v.y:G}, {v.z:G})");
                }

                // Lip Sync
                var lipSync = so.FindProperty("lipSync");
                if (lipSync != null)
                    sb.AppendLine($"  Lip Sync: {ComponentSerializer.GetEnumValue(lipSync)}");

                var visemeMesh = so.FindProperty("VisemeSkinnedMesh");
                if (visemeMesh != null && visemeMesh.objectReferenceValue != null)
                    sb.AppendLine($"  Lip Sync Mesh: {visemeMesh.objectReferenceValue.name}");

                // Eye Look
                var eyeLook = so.FindProperty("enableEyeLook");
                if (eyeLook != null)
                    sb.AppendLine($"  Eye Look: {(eyeLook.boolValue ? "Enabled" : "Disabled")}");

                // Auto Footsteps / Locomotion
                var autoFootsteps = so.FindProperty("autoFootsteps");
                if (autoFootsteps != null)
                    sb.AppendLine($"  Auto Footsteps: {(autoFootsteps.boolValue ? "true" : "false")}");
                var autoLocomotion = so.FindProperty("autoLocomotion");
                if (autoLocomotion != null)
                    sb.AppendLine($"  Auto Locomotion: {(autoLocomotion.boolValue ? "true" : "false")}");

                sb.AppendLine();

                // Playable Layers
                SerializePlayableLayers(sb, so, ctx);
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializePlayableLayers(StringBuilder sb, SerializedObject so, SerializeContext ctx)
        {
            var customizeLayers = so.FindProperty("customizeAnimationLayers");
            if (customizeLayers != null && !customizeLayers.boolValue)
            {
                sb.AppendLine("--- Playable Layers ---");
                sb.AppendLine("  (using defaults)");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("--- Playable Layers ---");

            SerializeLayerArray(sb, so.FindProperty("baseAnimationLayers"), ctx);
            SerializeLayerArray(sb, so.FindProperty("specialAnimationLayers"), ctx);

            sb.AppendLine();
        }

        private static void SerializeLayerArray(StringBuilder sb, SerializedProperty layers, SerializeContext ctx)
        {
            if (layers == null || !layers.isArray) return;

            for (int i = 0; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                var type = layer.FindPropertyRelative("type");
                var controller = layer.FindPropertyRelative("animatorController");
                var isDefault = layer.FindPropertyRelative("isDefault");
                var isEnabled = layer.FindPropertyRelative("isEnabled");
                var mask = layer.FindPropertyRelative("mask");

                string typeName = ComponentSerializer.GetEnumValue(type);

                if (isDefault != null && isDefault.boolValue)
                {
                    sb.AppendLine($"  [{typeName}] (default)");
                }
                else if (controller != null && controller.objectReferenceValue != null)
                {
                    string ctrlName = controller.objectReferenceValue.name;
                    string maskInfo = "";
                    if (mask != null && mask.objectReferenceValue != null)
                        maskInfo = $" (Mask: {mask.objectReferenceValue.name})";
                    sb.AppendLine($"  [{typeName}] {ctrlName}{maskInfo}");

                    ctx.CollectController(controller.objectReferenceValue);
                }
                else
                {
                    string enabled = (isEnabled != null && !isEnabled.boolValue) ? " [disabled]" : "";
                    sb.AppendLine($"  [{typeName}] (none){enabled}");
                }
            }
        }

        public static void SerializeExpressionParameters(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
        {
            var descriptor = FindComponentByTypeName(avatarRoot, "VRCAvatarDescriptor");
            if (descriptor == null) return;

            var so = new SerializedObject(descriptor);
            try
            {
                var customExpr = so.FindProperty("customExpressions");
                if (customExpr != null && !customExpr.boolValue)
                {
                    return;
                }

                var exprParams = so.FindProperty("expressionParameters");
                if (exprParams == null || exprParams.objectReferenceValue == null)
                {
                    return;
                }

                var paramsSO = new SerializedObject(exprParams.objectReferenceValue);
                try
                {
                    var parameters = paramsSO.FindProperty("parameters");

                    if (parameters == null || !parameters.isArray || parameters.arraySize == 0)
                    {
                        return;
                    }

                    int totalCost = 0;
                    var paramLines = new List<string>();

                    for (int i = 0; i < parameters.arraySize; i++)
                    {
                        var param = parameters.GetArrayElementAtIndex(i);
                        var name = param.FindPropertyRelative("name");
                        var valueType = param.FindPropertyRelative("valueType");
                        var defaultValue = param.FindPropertyRelative("defaultValue");
                        var saved = param.FindPropertyRelative("saved");
                        var networkSynced = param.FindPropertyRelative("networkSynced");

                        if (name == null || string.IsNullOrEmpty(name.stringValue)) continue;

                        string typeName = ComponentSerializer.GetEnumValue(valueType);
                        float defVal = defaultValue != null ? defaultValue.floatValue : 0;
                        bool isSaved = saved != null && saved.boolValue;
                        bool isSynced = networkSynced != null && networkSynced.boolValue;

                        var line = new StringBuilder();
                        line.Append($"  [{typeName}] {name.stringValue} (Default: {defVal:G}");
                        if (isSaved) line.Append(", Saved");
                        if (isSynced) line.Append(", Synced");
                        line.Append(")");
                        paramLines.Add(line.ToString());

                        if (isSynced && valueType != null)
                        {
                            totalCost += GetParameterCost(valueType.enumValueIndex);
                        }
                    }

                    sb.AppendLine($"--- Expression Parameters (Cost: {totalCost}/{VrcMaxParameterCost}) ---");
                    foreach (var line in paramLines)
                        sb.AppendLine(line);
                    sb.AppendLine();
                }
                finally
                {
                    paramsSO.Dispose();
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        public static void SerializeExpressionMenu(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
        {
            var descriptor = FindComponentByTypeName(avatarRoot, "VRCAvatarDescriptor");
            if (descriptor == null) return;

            var so = new SerializedObject(descriptor);
            try
            {
                var customExpr = so.FindProperty("customExpressions");
                if (customExpr != null && !customExpr.boolValue)
                {
                    return;
                }

                var exprMenu = so.FindProperty("expressionsMenu");
                if (exprMenu == null || exprMenu.objectReferenceValue == null)
                {
                    return;
                }

                sb.AppendLine("--- Expression Menu ---");
                ctx.VisitedMenus.Clear();
                SerializeMenuAsset(sb, exprMenu.objectReferenceValue, "  ", ctx);
                sb.AppendLine();
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeMenuAsset(StringBuilder sb, Object menuAsset, string indent, SerializeContext ctx)
        {
            if (menuAsset == null || ctx.VisitedMenus.Contains(menuAsset)) return;
            ctx.VisitedMenus.Add(menuAsset);

            var menuSO = new SerializedObject(menuAsset);
            try
            {
                var controls = menuSO.FindProperty("controls");

                if (controls == null || !controls.isArray)
                {
                    return;
                }

                for (int i = 0; i < controls.arraySize; i++)
                {
                    var control = controls.GetArrayElementAtIndex(i);
                    var name = control.FindPropertyRelative("name");
                    var type = control.FindPropertyRelative("type");
                    var parameter = control.FindPropertyRelative("parameter");
                    var value = control.FindPropertyRelative("value");
                    var subMenu = control.FindPropertyRelative("subMenu");
                    var subParameters = control.FindPropertyRelative("subParameters");

                    string controlName = name?.stringValue ?? "(unnamed)";
                    string typeName = ComponentSerializer.GetEnumValue(type);

                    string paramName = "";
                    if (parameter != null)
                    {
                        var paramNameProp = parameter.FindPropertyRelative("name");
                        if (paramNameProp != null)
                            paramName = paramNameProp.stringValue;
                    }

                    string paramInfo = "";
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        float val = value != null ? value.floatValue : 0;
                        paramInfo = $" (Parameter: {paramName} = {val:G})";
                    }

                    sb.AppendLine($"{indent}[{typeName}] {controlName}{paramInfo}");

                    // Sub parameters (for puppets)
                    if (subParameters != null && subParameters.isArray)
                    {
                        for (int j = 0; j < subParameters.arraySize; j++)
                        {
                            var subParam = subParameters.GetArrayElementAtIndex(j);
                            var subParamName = subParam?.FindPropertyRelative("name");
                            if (subParamName != null && !string.IsNullOrEmpty(subParamName.stringValue))
                                sb.AppendLine($"{indent}  SubParameter: {subParamName.stringValue}");
                        }
                    }

                    // SubMenu recursion
                    if (subMenu != null && subMenu.objectReferenceValue != null)
                    {
                        SerializeMenuAsset(sb, subMenu.objectReferenceValue, indent + "  ", ctx);
                    }
                }
            }
            finally
            {
                menuSO.Dispose();
            }
        }

        public static void SerializePhysComponents(StringBuilder sb, GameObject avatarRoot, SerializeContext ctx)
        {
            var allComponents = avatarRoot.GetComponentsInChildren<Component>(true);

            var physBones = new List<(Component comp, string path)>();
            var physBoneColliders = new List<(Component comp, string path)>();
            var contactSenders = new List<(Component comp, string path)>();
            var contactReceivers = new List<(Component comp, string path)>();

            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                string path = SerializeContext.GetRelativePath(avatarRoot.transform, comp.transform);

                switch (typeName)
                {
                    case "VRCPhysBone":
                        physBones.Add((comp, path));
                        break;
                    case "VRCPhysBoneCollider":
                        physBoneColliders.Add((comp, path));
                        break;
                    case "VRCContactSender":
                        contactSenders.Add((comp, path));
                        break;
                    case "VRCContactReceiver":
                        contactReceivers.Add((comp, path));
                        break;
                }
            }

            // PhysBones
            if (physBones.Count > 0 || physBoneColliders.Count > 0)
            {
                sb.AppendLine("--- PhysBones ---");
                sb.AppendLine();

                foreach (var (comp, path) in physBones)
                {
                    sb.AppendLine($"  [PhysBone] @ {path}");
                    SerializePhysBone(sb, comp, "    ");
                    sb.AppendLine();
                }

                foreach (var (comp, path) in physBoneColliders)
                {
                    sb.AppendLine($"  [PhysBone Collider] @ {path}");
                    SerializePhysBoneCollider(sb, comp, "    ");
                    sb.AppendLine();
                }
            }

            // Contacts
            if (contactSenders.Count > 0 || contactReceivers.Count > 0)
            {
                sb.AppendLine("--- Contacts ---");
                sb.AppendLine();

                foreach (var (comp, path) in contactSenders)
                {
                    sb.AppendLine($"  [Contact Sender] @ {path}");
                    SerializeContact(sb, comp, "    ");
                    sb.AppendLine();
                }

                foreach (var (comp, path) in contactReceivers)
                {
                    sb.AppendLine($"  [Contact Receiver] @ {path}");
                    SerializeContact(sb, comp, "    ");
                    sb.AppendLine();
                }
            }
        }

        private static void SerializePhysBone(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                // Root Transform
                var rootTransform = so.FindProperty("rootTransform");
                string rootName = rootTransform?.objectReferenceValue != null
                    ? rootTransform.objectReferenceValue.name
                    : "(self)";
                sb.AppendLine($"{indent}Root Transform: {rootName}");

                // Integration Type
                SerializeEnumProp(sb, so, "integrationType", "Integration Type", indent);

                // Physics properties
                SerializeFloatProp(sb, so, "pull", "Pull", indent);
                SerializeFloatProp(sb, so, "spring", "Spring", indent);
                SerializeFloatProp(sb, so, "stiffness", "Stiffness", indent);
                SerializeFloatProp(sb, so, "gravity", "Gravity", indent);
                SerializeFloatProp(sb, so, "gravityFalloff", "Gravity Falloff", indent);

                // Immobile
                SerializeEnumProp(sb, so, "immobileType", "Immobile Type", indent);
                SerializeFloatProp(sb, so, "immobile", "Immobile", indent);

                // Limits
                SerializeEnumProp(sb, so, "limitType", "Limit Type", indent);
                var limitType = so.FindProperty("limitType");
                if (limitType != null && limitType.enumValueIndex > 0)
                {
                    SerializeFloatProp(sb, so, "maxAngleX", "Max Angle X", indent);
                    SerializeFloatProp(sb, so, "maxAngleZ", "Max Angle Z", indent);
                }

                // Radius & Endpoint
                SerializeFloatProp(sb, so, "radius", "Radius", indent);
                SerializeVector3PropIfNonZero(sb, so, "endpointPosition", "Endpoint", indent);

                // Collision & Interaction
                SerializeEnumProp(sb, so, "allowCollision", "Allow Collision", indent);
                SerializeEnumProp(sb, so, "allowGrabbing", "Allow Grabbing", indent);
                SerializeEnumProp(sb, so, "allowPosing", "Allow Posing", indent);

                // Parameter
                SerializeStringPropIfNotEmpty(sb, so, "parameter", "Parameter", indent);

                // Colliders
                var colliders = so.FindProperty("colliders");
                if (colliders != null && colliders.isArray && colliders.arraySize > 0)
                {
                    sb.Append($"{indent}Colliders: ");
                    var names = new List<string>();
                    for (int i = 0; i < colliders.arraySize; i++)
                    {
                        var col = colliders.GetArrayElementAtIndex(i);
                        if (col.objectReferenceValue != null)
                            names.Add(col.objectReferenceValue.name);
                    }
                    sb.AppendLine(names.Count > 0 ? string.Join(", ", names) : "(none)");
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializePhysBoneCollider(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                SerializeObjectRefProp(sb, so, "rootTransform", "Root Transform", indent);
                SerializeShapeProperties(sb, so, indent);

                SerializeQuaternionPropIfNonZero(sb, so, "rotation", "Rotation", indent);

                var insideBounds = so.FindProperty("insideBounds");
                if (insideBounds != null && insideBounds.boolValue)
                    sb.AppendLine($"{indent}Inside Bounds: true");
            }
            finally
            {
                so.Dispose();
            }
        }

        private static void SerializeContact(StringBuilder sb, Component comp, string indent)
        {
            var so = new SerializedObject(comp);
            try
            {
                SerializeObjectRefProp(sb, so, "rootTransform", "Root Transform", indent);
                SerializeShapeProperties(sb, so, indent);

                // Parameter (Contact Receiver)
                SerializeStringPropIfNotEmpty(sb, so, "parameter", "Parameter", indent);

                // Receiver Type
                SerializeEnumProp(sb, so, "receiverType", "Receiver Type", indent);

                // Allow Self / Others
                var allowSelf = so.FindProperty("allowSelf");
                if (allowSelf != null)
                    sb.AppendLine($"{indent}Allow Self: {(allowSelf.boolValue ? "true" : "false")}");
                var allowOthers = so.FindProperty("allowOthers");
                if (allowOthers != null)
                    sb.AppendLine($"{indent}Allow Others: {(allowOthers.boolValue ? "true" : "false")}");

                // Local Only
                var localOnly = so.FindProperty("localOnly");
                if (localOnly != null && localOnly.boolValue)
                    sb.AppendLine($"{indent}Local Only: true");

                // Collision Tags
                var collisionTags = so.FindProperty("collisionTags");
                if (collisionTags != null && collisionTags.isArray && collisionTags.arraySize > 0)
                {
                    var tags = new List<string>();
                    for (int i = 0; i < collisionTags.arraySize; i++)
                    {
                        var tag = collisionTags.GetArrayElementAtIndex(i);
                        if (!string.IsNullOrEmpty(tag.stringValue))
                            tags.Add(tag.stringValue);
                    }
                    if (tags.Count > 0)
                        sb.AppendLine($"{indent}Collision Tags: {string.Join(", ", tags)}");
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        /// <summary>
        /// Serializes shape properties common to PhysBoneCollider and Contact components.
        /// </summary>
        private static void SerializeShapeProperties(StringBuilder sb, SerializedObject so, string indent)
        {
            SerializeEnumProp(sb, so, "shapeType", "Shape Type", indent);
            SerializeFloatProp(sb, so, "radius", "Radius", indent);
            SerializeFloatProp(sb, so, "height", "Height", indent);
            SerializeVector3PropIfNonZero(sb, so, "position", "Position", indent);
        }

        #region Helpers

        private static int GetParameterCost(int valueTypeIndex)
        {
            switch (valueTypeIndex)
            {
                case 0: return VrcIntParameterCost;
                case 1: return VrcFloatParameterCost;
                case 2: return VrcBoolParameterCost;
                default: return 0;
            }
        }

        private static void SerializeFloatProp(StringBuilder sb, SerializedObject so, string propName, string displayName, string indent)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                sb.AppendLine($"{indent}{displayName}: {prop.floatValue:G}");
        }

        private static void SerializeEnumProp(StringBuilder sb, SerializedObject so, string propName, string displayName, string indent)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                sb.AppendLine($"{indent}{displayName}: {ComponentSerializer.GetEnumValue(prop)}");
        }

        private static void SerializeStringPropIfNotEmpty(StringBuilder sb, SerializedObject so, string propName, string displayName, string indent)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && !string.IsNullOrEmpty(prop.stringValue))
                sb.AppendLine($"{indent}{displayName}: {prop.stringValue}");
        }

        private static void SerializeObjectRefProp(StringBuilder sb, SerializedObject so, string propName, string displayName, string indent)
        {
            var prop = so.FindProperty(propName);
            if (prop?.objectReferenceValue != null)
                sb.AppendLine($"{indent}{displayName}: {prop.objectReferenceValue.name}");
        }

        private static void SerializeVector3PropIfNonZero(StringBuilder sb, SerializedObject so, string propName, string displayName, string indent)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                var v = prop.vector3Value;
                if (v != Vector3.zero)
                    sb.AppendLine($"{indent}{displayName}: ({v.x:G}, {v.y:G}, {v.z:G})");
            }
        }

        private static void SerializeQuaternionPropIfNonZero(StringBuilder sb, SerializedObject so, string propName, string displayName, string indent)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                var euler = prop.quaternionValue.eulerAngles;
                if (euler != Vector3.zero)
                    sb.AppendLine($"{indent}{displayName}: ({euler.x:G}, {euler.y:G}, {euler.z:G})");
            }
        }

        private static Component FindComponentByTypeName(GameObject go, string typeName)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp != null && comp.GetType().Name == typeName)
                    return comp;
            }
            return null;
        }

        #endregion
    }
}
