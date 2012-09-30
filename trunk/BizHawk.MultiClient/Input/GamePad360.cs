using System;
using System.Collections.Generic;
using SlimDX;
using SlimDX.XInput;

namespace BizHawk.MultiClient
{
    public class GamePad360
    {
        // ********************************** Static interface **********************************

        public static List<GamePad360> Devices;

        public static void Initialize()
        {
            Devices = new List<GamePad360>();
            var c1 = new SlimDX.XInput.Controller(UserIndex.One);
            var c2 = new SlimDX.XInput.Controller(UserIndex.Two);
            var c3 = new SlimDX.XInput.Controller(UserIndex.Three);
            var c4 = new SlimDX.XInput.Controller(UserIndex.Four);

            if (c1.IsConnected) Devices.Add(new GamePad360(c1));
            if (c2.IsConnected) Devices.Add(new GamePad360(c2));
            if (c3.IsConnected) Devices.Add(new GamePad360(c3));
            if (c4.IsConnected) Devices.Add(new GamePad360(c4));
        }

        public static void UpdateAll()
        {
            foreach (var device in Devices)
                device.Update();
        }

        // ********************************** Instance Members **********************************

        readonly SlimDX.XInput.Controller controller;
        State state;

        GamePad360(SlimDX.XInput.Controller c)
        {
            controller = c;
            InitializeButtons();
            Update();
        }

        public void Update()
        {
            if (controller.IsConnected == false)
                return;

            state = controller.GetState();
        }

        public int NumButtons { get; private set; }

        List<string> names = new List<string>();
        List<Func<bool>> actions = new List<Func<bool>>();

        void InitializeButtons()
        {
            const int dzp = 9000;
            const int dzn = -9000;
            const int dzt = 40;

            AddItem("A", () => (state.Gamepad.Buttons & GamepadButtonFlags.A) != 0);
            AddItem("B", () => (state.Gamepad.Buttons & GamepadButtonFlags.B) != 0);
            AddItem("X", () => (state.Gamepad.Buttons & GamepadButtonFlags.X) != 0);
            AddItem("Y", () => (state.Gamepad.Buttons & GamepadButtonFlags.Y) != 0);

            AddItem("Start", () => (state.Gamepad.Buttons & GamepadButtonFlags.Start) != 0);
            AddItem("Back", () => (state.Gamepad.Buttons & GamepadButtonFlags.Back) != 0);
            AddItem("LeftThumb", () => (state.Gamepad.Buttons & GamepadButtonFlags.LeftThumb) != 0);
            AddItem("RightThumb", () => (state.Gamepad.Buttons & GamepadButtonFlags.RightThumb) != 0);
            AddItem("LeftShoulder", () => (state.Gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0);
            AddItem("RightShoulder", () => (state.Gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0);

            AddItem("DpadUp", () => (state.Gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0);
            AddItem("DpadDown", () => (state.Gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0);
            AddItem("DpadLeft", () => (state.Gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0);
            AddItem("DpadRight", () => (state.Gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0);

            AddItem("LStickUp", () => state.Gamepad.LeftThumbY >= dzp);
            AddItem("LStickDown", () => state.Gamepad.LeftThumbY <= dzn);
            AddItem("LStickLeft", () => state.Gamepad.LeftThumbX <= dzn);
            AddItem("LStickRight", () => state.Gamepad.LeftThumbX >= dzp);

            AddItem("RStickUp", () => state.Gamepad.RightThumbY >= dzp);
            AddItem("RStickDown", () => state.Gamepad.RightThumbY <= dzn);
            AddItem("RStickLeft", () => state.Gamepad.RightThumbX <= dzn);
            AddItem("RStickRight", () => state.Gamepad.RightThumbX >= dzp);
            
            AddItem("LeftTrigger", () => state.Gamepad.LeftTrigger > dzt);
            AddItem("RightTrigger", () => state.Gamepad.RightTrigger > dzt);
        }

        void AddItem(string name, Func<bool> pressed)
        {
            names.Add(name);
            actions.Add(pressed);
            NumButtons++;
        }

        public string ButtonName(int index)
        {
            return names[index];
        }

        public bool Pressed(int index)
        {
            return actions[index]();
        }
    }
}
