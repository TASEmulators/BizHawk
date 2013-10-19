namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 SuperGame bankswitched cartridge w/Pokey
    /// </summary>
    public sealed class Cart78SGP : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank0: 0x00000:0x4000
        // Bank1: 0x04000:0x4000      0x4000:0x4000  Pokey
        // Bank2: 0x08000:0x4000      0x8000:0x4000  Bank0-7 (0 on startup)
        // Bank3: 0x0c000:0x4000      0xc000:0x4000  Bank7
        // Bank4: 0x10000:0x4000
        // Bank5: 0x14000:0x4000
        // Bank6: 0x18000:0x4000
        // Bank7: 0x1c000:0x4000
        //
        readonly int[] _bank = new int[4];
        PokeySound _pokeySound;

        #region IDevice Members

        public override void Reset()
        {
            base.Reset();
            if (_pokeySound != null)
                _pokeySound.Reset();
        }

        public override byte this[ushort addr]
        {
            get
            {
                var bankNo = addr >> 14;
                switch (bankNo)
                {
                    case 1:
                        return (_pokeySound != null) ? _pokeySound.Read(addr) : (byte)0;
                    default:
                        return ROM[(_bank[bankNo] << 14) | (addr & 0x3fff)];
                }
            }
            set
            {
                var bankNo = addr >> 14;
                switch (bankNo)
                {
                    case 1:
                        if (_pokeySound != null)
                            _pokeySound.Update(addr, value);
                        break;
                    case 2:
                        _bank[2] = value & 7;
                        break;
                }
            }
        }

        #endregion

        public override void Attach(MachineBase m)
        {
            base.Attach(m);
            if (_pokeySound == null)
                _pokeySound = new PokeySound(M);
        }

        public override void StartFrame()
        {
            if (_pokeySound != null)
                _pokeySound.StartFrame();
        }

        public override void EndFrame()
        {
            if (_pokeySound != null)
                _pokeySound.EndFrame();
        }

        private Cart78SGP()
        {
        }

        public Cart78SGP(byte[] romBytes)
        {
            _bank[2] = 0;
            _bank[3] = 7;

            LoadRom(romBytes, 0x20000);
        }

        #region Serialization Members

        public Cart78SGP(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadBytes());
            _bank = input.ReadIntegers(4);
            _pokeySound = input.ReadOptionalPokeySound(m);
        }

        public override void GetObjectData(SerializationContext output)
        {
            if (_pokeySound == null)
                throw new Emu7800SerializationException("Cart78SGP must be attached before serialization.");

            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(ROM);
            output.Write(_bank);
            output.WriteOptional(_pokeySound);
        }

        #endregion
    }
}