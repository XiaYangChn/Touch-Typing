using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.ComponentModel;

namespace Touch_Typing
{
    public class KeyboardHook
    {
        #region Windows structure definitions

        [StructLayout(LayoutKind.Sequential)]
        private class KeyboardHookStruct
        {
            public int vkCode;

            public int scanCode;

            public int flags;

            public int time;

            public int dwExtraInfo;
        }
        #endregion

        #region Windows function imports

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int SetWindowsHookEx(
            int idHook,
            HookProc lpfn,
            IntPtr hMod,
            int dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(
            int idHook,
            int nCode,
            int wParam,
            IntPtr lParam);

        private delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        [DllImport("user32")]
        private static extern int ToAscii(
            int uVirtKey,
            int uScanCode,
            byte[] lpbKeyState,
            byte[] lpwTransKey,
            int fuState);

        [DllImport("user32")]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);

        //使用WINDOWS API函数代替获取当前实例的函数,防止钩子失效
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);

        #endregion

        #region Windows constants

        private const int WH_KEYBOARD_LL = 13;

        private const int WH_KEYBOARD = 2;


        private const int WM_KEYDOWN = 0x100;

        private const int WM_KEYUP = 0x101;

        private const int WM_SYSKEYDOWN = 0x104;

        private const int WM_SYSKEYUP = 0x105;

        private const byte VK_SHIFT = 0x10;
        private const byte VK_CAPITAL = 0x14;
        private const byte VK_NUMLOCK = 0x90;

        #endregion

        public KeyboardHook()
        {
            Start();
        }

        public KeyboardHook(bool InstallKeyboardHook)
        {
            Start(InstallKeyboardHook);
        }

        ~KeyboardHook()
        {
            Stop(true, false);
        }

        public event KeyEventHandler KeyDown;

        public event KeyPressEventHandler KeyPress;

        public event KeyEventHandler KeyUp;


        private int hKeyboardHook = 0;


        private static HookProc KeyboardHookProcedure;


        public void Start()
        {
            this.Start(true);
        }


        public void Start(bool InstallKeyboardHook)
        {
            if (hKeyboardHook == 0 && InstallKeyboardHook)
            {

                KeyboardHookProcedure = new HookProc(KeyboardHookProc);

                hKeyboardHook = SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    KeyboardHookProcedure,
                    GetModuleHandle(
                        System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName),
                    0);

                if (hKeyboardHook == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    Stop(true, false);

                    throw new Win32Exception(errorCode);
                }
            }
        }

        public void Stop()
        {
            this.Stop(true, true);
        }

        public void Stop(bool UninstallKeyboardHook, bool ThrowExceptions)
        {
            if (hKeyboardHook != 0 && UninstallKeyboardHook)
            {
                int retKeyboard = UnhookWindowsHookEx(hKeyboardHook);

                hKeyboardHook = 0;

                if (retKeyboard == 0 && ThrowExceptions)
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    throw new Win32Exception(errorCode);
                }
            }
        }


        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            bool handled = false;

            if ((nCode >= 0) && (KeyDown != null || KeyUp != null || KeyPress != null))
            {

                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

                if (KeyDown != null && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
                {
                    Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyDown(this, e);
                    handled = handled || e.Handled;
                }

                // raise KeyPress
                if (KeyPress != null && wParam == WM_KEYDOWN)
                {
                    bool isDownShift = ((GetKeyState(VK_SHIFT) & 0x80) == 0x80 ? true : false);
                    bool isDownCapslock = (GetKeyState(VK_CAPITAL) != 0 ? true : false);

                    byte[] keyState = new byte[256];
                    GetKeyboardState(keyState);
                    byte[] inBuffer = new byte[2];
                    if (ToAscii(MyKeyboardHookStruct.vkCode,
                              MyKeyboardHookStruct.scanCode,
                              keyState,
                              inBuffer,
                              MyKeyboardHookStruct.flags) == 1)
                    {
                        char key = (char)inBuffer[0];
                        if ((isDownCapslock ^ isDownShift) && Char.IsLetter(key)) key = Char.ToUpper(key);
                        KeyPressEventArgs e = new KeyPressEventArgs(key);
                        KeyPress(this, e);
                        handled = handled || e.Handled;
                    }
                }

                if (KeyUp != null && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
                {
                    Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyUp(this, e);
                    handled = handled || e.Handled;
                }

            }

            if (handled)
                return 1;
            else
                return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }
    }
}
