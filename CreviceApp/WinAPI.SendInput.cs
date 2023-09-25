using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;


namespace Crevice.WinAPI.SendInput
{
    using Crevice.WinAPI.Helper;

    public class InputSender
    {
        static class NativeMethods {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

            [DllImport("user32.dll")]
            public static extern uint MapVirtualKey(uint uCode, uint uMapType);
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct INPUT
        {
            public int type;
            public INPUTDATA data;
        }

        [StructLayout(LayoutKind.Explicit)]
        protected struct INPUTDATA
        {
            [FieldOffset(0)]
            public MOUSEINPUT asMouseInput;
            [FieldOffset(0)]
            public KEYBDINPUT asKeyboardInput;
            [FieldOffset(0)]
            public HARDWAREINPUT asHardwareInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct WHEELDELTA
        {
            public int delta;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct XBUTTON
        {
            public int type;
        }

        [StructLayout(LayoutKind.Explicit)]
        protected struct MOUSEDATA
        {
            [FieldOffset(0)]
            public WHEELDELTA asWheelDelta;
            [FieldOffset(0)]
            public XBUTTON asXButton;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public MOUSEDATA mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        // https://msdn.microsoft.com/ja-jp/library/windows/desktop/ms646273(v=vs.85).aspx
        [Flags]
        private enum XButtonType
        {
            XBUTTON1 = 0x01,
            XBUTTON2 = 0x02
        }

        // https://msdn.microsoft.com/ja-jp/library/windows/desktop/ms646270(v=vs.85).aspx
        [Flags]
        private enum InputType
        {
            INPUT_MOUSE    = 0x0,
            INPUT_KEYBOARD = 0x1,
            INPUT_HARDWARE = 0x2
        }
        
        // https://msdn.microsoft.com/ja-jp/library/windows/desktop/ms646260(v=vs.85).aspx
        [Flags]
        private enum MouseEventType 
        {
            MOUSEEVENTF_MOVE       = 0x0001,
            MOUSEEVENTF_LEFTDOWN   = 0x0002,
            MOUSEEVENTF_LEFTUP     = 0x0004,
            MOUSEEVENTF_RIGHTDOWN  = 0x0008,
            MOUSEEVENTF_RIGHTUP    = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP   = 0x0040,
            MOUSEEVENTF_XDOWN      = 0x0080,
            MOUSEEVENTF_XUP        = 0x0100,
            MOUSEEVENTF_WHEEL      = 0x0800,
            MOUSEEVENTF_HWHEEL     = 0x1000,
            MOUSEEVENTF_ABSOLUTE   = 0x8000
        }

        // https://msdn.microsoft.com/ja-jp/library/windows/desktop/ms646271(v=vs.85).aspx
        [Flags]
        private enum KeyboardEventType
        {
            KEYEVENTF_EXTENDEDKEY = 0x01,
            KEYEVENTF_KEYUP       = 0x02,
            KEYEVENTF_UNICODE     = 0x04,
            KEYEVENTF_SCANCODE    = 0x08
        }
        
        private const int WHEEL_DELTA = 120;

        private readonly UIntPtr MOUSEEVENTF_CREVICE_APP = new UIntPtr(0xFFFFFF00);
        private readonly UIntPtr KEYBOARDEVENTF_CREVICE_APP = new UIntPtr(0xFFFFFF00);

        protected void Send(INPUT[] input)
        {
            var log = new WinAPILogger("SendInput");
            foreach (var item in input.Select((v, i) => new { v, i}))
            {
                var inputType = (InputType)item.v.type;            
                if (inputType.Equals(InputType.INPUT_MOUSE))
                {
                    var data = item.v.data.asMouseInput;
                    var eventType = (MouseEventType)data.dwFlags;

                    log.Add($"MouseEvent ({item.i + 1}/{input.Length})");
                    log.Add($"dx: {data.dx}");
                    log.Add($"dy: {data.dy}");
                    log.Add($"dwFlags: {eventType} | {ToHexString(data.dwFlags)}");
                    if (eventType.HasFlag(MouseEventType.MOUSEEVENTF_XDOWN | MouseEventType.MOUSEEVENTF_XUP))
                    {
                        log.Add($"mouseData: {(XButtonType)data.mouseData.asXButton.type} | {ToHexString((uint)data.mouseData.asXButton.type)}");
                    } 
                    else if (eventType.HasFlag(MouseEventType.MOUSEEVENTF_WHEEL | MouseEventType.MOUSEEVENTF_HWHEEL))
                    {
                        log.Add($"mouseData: {data.mouseData.asWheelDelta.delta} | {ToHexString((uint)data.mouseData.asWheelDelta.delta)}");
                    }
                    log.Add($"dwExtraInfo: {ToHexString(data.dwExtraInfo.ToUInt64())}");
                }
                else if (inputType.HasFlag(InputType.INPUT_KEYBOARD))
                {
                    var data = item.v.data.asKeyboardInput;
                    var eventType = (KeyboardEventType)data.dwFlags;
                    log.Add($"KeyboardEvent ({item.i + 1}/{input.Length})");
                    log.Add($"wVk: {data.wVk}");
                    log.Add($"wScan: {data.wScan}");
                    log.Add($"dwFlags: {eventType} | {ToHexString(data.dwFlags)}");
                    log.Add($"dwExtraInfo: {ToHexString(data.dwExtraInfo.ToUInt64())}");
                }
            }
            if (NativeMethods.SendInput((uint)input.Length, input, Marshal.SizeOf(input[0])) > 0)
            {
                log.Success();
            }
            else
            {
                log.FailWithErrorCode();
            }
        }

        private string ToHexString(ulong data)
        {
            return BitConverter.ToString(BitConverter.GetBytes(data));
        }

        protected INPUT ToInput(KEYBDINPUT keyboardInput)
        {
            var input = new INPUT();
            input.type = (int)InputType.INPUT_KEYBOARD;
            input.data.asKeyboardInput = keyboardInput;
            return input;
        }

        private KEYBDINPUT GetCreviceKeyboardInput()
        {
            var keyboardInput = new KEYBDINPUT();
            // Set the CreviceApp signature to the keyboard event
            keyboardInput.dwExtraInfo = KEYBOARDEVENTF_CREVICE_APP;
            keyboardInput.time = 0;
            return keyboardInput;
        }

        private KEYBDINPUT KeyEvent(ushort keyCode)
        {
            var keyboardInput = GetCreviceKeyboardInput();
            keyboardInput.wVk = keyCode;
            return keyboardInput;
        }

        private KEYBDINPUT ExtendedKeyEvent(ushort keyCode)
        {
            var keyboardInput = KeyEvent(keyCode);
            keyboardInput.dwFlags = (int)KeyboardEventType.KEYEVENTF_EXTENDEDKEY;
            return keyboardInput;
        }

        private KEYBDINPUT KeyWithScanCodeEvent(ushort keyCode)
        {
            var keyboardInput = KeyEvent(keyCode);
            keyboardInput.wScan = (ushort)NativeMethods.MapVirtualKey(keyCode, 0);
            keyboardInput.dwFlags = keyboardInput.dwFlags | (int)KeyboardEventType.KEYEVENTF_SCANCODE;
            return keyboardInput;
        }

        private KEYBDINPUT ExtendedKeyWithScanCodeEvent(ushort keyCode)
        {
            var keyboardInput = ExtendedKeyEvent(keyCode);
            keyboardInput.wScan = (ushort)NativeMethods.MapVirtualKey(keyCode, 0);
            keyboardInput.dwFlags = keyboardInput.dwFlags | (int)KeyboardEventType.KEYEVENTF_SCANCODE;
            return keyboardInput;
        }

        private KEYBDINPUT UnicodeKeyEvent(char c)
        {
            var keyboardInput = new KEYBDINPUT();
            keyboardInput.wScan = c;
            keyboardInput.dwFlags = (int)KeyboardEventType.KEYEVENTF_UNICODE;
            return keyboardInput;
        }

        protected KEYBDINPUT KeyUpEvent(ushort keyCode)
        {
            var keyboardInput = KeyEvent(keyCode);
            keyboardInput.dwFlags = (int)KeyboardEventType.KEYEVENTF_KEYUP;
            return keyboardInput;
        }

        protected KEYBDINPUT ExtendedKeyUpEvent(ushort keyCode)
        {
            var keyboardInput = ExtendedKeyEvent(keyCode);
            keyboardInput.dwFlags = keyboardInput.dwFlags | (int)KeyboardEventType.KEYEVENTF_KEYUP;
            return keyboardInput;
        }

        protected KEYBDINPUT KeyUpWithScanCodeEvent(ushort keyCode)
        {
            var keyboardInput = KeyWithScanCodeEvent(keyCode);
            keyboardInput.dwFlags = keyboardInput.dwFlags | (int)KeyboardEventType.KEYEVENTF_KEYUP;
            return keyboardInput;

        }

        protected KEYBDINPUT ExtendedKeyUpWithScanCodeEvent(ushort keyCode)
        {
            var keyboardInput = ExtendedKeyWithScanCodeEvent(keyCode);
            keyboardInput.dwFlags = keyboardInput.dwFlags | (int)KeyboardEventType.KEYEVENTF_KEYUP;
            return keyboardInput;

        }

        protected KEYBDINPUT UnicodeKeyUpEvent(char c)
        {
            var keyboardInput = UnicodeKeyEvent(c);
            keyboardInput.dwFlags = keyboardInput.dwFlags | (int)KeyboardEventType.KEYEVENTF_KEYUP;
            return keyboardInput;
        }

        protected KEYBDINPUT KeyDownEvent(ushort keyCode)
            => KeyEvent(keyCode);

        protected KEYBDINPUT ExtendedKeyDownEvent(ushort keyCode)
            => ExtendedKeyEvent(keyCode);

        protected KEYBDINPUT KeyDownWithScanCodeEvent(ushort keyCode)
            => KeyWithScanCodeEvent(keyCode);

        protected KEYBDINPUT ExtendedKeyDownWithScanCodeEvent(ushort keyCode)
            => ExtendedKeyWithScanCodeEvent(keyCode);

        protected KEYBDINPUT UnicodeKeyDownEvent(char c)
            => UnicodeKeyEvent(c);
    }

    public class SingleInputSender : InputSender
    {
        protected void Send(params KEYBDINPUT[] keyboardInput)
        {
            var input = new INPUT[keyboardInput.Length];
            for (var i = 0; i < keyboardInput.Length; i++)
            {
                input[i] = ToInput(keyboardInput[i]);
            }
            Send(input);
        }

        public void KeyDown(int keyCode)
            => Send(KeyDownEvent((ushort)keyCode));

        public void KeyUp(int keyCode)
            => Send(KeyUpEvent((ushort)keyCode));

        public void ExtendedKeyDown(int keyCode)
            => Send(ExtendedKeyDownEvent((ushort)keyCode));

        public void ExtendedKeyUp(int keyCode)
            => Send(ExtendedKeyUpEvent((ushort)keyCode));

        public void KeyDownWithScanCode(int keyCode)
            => Send(KeyDownWithScanCodeEvent((ushort)keyCode));

        public void KeyUpWithScanCode(int keyCode)
            => Send(KeyUpWithScanCodeEvent((ushort)keyCode));

        public void ExtendedKeyDownWithScanCode(int keyCode)
            => Send(ExtendedKeyDownWithScanCodeEvent((ushort)keyCode));
        
        public void ExtendedKeyUpWithScanCode(int keyCode)
            => Send(ExtendedKeyUpWithScanCodeEvent((ushort)keyCode));

        public void UnicodeKeyDown(char c)
            => Send(UnicodeKeyDownEvent(c));
        
        public void UnicodeKeyUp(char c)
            => Send(UnicodeKeyUpEvent(c));
        
        public void UnicodeKeyStroke(string str)
        {
            var list = str
                .Select(c => new List<KEYBDINPUT>() { UnicodeKeyDownEvent(c), UnicodeKeyUpEvent(c) })
                .SelectMany(x => x);
            Send(list.ToArray());
        }

        public InputSequenceBuilder Multiple()
            => new InputSequenceBuilder();
    }

    public class InputSequenceBuilder : InputSender
    {
        private readonly IEnumerable<INPUT> buffer;

        public InputSequenceBuilder() : this(new List<INPUT>())
        { }

        private InputSequenceBuilder(IReadOnlyList<INPUT> xs)
        {
            buffer = xs;
        }
        
        private InputSequenceBuilder NewInstance(IReadOnlyList<INPUT> ys)
        {
            var xs = buffer.ToList();
            xs.AddRange(ys);
            return new InputSequenceBuilder(xs);
        }

        private InputSequenceBuilder NewInstance(params KEYBDINPUT[] keyboardEvent)
            => NewInstance(keyboardEvent.Select(x => ToInput(x)).ToList());

        public InputSequenceBuilder KeyDown(int keyCode)
            => NewInstance(KeyDownEvent((ushort)keyCode));

        public InputSequenceBuilder KeyUp(int keyCode)
            => NewInstance(KeyUpEvent((ushort)keyCode));

        public InputSequenceBuilder ExtendedKeyDown(int keyCode)
            => NewInstance(ExtendedKeyDownEvent((ushort)keyCode));

        public InputSequenceBuilder ExtendedKeyUp(int keyCode)
            => NewInstance(ExtendedKeyUpEvent((ushort)keyCode));

        public InputSequenceBuilder KeyDownWithScanCode(int keyCode)
            => NewInstance(KeyDownWithScanCodeEvent((ushort)keyCode));

        public InputSequenceBuilder KeyUpWithScanCode(int keyCode)
            => NewInstance(KeyUpWithScanCodeEvent((ushort)keyCode));

        public InputSequenceBuilder ExtendedKeyDownWithScanCode(int keyCode)
            => NewInstance(ExtendedKeyDownWithScanCodeEvent((ushort)keyCode));

        public InputSequenceBuilder ExtendedKeyUpWithScanCode(int keyCode)
            => NewInstance(ExtendedKeyUpWithScanCodeEvent((ushort)keyCode));

        public InputSequenceBuilder UnicodeKeyDown(char c)
            => NewInstance(UnicodeKeyDownEvent(c));

        public InputSequenceBuilder UnicodeKeyUp(char c)
            => NewInstance(UnicodeKeyUpEvent(c));

        public InputSequenceBuilder UnicodeKeyStroke(string str)
        {
            return NewInstance(str
                .Select(c => new List<KEYBDINPUT>() { UnicodeKeyDownEvent(c), UnicodeKeyUpEvent(c) })
                .SelectMany(x => x)
                .ToArray());
        }

        public void Send()
            => Send(buffer.ToArray());
    }
}
