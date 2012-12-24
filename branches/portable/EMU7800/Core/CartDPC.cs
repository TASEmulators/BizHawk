namespace EMU7800.Core
{
    /// <summary>
    /// Pitfall II cartridge.
    /// There are two 4k banks, 2k display bank, and the DPC chip.
    /// For complete details on the DPC chip see David P. Crane's United States Patent Number 4,644,495.
    /// </summary>
    public sealed class CartDPC : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank1: 0x0000:0x1000       0x1000:0x1000  Bank selected by accessing 0x1ff8,0x1ff9
        // Bank2: 0x1000:0x1000
        //
        const ushort DisplayBaseAddr = 0x2000;
        ushort BankBaseAddr;

        readonly byte[] MusicAmplitudes = new byte[] { 0x00, 0x04, 0x05, 0x09, 0x06, 0x0a, 0x0b, 0x0f };

        readonly byte[] Tops = new byte[8];
        readonly byte[] Bots = new byte[8];
        readonly ushort[] Counters = new ushort[8];
        readonly byte[] Flags = new byte[8];
        readonly bool[] MusicMode = new bool[3];

        ulong LastSystemClock;
        double FractionalClocks;

        byte _ShiftRegister;

        int Bank
        {
            set { BankBaseAddr = (ushort)(value * 0x1000); }
        }

        //
        // Generate a sequence of pseudo-random numbers 255 numbers long
        // by emulating an 8-bit shift register with feedback taps at
        // bits 4, 3, 2, and 0.
        byte ShiftRegister
        {
            get
            {
                var a = _ShiftRegister;
                a &= (1 << 0);

                var x = _ShiftRegister;
                x &= (1 << 2);
                x >>= 2;
                a ^= x;

                x = _ShiftRegister;
                x &= (1 << 3);
                x >>= 3;
                a ^= x;

                x = _ShiftRegister;
                x &= (1 << 4);
                x >>= 4;
                a ^= x;

                a <<= 7;
                _ShiftRegister >>= 1;
                _ShiftRegister |= a;

                return _ShiftRegister;
            }
            set { _ShiftRegister = value; }
        }

        #region IDevice Members

        public override void Reset()
        {
            Bank = 1;
            LastSystemClock = 3*M.CPU.Clock;
            FractionalClocks = 0.0;
            ShiftRegister = 1;
        }

        public override byte this[ushort addr]
        {
            get
            {
                addr &= 0x0fff;
                if (addr < 0x0040)
                {
                    return ReadPitfall2Reg(addr);
                }
                UpdateBank(addr);
                return ROM[BankBaseAddr + addr];
            }
            set
            {
                addr &= 0x0fff;
                if (addr >= 0x0040 && addr < 0x0080)
                {
                    WritePitfall2Reg(addr, value);
                }
                else
                {
                    UpdateBank(addr);
                }
            }
        }

        #endregion

        private CartDPC()
        {
        }

        public CartDPC(byte[] romBytes)
        {
            LoadRom(romBytes, 0x2800);
            Bank = 1;
        }

        void UpdateBank(ushort addr)
        {
            switch(addr)
            {
                case 0x0ff8:
                    Bank = 0;
                    break;
                case 0x0ff9:
                    Bank = 1;
                    break;
            }
        }

        byte ReadPitfall2Reg(ushort addr)
        {
            byte result;

            var i = addr & 0x07;
            var fn = (addr >> 3) & 0x07;

            // Update flag register for selected data fetcher
            if ((Counters[i] & 0x00ff) == Tops[i])
            {
                Flags[i] = 0xff;
            } 
            else if ((Counters[i] & 0x00ff) == Bots[i])
            {
                Flags[i] = 0x00;
            }
        
            switch (fn)
            {
                case 0x00:
                    if (i < 4)
                    {
                        // This is a random number read
                        result = ShiftRegister;
                        break;
                    }
                    // Its a music read
                    UpdateMusicModeDataFetchers();

                    byte j = 0;
                    if (MusicMode[0] && Flags[5] != 0)
                    {
                        j |= 0x01;
                    }
                    if (MusicMode[1] && Flags[6] != 0)
                    {
                        j |= 0x02;
                    }
                    if (MusicMode[2] && Flags[7] != 0)
                    {
                        j |= 0x04;
                    }
                    result = MusicAmplitudes[j];
                    break;
                    // DFx display data read
                case 0x01:
                    result = ROM[DisplayBaseAddr + 0x7ff - Counters[i]];
                    break;
                    // DFx display data read AND'd w/flag
                case 0x02:
                    result = ROM[DisplayBaseAddr + 0x7ff - Counters[i]];
                    result &= Flags[i];
                    break;
                    // DFx flag
                case 0x07:
                    result = Flags[i];
                    break;
                default:
                    result = 0;
                    break;
            }

            // Clock the selected data fetcher's counter if needed
            if (i < 5 || i >= 5 && MusicMode[i - 5] == false)
            {
                Counters[i]--;
                Counters[i] &= 0x07ff;
            }
 
            return result;
        }

        void UpdateMusicModeDataFetchers()
        {
            var sysClockDelta = 3*M.CPU.Clock - LastSystemClock;
            LastSystemClock = 3*M.CPU.Clock;

            var OSCclocks = ((15750.0 * sysClockDelta) / 1193191.66666667) + FractionalClocks;

            var wholeClocks = (int)OSCclocks;
            FractionalClocks = OSCclocks - wholeClocks;
            if (wholeClocks <= 0)
            {
                return;
            }

            for (var i=0; i < 3; i++)
            {
                var r = i + 5;
                if (!MusicMode[i]) continue;

                var top = Tops[r] + 1;
                var newLow = Counters[r] & 0x00ff;

                if (Tops[r] != 0)
                {
                    newLow -= (wholeClocks % top);
                    if (newLow < 0) 
                    {
                        newLow += top;
                    }
                } 
                else
                {
                    newLow = 0;
                }

                if (newLow <= Bots[r])
                {
                    Flags[r] = 0x00;
                } 
                else if (newLow <= Tops[r])
                {
                    Flags[r] = 0xff;
                }

                Counters[r] = (ushort)((Counters[r] & 0x0700) | (ushort)newLow);
            }
        }

        void WritePitfall2Reg(ushort addr, byte val)
        {
            var i = addr & 0x07;
            var fn = (addr >> 3) & 0x07;

            switch (fn)
            {
                    // DFx top count
                case 0x00:
                    Tops[i] = val;
                    Flags[i] = 0x00;
                    break;
                    // DFx bottom count
                case 0x01:
                    Bots[i] = val;
                    break;
                    // DFx counter low
                case 0x02:
                    Counters[i] &= 0x0700;
                    if (i >= 5 && MusicMode[i - 5])
                    {
                        // Data fetcher is in music mode so its low counter value
                        // should be loaded from the top register not the poked value
                        Counters[i] |= Tops[i];
                    }
                    else
                    {
                        // Data fetcher is either not a music mode data fetcher or it
                        // isn't in music mode so it's low counter value should be loaded
                        // with the poked value
                        Counters[i] |= val;
                    }
                    break;
                    // DFx counter high
                case 0x03:
                    Counters[i] &= 0x00ff;
                    Counters[i] |= (ushort)((val & 0x07) << 8);
                    // Execute special code for music mode data fetchers
                    if (i >= 5)
                    {
                        MusicMode[i - 5] = (val & 0x10) != 0;
                        // NOTE: We are not handling the clock source input for
                        // the music mode data fetchers.  We're going to assume
                        // they always use the OSC input.
                    }
                    break;
                    // Random Number Generator Reset
                case 0x06:
                    ShiftRegister = 1;
                    break;
            }
        }

        #region Serialization Members

        public CartDPC(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x2800), 0x2800);
            BankBaseAddr = input.ReadUInt16();
            Tops = input.ReadExpectedBytes(8);
            Bots = input.ReadExpectedBytes(8);
            Counters = input.ReadUnsignedShorts(8);
            Flags = input.ReadExpectedBytes(8);
            MusicMode = input.ReadBooleans(3);
            LastSystemClock = input.ReadUInt64();
            FractionalClocks = input.ReadDouble();
            _ShiftRegister = input.ReadByte();
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(ROM);
            output.Write(BankBaseAddr);
            output.Write(Tops);
            output.Write(Bots);
            output.Write(Counters);
            output.Write(Flags);
            output.Write(MusicMode);
            output.Write(LastSystemClock);
            output.Write(FractionalClocks);
            output.Write(_ShiftRegister);
        }

        #endregion
    }
}