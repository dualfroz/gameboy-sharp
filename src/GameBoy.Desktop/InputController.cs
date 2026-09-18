using Raylib_cs;

namespace GameBoy.Desktop
{
    public interface IKeyboard
    {
        bool IsDown(KeyboardKey key);
        bool IsPressed(KeyboardKey key);
    }

    public sealed class InputController
    {
        private readonly KeyBindings bindings;

        public InputController(KeyBindings bindings)
        {
            this.bindings = bindings;
        }

        public void Apply(IKeyboard keyboard, GameSession session)
        {
            foreach (var (button, key) in bindings.Buttons)
                session.Emulator.Joypad.SetButton(button, keyboard.IsDown(key));

            session.FastForward = keyboard.IsDown(bindings.FastForward);

            if (keyboard.IsPressed(bindings.Pause))
                session.TogglePause();
            if (keyboard.IsPressed(bindings.SaveState))
                session.SaveState();
            if (keyboard.IsPressed(bindings.LoadState))
                session.LoadState();
        }
    }
}
