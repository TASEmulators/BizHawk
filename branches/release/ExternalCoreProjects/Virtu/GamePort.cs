using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Jellyfish.Library;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    public sealed class GamePort : MachineComponent
    {
		// TODO: ressurect this
		public bool ReadButton0() { return false; }
		public bool ReadButton1() { return false; }
		public bool ReadButton2() { return false; }

		public bool Paddle0Strobe { get { return false; } }
		public bool Paddle1Strobe { get { return false; } }
		public bool Paddle2Strobe { get { return false; } }
		public bool Paddle3Strobe { get { return false; } }

		public void TriggerTimers() { }

		public GamePort() { }
        public GamePort(Machine machine) :
            base(machine)
        {
			/*
            _resetPaddle0StrobeEvent = ResetPaddle0StrobeEvent; // cache delegates; avoids garbage
            _resetPaddle1StrobeEvent = ResetPaddle1StrobeEvent;
            _resetPaddle2StrobeEvent = ResetPaddle2StrobeEvent;
            _resetPaddle3StrobeEvent = ResetPaddle3StrobeEvent;
			*/
        }

		/*
        public override void Initialize()
        {
            _keyboardService = Machine.Services.GetService<KeyboardService>();
            _gamePortService = Machine.Services.GetService<GamePortService>();

            JoystickDeadZone = 0.4f;

            InvertPaddles = true; // Raster Blaster
            SwapPaddles = true;
            Joystick0TouchX = 0.35f;
            Joystick0TouchY = 0.6f;
            Joystick0TouchWidth = 0.25f;
            Joystick0TouchHeight = 0.4f;
            Joystick0TouchRadius = 0.2f;
            Joystick0TouchKeepLast = true;
            Button0TouchX = 0;
            Button0TouchY = 0;
            Button0TouchWidth = 0.5f;
            Button0TouchHeight = 1;
            Button1TouchX = 0.5f;
            Button1TouchY = 0;
            Button1TouchWidth = 0.5f;
            Button1TouchHeight = 1;
            Button2TouchX = 0.75f;
            Button2TouchY = 0;
            Button2TouchWidth = 0.25f;
            Button2TouchHeight = 0.25f;
            Button2TouchOrder = 1;
        }
		
        public bool ReadButton0()
        {
            return (_gamePortService.IsButton0Down || _keyboardService.IsOpenAppleKeyDown || 
                (UseKeyboard && (Button0Key > 0) && _keyboardService.IsKeyDown(Button0Key)));
        }

        public bool ReadButton1()
        {
            return (_gamePortService.IsButton1Down || _keyboardService.IsCloseAppleKeyDown || 
                (UseKeyboard && (Button1Key > 0) && _keyboardService.IsKeyDown(Button1Key)));
        }

        public bool ReadButton2()
        {
            return (_gamePortService.IsButton2Down || (UseShiftKeyMod && !_keyboardService.IsShiftKeyDown) || // Shift' [TN9]
                (UseKeyboard && (Button2Key > 0) && _keyboardService.IsKeyDown(Button2Key)));
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public void TriggerTimers()
        {
            int paddle0 = _gamePortService.Paddle0;
            int paddle1 = _gamePortService.Paddle1;
            int paddle2 = _gamePortService.Paddle2;
            int paddle3 = _gamePortService.Paddle3;

            if (UseKeyboard) // override
            {
                if (((Joystick0UpLeftKey > 0) && _keyboardService.IsKeyDown(Joystick0UpLeftKey)) || 
                    ((Joystick0LeftKey > 0) && _keyboardService.IsKeyDown(Joystick0LeftKey)) || 
                    ((Joystick0DownLeftKey > 0) && _keyboardService.IsKeyDown(Joystick0DownLeftKey)))
                {
                    paddle0 -= PaddleScale;
                }
                if (((Joystick0UpRightKey > 0) && _keyboardService.IsKeyDown(Joystick0UpRightKey)) || 
                    ((Joystick0RightKey > 0) && _keyboardService.IsKeyDown(Joystick0RightKey)) || 
                    ((Joystick0DownRightKey > 0) && _keyboardService.IsKeyDown(Joystick0DownRightKey)))
                {
                    paddle0 += PaddleScale;
                }
                if (((Joystick0UpLeftKey > 0) && _keyboardService.IsKeyDown(Joystick0UpLeftKey)) || 
                    ((Joystick0UpKey > 0) && _keyboardService.IsKeyDown(Joystick0UpKey)) || 
                    ((Joystick0UpRightKey > 0) && _keyboardService.IsKeyDown(Joystick0UpRightKey)))
                {
                    paddle1 -= PaddleScale;
                }
                if (((Joystick0DownLeftKey > 0) && _keyboardService.IsKeyDown(Joystick0DownLeftKey)) || 
                    ((Joystick0DownKey > 0) && _keyboardService.IsKeyDown(Joystick0DownKey)) || 
                    ((Joystick0DownRightKey > 0) && _keyboardService.IsKeyDown(Joystick0DownRightKey)))
                {
                    paddle1 += PaddleScale;
                }
                if (((Joystick1UpLeftKey > 0) && _keyboardService.IsKeyDown(Joystick1UpLeftKey)) || 
                    ((Joystick1LeftKey > 0) && _keyboardService.IsKeyDown(Joystick1LeftKey)) || 
                    ((Joystick1DownLeftKey > 0) && _keyboardService.IsKeyDown(Joystick1DownLeftKey)))
                {
                    paddle2 -= PaddleScale;
                }
                if (((Joystick1UpRightKey > 0) && _keyboardService.IsKeyDown(Joystick1UpRightKey)) || 
                    ((Joystick1RightKey > 0) && _keyboardService.IsKeyDown(Joystick1RightKey)) || 
                    ((Joystick1DownRightKey > 0) && _keyboardService.IsKeyDown(Joystick1DownRightKey)))
                {
                    paddle2 += PaddleScale;
                }
                if (((Joystick1UpLeftKey > 0) && _keyboardService.IsKeyDown(Joystick1UpLeftKey)) || 
                    ((Joystick1UpKey > 0) && _keyboardService.IsKeyDown(Joystick1UpKey)) || 
                    ((Joystick1UpRightKey > 0) && _keyboardService.IsKeyDown(Joystick1UpRightKey)))
                {
                    paddle3 -= PaddleScale;
                }
                if (((Joystick1DownLeftKey > 0) && _keyboardService.IsKeyDown(Joystick1DownLeftKey)) || 
                    ((Joystick1DownKey > 0) && _keyboardService.IsKeyDown(Joystick1DownKey)) || 
                    ((Joystick1DownRightKey > 0) && _keyboardService.IsKeyDown(Joystick1DownRightKey)))
                {
                    paddle3 += PaddleScale;
                }
            }
            if (InvertPaddles)
            {
                paddle0 = 2 * PaddleScale - paddle0;
                paddle1 = 2 * PaddleScale - paddle1;
                paddle2 = 2 * PaddleScale - paddle2;
                paddle3 = 2 * PaddleScale - paddle3;
            }

            Paddle0Strobe = true;
            Paddle1Strobe = true;
            Paddle2Strobe = true;
            Paddle3Strobe = true;

            Machine.Events.AddEvent(MathHelpers.ClampByte(SwapPaddles ? paddle1 : paddle0) * CyclesPerValue, _resetPaddle0StrobeEvent); // [7-29]
            Machine.Events.AddEvent(MathHelpers.ClampByte(SwapPaddles ? paddle0 : paddle1) * CyclesPerValue, _resetPaddle1StrobeEvent);
            Machine.Events.AddEvent(MathHelpers.ClampByte(SwapPaddles ? paddle3 : paddle2) * CyclesPerValue, _resetPaddle2StrobeEvent);
            Machine.Events.AddEvent(MathHelpers.ClampByte(SwapPaddles ? paddle2 : paddle3) * CyclesPerValue, _resetPaddle3StrobeEvent);
        }

        private void ResetPaddle0StrobeEvent()
        {
            Paddle0Strobe = false;
        }

        private void ResetPaddle1StrobeEvent()
        {
            Paddle1Strobe = false;
        }

        private void ResetPaddle2StrobeEvent()
        {
            Paddle2Strobe = false;
        }

        private void ResetPaddle3StrobeEvent()
        {
            Paddle3Strobe = false;
        }

        public const int PaddleScale = 128;

        public bool InvertPaddles { get; set; }
        public bool SwapPaddles { get; set; }
        public bool UseShiftKeyMod { get; set; }
        public float JoystickDeadZone { get; set; }

        public bool UseKeyboard { get; set; }
        public int Joystick0UpLeftKey { get; set; }
        public int Joystick0UpKey { get; set; }
        public int Joystick0UpRightKey { get; set; }
        public int Joystick0LeftKey { get; set; }
        public int Joystick0RightKey { get; set; }
        public int Joystick0DownLeftKey { get; set; }
        public int Joystick0DownKey { get; set; }
        public int Joystick0DownRightKey { get; set; }
        public int Joystick1UpLeftKey { get; set; }
        public int Joystick1UpKey { get; set; }
        public int Joystick1UpRightKey { get; set; }
        public int Joystick1LeftKey { get; set; }
        public int Joystick1RightKey { get; set; }
        public int Joystick1DownLeftKey { get; set; }
        public int Joystick1DownKey { get; set; }
        public int Joystick1DownRightKey { get; set; }
        public int Button0Key { get; set; }
        public int Button1Key { get; set; }
        public int Button2Key { get; set; }

        public bool UseTouch { get; set; }
        public float Joystick0TouchX { get; set; }
        public float Joystick0TouchY { get; set; }
        public float Joystick0TouchWidth { get; set; }
        public float Joystick0TouchHeight { get; set; }
        public int Joystick0TouchOrder { get; set; }
        public float Joystick0TouchRadius { get; set; }
        public bool Joystick0TouchKeepLast { get; set; }
        public float Joystick1TouchX { get; set; }
        public float Joystick1TouchY { get; set; }
        public float Joystick1TouchWidth { get; set; }
        public float Joystick1TouchHeight { get; set; }
        public int Joystick1TouchOrder { get; set; }
        public float Joystick1TouchRadius { get; set; }
        public bool Joystick1TouchKeepLast { get; set; }
        public float Button0TouchX { get; set; }
        public float Button0TouchY { get; set; }
        public float Button0TouchWidth { get; set; }
        public float Button0TouchHeight { get; set; }
        public int Button0TouchOrder { get; set; }
        public float Button1TouchX { get; set; }
        public float Button1TouchY { get; set; }
        public float Button1TouchWidth { get; set; }
        public float Button1TouchHeight { get; set; }
        public int Button1TouchOrder { get; set; }
        public float Button2TouchX { get; set; }
        public float Button2TouchY { get; set; }
        public float Button2TouchWidth { get; set; }
        public float Button2TouchHeight { get; set; }
        public int Button2TouchOrder { get; set; }

        public bool Paddle0Strobe { get; private set; }
        public bool Paddle1Strobe { get; private set; }
        public bool Paddle2Strobe { get; private set; }
        public bool Paddle3Strobe { get; private set; }

        private const int CyclesPerValue = 11;

        private Action _resetPaddle0StrobeEvent;
        private Action _resetPaddle1StrobeEvent;
        private Action _resetPaddle2StrobeEvent;
        private Action _resetPaddle3StrobeEvent;

        private KeyboardService _keyboardService;
        private GamePortService _gamePortService;*/
    }
}
