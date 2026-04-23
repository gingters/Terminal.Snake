namespace TerminalSnake.Input;

public abstract record InputEvent;

public sealed record KeyEvent(ConsoleKey Key, bool Shift = false) : InputEvent;

public sealed record MouseClickEvent(int Column, int Row, MouseButton Button) : InputEvent;

public enum MouseButton
{
    Left,
    Middle,
    Right,
}
