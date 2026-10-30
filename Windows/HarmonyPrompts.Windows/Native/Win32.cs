using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HarmonyPrompts.Windows.Native;

internal static class Win32
{
    internal const uint EventSystemForeground = 3;
    internal const uint EventObjectDestroy = 0x8001;
    internal const uint KeyEventKeyUp = 2;
    internal const uint InputKeyboard = 1;
    internal delegate void WinEventProc(nint hook, uint eventType, nint hwnd, int objectId, int childId, uint threadId, uint time);

    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool OpenClipboard(nint owner);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool CloseClipboard();
    [DllImport("user32.dll")] internal static extern nint GetClipboardData(uint format);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern uint RegisterClipboardFormat(string name);
    [DllImport("kernel32.dll")] internal static extern nuint GlobalSize(nint memory);
    [DllImport("kernel32.dll")] internal static extern nint GlobalLock(nint memory);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GlobalUnlock(nint memory);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(nint hwnd, StringBuilder name, int maxCount);
    [DllImport("user32.dll", SetLastError = true)] internal static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint module, WinEventProc callback, uint processId, uint threadId, uint flags);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UnhookWinEvent(nint hook);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, [In] Input[] inputs, int size);
    [DllImport("user32.dll", SetLastError = true)] internal static extern nint OpenInputDesktop(uint flags, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool CloseDesktop(nint desktop);
    [DllImport("user32.dll")] internal static extern nint GetThreadDesktop(uint threadId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetUserObjectInformation(nint handle, int index, StringBuilder info, uint length, out uint needed);
    [DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll")] internal static extern nint GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern nint OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint processId);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool CloseHandle(nint handle);
    [DllImport("advapi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool OpenProcessToken(nint process, uint access, out nint token);
    [DllImport("advapi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetTokenInformation(nint token, int infoClass, nint information, uint length, out uint needed);
    [DllImport("advapi32.dll")] internal static extern nint GetSidSubAuthorityCount(nint sid);
    [DllImport("advapi32.dll")] internal static extern nint GetSidSubAuthority(nint sid, uint index);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseInput { public int X, Y; public uint MouseData, Flags, Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardInput { public ushort VirtualKey, ScanCode; public uint Flags, Time; public nuint ExtraInfo; }

    internal static Input Key(ushort key, bool up = false) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = key, Flags = up ? KeyEventKeyUp : 0 } }
    };

    internal static uint? IntegrityLevel(nint process)
    {
        if (!OpenProcessToken(process, 8, out var token)) return null;
        nint buffer = 0;
        try
        {
            GetTokenInformation(token, 25, 0, 0, out var size);
            if (size == 0) return null;
            buffer = Marshal.AllocHGlobal(checked((int)size));
            if (!GetTokenInformation(token, 25, buffer, size, out _)) return null;
            var sid = Marshal.ReadIntPtr(buffer);
            var count = Marshal.ReadByte(GetSidSubAuthorityCount(sid));
            return count == 0 ? null : unchecked((uint)Marshal.ReadInt32(GetSidSubAuthority(sid, (uint)count - 1)));
        }
        finally
        {
            if (buffer != 0) Marshal.FreeHGlobal(buffer);
            CloseHandle(token);
        }
    }

    internal static bool IsDefaultInputDesktop()
    {
        var input = OpenInputDesktop(0, false, 1);
        if (input == 0) return false;
        try
        {
            var inputName = new StringBuilder(256);
            var ourName = new StringBuilder(256);
            return GetUserObjectInformation(input, 2, inputName, 512, out _) &&
                GetUserObjectInformation(GetThreadDesktop(GetCurrentThreadId()), 2, ourName, 512, out _) &&
                inputName.ToString() == "Default" && inputName.ToString() == ourName.ToString();
        }
        finally { CloseDesktop(input); }
    }
}
