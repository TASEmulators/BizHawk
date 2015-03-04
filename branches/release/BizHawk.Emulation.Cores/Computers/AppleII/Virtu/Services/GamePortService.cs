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
                UpdateKey(keyboard.Joystick0UpKey, IsJoystick0Up, ref _isJoystick0UpKeyDown, ref _wasJoystick0UpKeyDown);
                UpdateKey(keyboard.Joystick0LeftKey, IsJoystick0Left, ref _isJoystick0LeftKeyDown, ref _wasJoystick0LeftKeyDown);
                UpdateKey(keyboard.Joystick0RightKey, IsJoystick0Right, ref _isJoystick0RightKeyDown, ref _wasJoystick0RightKeyDown);
                UpdateKey(keyboard.Joystick0DownKey, IsJoystick0Down, ref _isJoystick0DownKeyDown, ref _wasJoystick0DownKeyDown);
                UpdateKey(keyboard.Joystick0UpLeftKey, IsJoystick0Up && IsJoystick0Left, ref _isJoystick0UpLeftKeyDown, ref _wasJoystick0UpLeftKeyDown);
                UpdateKey(keyboard.Joystick0UpRightKey, IsJoystick0Up && IsJoystick0Right, ref _isJoystick0UpRightKeyDown, ref _wasJoystick0UpRightKeyDown);
                UpdateKey(keyboard.Joystick0DownLeftKey, IsJoystick0Down && IsJoystick0Left, ref _isJoystick0DownLeftKeyDown, ref _wasJoystick0DownLeftKeyDown);
                UpdateKey(keyboard.Joystick0DownRightKey, IsJoystick0Down && IsJoystick0Right, ref _isJoystick0DownRightKeyDown, ref _wasJoystick0DownRightKeyDown);
                UpdateKey(keyboard.Joystick1UpKey, IsJoystick1Up, ref _isJoystick1UpKeyDown, ref _wasJoystick1UpKeyDown);
                UpdateKey(keyboard.Joystick1LeftKey, IsJoystick1Left, ref _isJoystick1LeftKeyDown, ref _wasJoystick1LeftKeyDown);
                UpdateKey(keyboard.Joystick1RightKey, IsJoystick1Right, ref _isJoystick1RightKeyDown, ref _wasJoystick1RightKeyDown);
                UpdateKey(keyboard.Joystick1DownKey, IsJoystick1Down, ref _isJoystick1DownKeyDown, ref _wasJoystick1DownKeyDown);
                UpdateKey(keyboard.Joystick1UpLeftKey, IsJoystick1Up && IsJoystick1Left, ref _isJoystick1UpLeftKeyDown, ref _wasJoystick1UpLeftKeyDown);
                UpdateKey(keyboard.Joystick1UpRightKey, IsJoystick1Up && IsJoystick1Right, ref _isJoystick1UpRightKeyDown, ref _wasJoystick1UpRightKeyDown);
                UpdateKey(keyboard.Joystick1DownLeftKey, IsJoystick1Down && IsJoystick1Left, ref _isJoystick1DownLeftKeyDown, ref _wasJoystick1DownLeftKeyDown);
                UpdateKey(keyboard.Joystick1DownRightKey, IsJoystick1Down && IsJoystick1Right, ref _isJoystick1DownRightKeyDown, ref _wasJoystick1DownRightKeyDown);
                UpdateKey(keyboard.Button0Key, IsButton0Down, ref _isButton0KeyDown, ref _wasButton0KeyDown);
                UpdateKey(keyboard.Button1Key, IsButton1Down, ref _isButton1KeyDown, ref _wasButton1KeyDown);
                UpdateKey(keyboard.Button2Key, IsButton2Down, ref _isButton2KeyDown, ref _wasButton2KeyDown);

                if (_lastKey > 0) // repeat last key
                {
                    long time = DateTime.UtcNow.Ticks;
                    if (time - _lastTime >= _repeatTime)
                    {
                        _lastTime = time;
                        _repeatTime = RepeatSpeed;
                        keyboard.Latch = _lastKey;
                    }
                }
            }
        }

        private void UpdateKey(int key, bool isActive, ref bool isKeyDown, ref bool wasKeyDown)
        {
            wasKeyDown = isKeyDown;
            isKeyDown = (key > 0) && isActive;

            if (isKeyDown != wasKeyDown)
            {
                if (isKeyDown)
                {
                    _lastKey = key;
                    _lastTime = DateTime.UtcNow.Ticks;
                    _repeatTime = RepeatDelay;
                    Machine.Keyboard.Latch = key;
                }
                else if (key == _lastKey)
                {
                    _lastKey = 0;
                }
            }
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

        private int _lastKey;
        private long _lastTime;
        private long _repeatTime;
    }
}
