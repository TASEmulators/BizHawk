using System;
using System.IO;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    public sealed class Keyboard : MachineComponent
    {
        public Keyboard(Machine machine) :
            base(machine)
        {
        }

        public override void Initialize()
        {
            _keyboardService = Machine.Services.GetService<KeyboardService>();

            UseGamePort = true; // Raster Blaster
            Button2Key = ' ';
        }

        public override void LoadState(BinaryReader reader, Version version)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            DisableResetKey = reader.ReadBoolean();

            UseGamePort = reader.ReadBoolean();
            Joystick0UpLeftKey = reader.ReadInt32();
            Joystick0UpKey = reader.ReadInt32();
            Joystick0UpRightKey = reader.ReadInt32();
            Joystick0LeftKey = reader.ReadInt32();
            Joystick0RightKey = reader.ReadInt32();
            Joystick0DownLeftKey = reader.ReadInt32();
            Joystick0DownKey = reader.ReadInt32();
            Joystick0DownRightKey = reader.ReadInt32();
            Joystick1UpLeftKey = reader.ReadInt32();
            Joystick1UpKey = reader.ReadInt32();
            Joystick1UpRightKey = reader.ReadInt32();
            Joystick1LeftKey = reader.ReadInt32();
            Joystick1RightKey = reader.ReadInt32();
            Joystick1DownLeftKey = reader.ReadInt32();
            Joystick1DownKey = reader.ReadInt32();
            Joystick1DownRightKey = reader.ReadInt32();
            Button0Key = reader.ReadInt32();
            Button1Key = reader.ReadInt32();
            Button2Key = reader.ReadInt32();
        }

        public override void SaveState(BinaryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.Write(DisableResetKey);

            writer.Write(UseGamePort);
            writer.Write(Joystick0UpLeftKey);
            writer.Write(Joystick0UpKey);
            writer.Write(Joystick0UpRightKey);
            writer.Write(Joystick0LeftKey);
            writer.Write(Joystick0RightKey);
            writer.Write(Joystick0DownLeftKey);
            writer.Write(Joystick0DownKey);
            writer.Write(Joystick0DownRightKey);
            writer.Write(Joystick1UpLeftKey);
            writer.Write(Joystick1UpKey);
            writer.Write(Joystick1UpRightKey);
            writer.Write(Joystick1LeftKey);
            writer.Write(Joystick1RightKey);
            writer.Write(Joystick1DownLeftKey);
            writer.Write(Joystick1DownKey);
            writer.Write(Joystick1DownRightKey);
            writer.Write(Button0Key);
            writer.Write(Button1Key);
            writer.Write(Button2Key);
        }

        public void ResetStrobe()
        {
            Strobe = false;
        }

        public bool DisableResetKey { get; set; }

        public bool UseGamePort { get; set; }
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

        public bool IsAnyKeyDown { get { return _keyboardService.IsAnyKeyDown; } }
        public int Latch { get { return _latch; } set { _latch = value; Strobe = true; } }
        public bool Strobe { get; private set; }

        private KeyboardService _keyboardService;

        private int _latch;
    }
}
