using CollisionBear.WorldEditor.Extensions;
using UnityEngine;

namespace CollisionBear.WorldEditor.Utils
{
    [System.Serializable]
    public class KeyBinding
    {
        public const int MaxKeyCodeValue = 1024;

        public BindingKeyCode KeyCode;
        public ShortcutModifiers Modifiers;

        public string SettingName;

        public string ToolTip;


        public KeyBinding() { }
        public KeyBinding(string settingName, BindingKeyCode keyCode, ShortcutModifiers modifiers)
        {
            SettingName = settingName;
            KeyCode = keyCode;
            Modifiers = modifiers;

        }

        public KeyBinding(string settingName, string toolTip, BindingKeyCode keyCode, ShortcutModifiers modifiers)
            : this(settingName, keyCode, modifiers)
        {
            ToolTip = toolTip;
        }

        public bool EventMatch(Event currentEvent)
        {
            if (KeyCode == BindingKeyCode.None) {
                return false;
            }

            if (!currentEvent.HasModifiers(Modifiers)) {
                return false;
            }

            if ((int)KeyCode < MaxKeyCodeValue) {
                if (currentEvent.keyCode == (KeyCode)KeyCode) {
                    return true;
                }
            } else if (KeyCode == BindingKeyCode.ScrollWheel) {
                return currentEvent.isScrollWheel && currentEvent.delta.sqrMagnitude > 0;
            }

            return false;
        }

        public int GetDelta(Event currentEvent)
        {
            if (KeyCode == BindingKeyCode.ScrollWheel) {
                return GetMouseWheelDeltaSteps(currentEvent.delta);
            } else {
                return 1;
            }
        }

        private int GetMouseWheelDeltaSteps(Vector2 delta) => Mathf.Clamp((int)HighestValue(delta), -1, 1);

        private float HighestValue(Vector2 delta)
            => Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? delta.x : delta.y;

        public string GetToolTip() => $"{SettingName}\n{ToolTip}\n{GetHotKeyString()}";

        public string GetHotKeyString() => $"[{GetModifiers()}{KeyCode}]";
        private string GetModifiers() => Modifiers == ShortcutModifiers.None ? "" : $"{Modifiers} + ";
    }

    [System.Serializable]
    public class KeyInstruction
    {
        public string SettingName;

        public string Description;

        public KeyInstruction() { }

        public KeyInstruction(string name, string description)
        {
            SettingName = name;
            Description = description;
        }
    }

    public enum BindingKeyCode : int
    {
        None = KeyCode.None,
        A = KeyCode.A,
        B = KeyCode.B,
        C = KeyCode.C,
        D = KeyCode.D,
        E = KeyCode.E,
        F = KeyCode.F,
        G = KeyCode.G,
        H = KeyCode.H,
        I = KeyCode.I,
        J = KeyCode.J,
        K = KeyCode.K,
        L = KeyCode.L,
        M = KeyCode.M,
        N = KeyCode.N,
        O = KeyCode.O,
        P = KeyCode.P,
        Q = KeyCode.Q,
        R = KeyCode.R,
        S = KeyCode.S,
        T = KeyCode.T,
        U = KeyCode.U,
        V = KeyCode.V,
        W = KeyCode.W,
        X = KeyCode.X,
        Y = KeyCode.Y,
        Z = KeyCode.Z,
        Escape = KeyCode.Escape,
        Alpha0 = KeyCode.Alpha0,
        Alpha1 = KeyCode.Alpha1,
        Alpha2 = KeyCode.Alpha2,
        Alpha3 = KeyCode.Alpha3,
        Alpha4 = KeyCode.Alpha4,
        Alpha5 = KeyCode.Alpha5,
        Alpha6 = KeyCode.Alpha6,
        Alpha7 = KeyCode.Alpha7,
        Alpha8 = KeyCode.Alpha8,
        Alpha9 = KeyCode.Alpha9,
        Space = KeyCode.Space,
        Delete = KeyCode.Delete,
        Backspace = KeyCode.Backspace,
        Tab = KeyCode.Tab,
        Return = KeyCode.Return,
        Keypad0 = KeyCode.Keypad0,
        Keypad1 = KeyCode.Keypad1,
        Keypad2 = KeyCode.Keypad2,
        Keypad3 = KeyCode.Keypad3,
        Keypad4 = KeyCode.Keypad4,
        Keypad5 = KeyCode.Keypad5,
        Keypad6 = KeyCode.Keypad6,
        Keypad7 = KeyCode.Keypad7,
        Keypad8 = KeyCode.Keypad8,
        Keypad9 = KeyCode.Keypad9,
        KeypadPeriod = KeyCode.KeypadPeriod,
        KeypadDivide = KeyCode.KeypadDivide,
        KeypadMultiply = KeyCode.KeypadMultiply,
        KeypadMinus = KeyCode.KeypadMinus,
        KeypadPlus = KeyCode.KeypadPlus,
        KeypadEnter = KeyCode.KeypadEnter,
        KeypadEquals = KeyCode.KeypadEquals,

        ScrollWheel = KeyBinding.MaxKeyCodeValue,
    }

    [System.Flags]
    public enum ShortcutModifiers
    {
        None = 0,
        Alt = 1,
        Action = 2,
        Shift = 4,
        Control = 8
    }
}