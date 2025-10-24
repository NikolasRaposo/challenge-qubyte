using CollisionBear.WorldEditor.Utils;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor.Extensions
{
    public static class EventExtension
    {
        public static bool IsPureMouseStartClick(this Event current)
        {
            return current.type == EventType.MouseDown && current.button == 0 && !current.alt && !current.control && !current.shift;
        }

        public static bool IsPureMouseDrag(this Event current)
        {
            return current.type == EventType.MouseDrag && current.button == 0 && !current.alt && !current.control && !current.shift;
        }

        public static bool IsPureMouseEndClick(this Event current)
        {
            return current.type == EventType.MouseUp && current.button == 0 && !current.alt && !current.control;
        }

        public static bool NoModifiers(this Event current)
        {
            return !current.alt && !current.control && !current.shift;
        }

        public static bool HasModifiers(this Event currentEvent, ShortcutModifiers shortcutModifiers)
        {
            if ((shortcutModifiers.HasFlag(ShortcutModifiers.Shift) && !currentEvent.shift)) {
                return false;
            }

            if ((!shortcutModifiers.HasFlag(ShortcutModifiers.Shift) && currentEvent.shift)) {
                return false;
            }

            if ((shortcutModifiers.HasFlag(ShortcutModifiers.Alt) && !currentEvent.alt)) {
                return false;
            }

            if ((!shortcutModifiers.HasFlag(ShortcutModifiers.Alt) && currentEvent.alt)) {
                return false;
            }

            if ((shortcutModifiers.HasFlag(ShortcutModifiers.Control) && !currentEvent.control)) {
                return false;
            }

            if ((!shortcutModifiers.HasFlag(ShortcutModifiers.Control) && currentEvent.control)) {
                return false;
            }

            return true;
        }

        public static Ray GUIPointToRay(this Event currentEvent)
        {
            return HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        }
    }
}