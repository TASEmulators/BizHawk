using System;

namespace Jellyfish.Virtu.Services
{
    public class GamePortService : MachineService
    {
        public GamePortService(Machine machine) : 
            base(machine)
        {
            Paddle0 = Paddle1 = Paddle2 = Paddle3 = 255; // not connected
        }

        public virtual void Update() // main thread
        {
            var keyboard = Machine.Keyboard;

            if (keyboard.UseGamePort)
            {
                //UpdateKey(keyboard.Joystick0UpKey, IsJoystick0Up, ref _isJoystick0UpKeyDown, ref _wasJoystick0UpKeyDown);
                //UpdateKey(keyboard.Joystick0LeftKey, IsJoystick0Left, ref _isJoystick0LeftKeyDown, ref _wasJoystick0LeftKeyDown);
                //UpdateKey(keyboard.Joystick0RightKey, IsJoystick0Right, ref _isJoystick0RightKeyDown, ref _wasJoystick0RightKeyDown);
                //UpdateKey(keyboard.Joystick0DownKey, IsJoystick0Down, ref _isJoystick0DownKeyDown, ref _wasJoystick0DownKeyDown);
                //UpdateKey(keyboard.Joystick0UpLeftKey, IsJoystick0Up && IsJoystick0Left, ref _isJoystick0UpLeftKeyDown, ref _wasJoystick0UpLeftKeyDown);
                //UpdateKey(keyboard.Joystick0UpRightKey, IsJoystick0Up && IsJoystick0Right, ref _isJoystick0UpRightKeyDown, ref _wasJoystick0UpRightKeyDown);
                //UpdateKey(keyboard.Joystick0DownLeftKey, IsJoystick0Down && IsJoystick0Left, ref _isJoystick0DownLeftKeyDown, ref _wasJoystick0DownLeftKeyDown);
                //UpdateKey(keyboard.Joystick0DownRightKey, IsJoystick0Down && IsJoystick0Right, ref _isJoystick0DownRightKeyDown, ref _wasJoystick0DownRightKeyDown);
                //UpdateKey(keyboard.Joystick1UpKey, IsJoystick1Up, ref _isJoystick1UpKeyDown, ref _wasJoystick1UpKeyDown);
                //UpdateKey(keyboard.Joystick1LeftKey, IsJoystick1Left, ref _isJoystick1LeftKeyDown, ref _wasJoystick1LeftKeyDown);
                //UpdateKey(keyboard.Joystick1RightKey, IsJoystick1Right, ref _isJoystick1RightKeyDown, ref _wasJoystick1RightKeyDown);
                //UpdateKey(keyboard.Joystick1DownKey, IsJoystick1Down, ref _isJoystick1DownKeyDown, ref _wasJoystick1DownKeyDown);
                //UpdateKey(keyboard.Joystick1UpLeftKey, IsJoystick1Up && IsJoystick1Left, ref _isJoystick1UpLeftKeyDown, ref _wasJoystick1UpLeftKeyDown);
                //UpdateKey(keyboard.Joystick1UpRightKey, IsJoystick1Up && IsJoystick1Right, ref _isJoystick1UpRightKeyDown, ref _wasJoystick1UpRightKeyDown);
                //UpdateKey(keyboard.Joystick1DownLeftKey, IsJoystick1Down && IsJoystick1Left, ref _isJoystick1DownLeftKeyDown, ref _wasJoystick1DownLeftKeyDown);
                //UpdateKey(keyboard.Joystick1DownRightKey, IsJoystick1Down && IsJoystick1Right, ref _isJoystick1DownRightKeyDown, ref _wasJoystick1DownRightKeyDown);

                //all the keys are going through this one thing atm
                UpdateKey(keyboard.Button0Key, IsButton0Down, ref _isButton0KeyDown, ref _wasButton0KeyDown);
                //UpdateKey(keyboard.Button1Key, IsButton1Down, ref _isButton1KeyDown, ref _wasButton1KeyDown);
                //UpdateKey(keyboard.Button2Key, IsButton2Down, ref _isButton2KeyDown, ref _wasButton2KeyDown);

             
                /*
                if (_lastKey > 0) // repeat last key
                {
                    long time = DateTime.UtcNow.Ticks;
                    if (time - _lastTime >= _repeatTime)
                    {
                        _lastTime = time;
                        _repeatTime = RepeatSpeed;
                        keyboard.Latch = GetAsciiKey((Buttons)_lastKey, Machine.Keyboard.IsControlKeyDown, Machine.Keyboard.IsShiftKeyDown, false);
                    }
                }
                 * */
            }
        }

        static int t = 0;

        private void UpdateKey(ulong key, bool isActive, ref bool isKeyDown, ref bool wasKeyDown)
        {
            wasKeyDown = isKeyDown;
            isKeyDown = (key > 0);// && isActive;

            if (isKeyDown != wasKeyDown)
            {
                if (isKeyDown)
                {
                    _lastKey = key;
                    _lastTime = DateTime.UtcNow.Ticks;
                    _repeatTime = RepeatDelay;
                        Machine.Keyboard.Latch = GetAsciiKey((Buttons)key, Machine.Keyboard.IsControlKeyDown, Machine.Keyboard.IsShiftKeyDown, false);
                }
                else if (key == _lastKey)
                {
                    _lastKey = 0;
                }
            }


        }

        private static int GetAsciiKey(Buttons bizKey, bool bizCtrl, bool bizShift, bool bizCaps)
        {
            
            bool control = bizCtrl;
            bool shift = bizShift;
            bool capsLock = bizCaps;

            switch (bizKey)
            {
                case 0:
                    return 0x00;
                case Buttons.Left:
                    return 0x08;

                case Buttons.Tab:
                    return 0x09;

                case Buttons.Down:
                    return 0x0A;

                case Buttons.Up:
                    return 0x0B;

                case Buttons.Enter:
                    return 0x0D;

                case Buttons.Right:
                    return 0x15;

                case Buttons.Escape:
                    return 0x1B;

                case Buttons.Back:
                    return control ? -1 : 0x7F;

                case Buttons.Space:
                    return ' ';

                case Buttons.Key1:
                    return shift ? '!' : '1';

                case Buttons.Key2:
                    return control ? 0x00 : shift ? '@' : '2';

                case Buttons.Key3:
                    return shift ? '#' : '3';

                case Buttons.Key4:
                    return shift ? '$' : '4';

                case Buttons.Key5:
                    return shift ? '%' : '5';

                case Buttons.Key6:
                    return control ? 0x1E : shift ? '^' : '6';

                case Buttons.Key7:
                    return shift ? '&' : '7';

                case Buttons.Key8:
                    return shift ? '*' : '8';

                case Buttons.Key9:
                    return shift ? '(' : '9';

                case Buttons.Key0:
                    return shift ? ')' : '0';

                case Buttons.KeyA:
                    return control ? 0x01 : capsLock ? 'A' : 'a';

                case Buttons.KeyB:
                    return control ? 0x02 : capsLock ? 'B' : 'b';

                case Buttons.KeyC:
                    return control ? 0x03 : capsLock ? 'C' : 'c';

                case Buttons.KeyD:
                    return control ? 0x04 : capsLock ? 'D' : 'd';

                case Buttons.KeyE:
                    return control ? 0x05 : capsLock ? 'E' : 'e';

                case Buttons.KeyF:
                    return control ? 0x06 : capsLock ? 'F' : 'f';

                case Buttons.KeyG:
                    return control ? 0x07 : capsLock ? 'G' : 'g';

                case Buttons.KeyH:
                    return control ? 0x08 : capsLock ? 'H' : 'h';

                case Buttons.KeyI:
                    return control ? 0x09 : capsLock ? 'I' : 'i';

                case Buttons.KeyJ:
                    return control ? 0x0A : capsLock ? 'J' : 'j';

                case Buttons.KeyK:
                    return control ? 0x0B : capsLock ? 'K' : 'k';

                case Buttons.KeyL:
                    return control ? 0x0C : capsLock ? 'L' : 'l';

                case Buttons.KeyM:
                    return control ? 0x0D : capsLock ? 'M' : 'm';

                case Buttons.KeyN:
                    return control ? 0x0E : capsLock ? 'N' : 'n';

                case Buttons.KeyO:
                    return control ? 0x0F : capsLock ? 'O' : 'o';

                case Buttons.KeyP:
                    return control ? 0x10 : capsLock ? 'P' : 'p';

                case Buttons.KeyQ:
                    return control ? 0x11 : capsLock ? 'Q' : 'q';

                case Buttons.KeyR:
                    return control ? 0x12 : capsLock ? 'R' : 'r';

                case Buttons.KeyS:
                    return control ? 0x13 : capsLock ? 'S' : 's';

                case Buttons.KeyT:
                    return control ? 0x14 : capsLock ? 'T' : 't';

                case Buttons.KeyU:
                    return control ? 0x15 : capsLock ? 'U' : 'u';

                case Buttons.KeyV:
                    return control ? 0x16 : capsLock ? 'V' : 'v';

                case Buttons.KeyW:
                    return control ? 0x17 : capsLock ? 'W' : 'w';

                case Buttons.KeyX:
                    return control ? 0x18 : capsLock ? 'X' : 'x';

                case Buttons.KeyY:
                    return control ? 0x19 : capsLock ? 'Y' : 'y';

                case Buttons.KeyZ:
                    return control ? 0x1A : capsLock ? 'Z' : 'z';
                    //TODO: Get around to supporting those keys too
                    /*
                case Key.Oem1:
                    return shift ? ':' : ';';

                case Key.Oem2:
                    return shift ? '?' : '/';

                case Key.Oem3:
                    return shift ? '~' : '`';

                case Key.Oem4:
                    return shift ? '{' : '[';

                case Key.Oem5:
                    return control ? 0x1C : shift ? '|' : '\\';

                case Key.Oem6:
                    return control ? 0x1D : shift ? '}' : ']';

                case Key.Oem7:
                    return shift ? '"' : '\'';

                case Key.OemMinus:
                    return control ? 0x1F : shift ? '_' : '-';

                case Key.OemPlus:
                    return shift ? '+' : '=';

                case Key.OemComma:
                    return shift ? '<' : ',';

                case Key.OemPeriod:
                    return shift ? '>' : '.';
                     * */
            }

            return 0;
        }

        public int Paddle0 { get; protected set; }
        public int Paddle1 { get; protected set; }
        public int Paddle2 { get; protected set; }
        public int Paddle3 { get; protected set; }

        public bool IsJoystick0Up { get; protected set; }
        public bool IsJoystick0Left { get; protected set; }
        public bool IsJoystick0Right { get; protected set; }
        public bool IsJoystick0Down { get; protected set; }

        public bool IsJoystick1Up { get; protected set; }
        public bool IsJoystick1Left { get; protected set; }
        public bool IsJoystick1Right { get; protected set; }
        public bool IsJoystick1Down { get; protected set; }

        public bool IsButton0Down { get; protected set; }
        public bool IsButton1Down { get; protected set; }
        public bool IsButton2Down { get; protected set; }

        private static readonly long RepeatDelay = TimeSpan.FromMilliseconds(500).Ticks;
        private static readonly long RepeatSpeed = TimeSpan.FromMilliseconds(32).Ticks;

        private bool _isJoystick0UpLeftKeyDown;
        private bool _isJoystick0UpKeyDown;
        private bool _isJoystick0UpRightKeyDown;
        private bool _isJoystick0LeftKeyDown;
        private bool _isJoystick0RightKeyDown;
        private bool _isJoystick0DownLeftKeyDown;
        private bool _isJoystick0DownKeyDown;
        private bool _isJoystick0DownRightKeyDown;
        private bool _isJoystick1UpLeftKeyDown;
        private bool _isJoystick1UpKeyDown;
        private bool _isJoystick1UpRightKeyDown;
        private bool _isJoystick1LeftKeyDown;
        private bool _isJoystick1RightKeyDown;
        private bool _isJoystick1DownLeftKeyDown;
        private bool _isJoystick1DownKeyDown;
        private bool _isJoystick1DownRightKeyDown;
        private bool _isButton0KeyDown;
        private bool _isButton1KeyDown;
        private bool _isButton2KeyDown;

        private bool _wasJoystick0UpLeftKeyDown;
        private bool _wasJoystick0UpKeyDown;
        private bool _wasJoystick0UpRightKeyDown;
        private bool _wasJoystick0LeftKeyDown;
        private bool _wasJoystick0RightKeyDown;
        private bool _wasJoystick0DownLeftKeyDown;
        private bool _wasJoystick0DownKeyDown;
        private bool _wasJoystick0DownRightKeyDown;
        private bool _wasJoystick1UpLeftKeyDown;
        private bool _wasJoystick1UpKeyDown;
        private bool _wasJoystick1UpRightKeyDown;
        private bool _wasJoystick1LeftKeyDown;
        private bool _wasJoystick1RightKeyDown;
        private bool _wasJoystick1DownLeftKeyDown;
        private bool _wasJoystick1DownKeyDown;
        private bool _wasJoystick1DownRightKeyDown;
        private bool _wasButton0KeyDown;
        private bool _wasButton1KeyDown;
        private bool _wasButton2KeyDown;

        private ulong _lastKey;
        private long _lastTime;
        private long _repeatTime;
    }
}
