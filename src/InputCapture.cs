using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal static class InputCapture
    {
        public static bool WindowVisible { get; private set; }

        public static bool PointerOverWindow { get; private set; }

        public static bool FloatingButtonActive { get; private set; }

        public static bool TextInputFocused { get; private set; }

        public static bool ShouldBlockGameInput
        {
            get { return WindowVisible || FloatingButtonActive; }
        }

        public static void SetWindowState(bool visible, bool pointerOverWindow)
        {
            WindowVisible = visible;
            PointerOverWindow = pointerOverWindow;
        }

        public static void SetTextInputFocused(bool focused)
        {
            TextInputFocused = focused;
        }

        public static void SetFloatingButtonState(bool active)
        {
            FloatingButtonActive = active;
        }
    }
}
