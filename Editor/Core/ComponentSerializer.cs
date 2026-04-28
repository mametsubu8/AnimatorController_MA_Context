using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AnimatorControllerMAContext.Editor
{
    public static class ComponentSerializer
    {
        private const int MaxArrayDisplayCount = 50;

        private static readonly HashSet<string> SkippedPropertyNames = new HashSet<string>
        {
            "m_ObjectHideFlags", "m_Script", "m_Name", "m_EditorHideFlags",
            "m_EditorClassIdentifier", "m_GameObject", "m_CorrespondingSourceObject",
            "m_PrefabInstance", "m_PrefabAsset"
        };

        public static void Serialize(StringBuilder sb, Component component, string indent)
        {
            if (component == null) return;

            var so = new SerializedObject(component);
            try
            {
                var prop = so.GetIterator();
                bool entered = prop.NextVisible(true);

                while (entered)
                {
                    if (!IsSkippedProperty(prop.name))
                    {
                        SerializeProperty(sb, prop, indent);
                    }
                    entered = prop.NextVisible(false);
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        public static void SerializeProperty(StringBuilder sb, SerializedProperty prop, string indent)
        {
            if (prop == null) return;

            string displayName = prop.displayName;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    sb.AppendLine($"{indent}{displayName}: {prop.intValue}");
                    break;
                case SerializedPropertyType.Boolean:
                    sb.AppendLine($"{indent}{displayName}: {(prop.boolValue ? "true" : "false")}");
                    break;
                case SerializedPropertyType.Float:
                    sb.AppendLine($"{indent}{displayName}: {prop.floatValue:G}");
                    break;
                case SerializedPropertyType.String:
                    sb.AppendLine($"{indent}{displayName}: {prop.stringValue}");
                    break;
                case SerializedPropertyType.Enum:
                    sb.AppendLine($"{indent}{displayName}: {GetEnumValue(prop)}");
                    break;
                case SerializedPropertyType.ObjectReference:
                    string objName = prop.objectReferenceValue != null
                        ? prop.objectReferenceValue.name
                        : "(none)";
                    sb.AppendLine($"{indent}{displayName}: {objName}");
                    break;
                case SerializedPropertyType.Vector2:
                    var v2 = prop.vector2Value;
                    sb.AppendLine($"{indent}{displayName}: ({v2.x:G}, {v2.y:G})");
                    break;
                case SerializedPropertyType.Vector3:
                    var v3 = prop.vector3Value;
                    sb.AppendLine($"{indent}{displayName}: ({v3.x:G}, {v3.y:G}, {v3.z:G})");
                    break;
                case SerializedPropertyType.Vector4:
                    var v4 = prop.vector4Value;
                    sb.AppendLine($"{indent}{displayName}: ({v4.x:G}, {v4.y:G}, {v4.z:G}, {v4.w:G})");
                    break;
                case SerializedPropertyType.Quaternion:
                    var euler = prop.quaternionValue.eulerAngles;
                    sb.AppendLine($"{indent}{displayName}: ({euler.x:G}, {euler.y:G}, {euler.z:G})");
                    break;
                case SerializedPropertyType.Color:
                    var c = prop.colorValue;
                    sb.AppendLine($"{indent}{displayName}: ({c.r:G}, {c.g:G}, {c.b:G}, {c.a:G})");
                    break;
                case SerializedPropertyType.Bounds:
                    var b = prop.boundsValue;
                    sb.AppendLine($"{indent}{displayName}: Center({b.center.x:G}, {b.center.y:G}, {b.center.z:G}) Size({b.size.x:G}, {b.size.y:G}, {b.size.z:G})");
                    break;
                case SerializedPropertyType.ArraySize:
                    break;
                case SerializedPropertyType.AnimationCurve:
                    var curve = prop.animationCurveValue;
                    sb.AppendLine($"{indent}{displayName}: AnimationCurve ({curve.keys.Length} keys)");
                    break;
                case SerializedPropertyType.LayerMask:
                    sb.AppendLine($"{indent}{displayName}: {prop.intValue}");
                    break;
                default:
                    if (prop.isArray)
                    {
                        SerializeArray(sb, prop, indent);
                    }
                    else
                    {
                        sb.AppendLine($"{indent}{displayName}: [{prop.propertyType}]");
                    }
                    break;
            }
        }

        internal static void SerializeArray(StringBuilder sb, SerializedProperty prop, string indent)
        {
            if (prop == null || !prop.isArray) return;

            if (prop.arraySize == 0)
            {
                sb.AppendLine($"{indent}{prop.displayName}: (empty)");
                return;
            }

            sb.AppendLine($"{indent}{prop.displayName} ({prop.arraySize}):");
            int maxShow = System.Math.Min(prop.arraySize, MaxArrayDisplayCount);
            for (int i = 0; i < maxShow; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                SerializeArrayElement(sb, element, i, indent + "  ");
            }
            if (prop.arraySize > maxShow)
            {
                sb.AppendLine($"{indent}  ... (+{prop.arraySize - maxShow} more)");
            }
        }

        private static void SerializeArrayElement(StringBuilder sb, SerializedProperty element, int index, string indent)
        {
            switch (element.propertyType)
            {
                case SerializedPropertyType.String:
                    sb.AppendLine($"{indent}[{index}] {element.stringValue}");
                    break;
                case SerializedPropertyType.Integer:
                    sb.AppendLine($"{indent}[{index}] {element.intValue}");
                    break;
                case SerializedPropertyType.Float:
                    sb.AppendLine($"{indent}[{index}] {element.floatValue:G}");
                    break;
                case SerializedPropertyType.Boolean:
                    sb.AppendLine($"{indent}[{index}] {(element.boolValue ? "true" : "false")}");
                    break;
                case SerializedPropertyType.ObjectReference:
                    string name = element.objectReferenceValue != null
                        ? element.objectReferenceValue.name
                        : "(none)";
                    sb.AppendLine($"{indent}[{index}] {name}");
                    break;
                case SerializedPropertyType.Enum:
                    sb.AppendLine($"{indent}[{index}] {GetEnumValue(element)}");
                    break;
                default:
                    if (element.hasVisibleChildren)
                    {
                        sb.AppendLine($"{indent}[{index}]");
                        SerializeChildProperties(sb, element, indent + "  ");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}[{index}] [{element.propertyType}]");
                    }
                    break;
            }
        }

        internal static void SerializeChildProperties(StringBuilder sb, SerializedProperty parent, string indent)
        {
            var child = parent.Copy();
            int depth = child.depth;
            bool entered = child.NextVisible(true);

            while (entered && child.depth > depth)
            {
                SerializeProperty(sb, child, indent);
                entered = child.NextVisible(false);
            }
        }

        public static string GetEnumValue(SerializedProperty prop)
        {
            if (prop == null) return "(null)";
            if (prop.propertyType != SerializedPropertyType.Enum)
                return prop.intValue.ToString();
            if (prop.enumDisplayNames != null &&
                prop.enumValueIndex >= 0 &&
                prop.enumValueIndex < prop.enumDisplayNames.Length)
            {
                return prop.enumDisplayNames[prop.enumValueIndex];
            }
            return prop.intValue.ToString();
        }

        private static bool IsSkippedProperty(string propertyName)
        {
            return SkippedPropertyNames.Contains(propertyName);
        }
    }
}
