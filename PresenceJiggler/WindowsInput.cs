using System.Runtime.InteropServices;

namespace PresenceJiggler;

internal static class WindowsInput
{
    private const uint InputMouse = 0;
    private const uint MouseEventMove = 0x0001;
    private const int VkLButton = 0x01;
    private const int VkRButton = 0x02;
    private const int VkMButton = 0x04;
    private const int VkXButton1 = 0x05;
    private const int VkXButton2 = 0x06;

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint type;
        public MouseInputUnion data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct MouseInputUnion
    {
        [FieldOffset(0)] public MouseInput mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, [In] Input[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    public static bool TryGetIdleDuration(out TimeSpan idle)
    {
        var info = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
        {
            idle = TimeSpan.Zero;
            return false;
        }

        // Restar como uint maneja el rollover del contador de milisegundos.
        uint elapsedMs = unchecked((uint)Environment.TickCount - info.dwTime);
        idle = TimeSpan.FromMilliseconds(elapsedMs);
        return true;
    }

    public static bool IsAnyMouseButtonDown() =>
        IsDown(VkLButton) || IsDown(VkRButton) || IsDown(VkMButton) ||
        IsDown(VkXButton1) || IsDown(VkXButton2);

    private static bool IsDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    public static bool NudgeCursor()
    {
        if (IsAnyMouseButtonDown())
            return false;

        var mousePosition = Cursor.Position;
        var desktop = SystemInformation.VirtualScreen;
        int dx = mousePosition.X < desktop.Right - 1 ? 1 : -1;

        // Se envían ambos movimientos juntos para minimizar el efecto visual.
        var inputs = new[] { Move(dx), Move(-dx) };
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

    private static Input Move(int dx) => new()
    {
        type = InputMouse,
        data = new MouseInputUnion
        {
            mouse = new MouseInput { dx = dx, dy = 0, dwFlags = MouseEventMove }
        }
    };
}
