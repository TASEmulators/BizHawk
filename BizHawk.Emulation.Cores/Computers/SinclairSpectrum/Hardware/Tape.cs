using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /*
     *  Much of the TAPE implementation has been taken from: https://github.com/Dotneteer/spectnetide
     *  
     *  MIT License

        Copyright (c) 2017 Istvan Novak

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.
     */

    /// <summary>
    /// Represents the tape device (or DATACORDER as AMSTRAD liked to call it)
    /// </summary>
    public class Tape
    {
        private SpectrumBase _machine { get; set; }
        private Z80A _cpu { get; set; }
        private Buzzer _buzzer { get; set; }

        private TapeOperationMode _currentMode;
        private TapeFilePlayer _tapePlayer;
        private bool _micBitState;
        private long _lastMicBitActivityCycle;
        private SavePhase _savePhase;
        private int _pilotPulseCount;
        private int _bitOffset;
        private byte _dataByte;
        private int _dataLength;
        private byte[] _dataBuffer;
        private int _dataBlockCount;
        private MicPulseType _prevDataPulse;

        /// <summary>
        /// Number of tacts after save mod can be exited automatically
        /// </summary>
        public const int SAVE_STOP_SILENCE = 17500000;

        /// <summary>
        /// The address of the ERROR routine in the Spectrum ROM
        /// </summary>
        public const ushort ERROR_ROM_ADDRESS = 0x0008;

        /// <summary>
        /// The maximum distance between two scans of the EAR bit
        /// </summary>
        public const int MAX_TACT_JUMP = 10000;

        /// <summary>
        /// The width tolerance of save pulses
        /// </summary>
        public const int SAVE_PULSE_TOLERANCE = 24;

        /// <summary>
        /// Minimum number of pilot pulses before SYNC1
        /// </summary>
        public const int MIN_PILOT_PULSE_COUNT = 3000;

        /// <summary>
        /// Lenght of the data buffer to allocate for the SAVE operation
        /// </summary>
        public const int DATA_BUFFER_LENGTH = 0x10000;

        /// <summary>
        /// Gets the tape content provider
        /// </summary>
        public ITapeProvider TapeProvider { get; }

        /// <summary>
        /// The TapeFilePlayer that can playback tape content
        /// </summary>
        public TapeFilePlayer TapeFilePlayer => _tapePlayer;

        /// <summary>
        /// The current operation mode of the tape
        /// </summary>
        public TapeOperationMode CurrentMode => _currentMode;


        private bool _fastLoad = false;


        public virtual void Init(SpectrumBase machine)
        {
            _machine = machine;
            _cpu = _machine.CPU;
            _buzzer = machine.BuzzerDevice;
            Reset();
        }

        public Tape(ITapeProvider tapeProvider)
        {
            TapeProvider = tapeProvider;
        }

        public virtual void Reset()
        {
            TapeProvider?.Reset();
            _tapePlayer = null;
            _currentMode = TapeOperationMode.Passive;
            _savePhase = SavePhase.None;
            _micBitState = true;
        }

        public void CPUFrameCompleted()
        {
            SetTapeMode();
            if (CurrentMode == TapeOperationMode.Load
                && _fastLoad
                && TapeFilePlayer != null
                && TapeFilePlayer.PlayPhase != PlayPhase.Completed
                && _cpu.RegPC == 1529) //_machine.RomData.LoadBytesRoutineAddress)
            {
                
                if (FastLoadFromTzx())
                {
                    //FastLoadCompleted?.Invoke(this, EventArgs.Empty);
                }
                
            }
        }

        /// <summary>
        /// Sets the current tape mode according to the current PC register
        /// and the MIC bit state
        /// </summary>
        public void SetTapeMode()
        {
            switch (_currentMode)
            {
                case TapeOperationMode.Passive:
                    if (_cpu.RegPC == 1523) // _machine.RomData.LoadBytesRoutineAddress) //1529
                    {
                        EnterLoadMode();
                    }
                    else if (_cpu.RegPC == 2416) // _machine.RomData.SaveBytesRoutineAddress)
                    {
                        EnterSaveMode();
                    }

                    var res = _cpu.RegPC;
                    var res2 = _machine.Spectrum.RegPC;

                    return;
                case TapeOperationMode.Save:
                    if (_cpu.RegPC == ERROR_ROM_ADDRESS
                        || (int)(_cpu.TotalExecutedCycles - _lastMicBitActivityCycle) > SAVE_STOP_SILENCE)
                    {
                        LeaveSaveMode();
                    }
                    return;
                case TapeOperationMode.Load:
                    if ((_tapePlayer?.Eof ?? false) || _cpu.RegPC == ERROR_ROM_ADDRESS)
                    {
                        LeaveLoadMode();
                    }
                    return;
            }
        }

        /// <summary>
        /// Puts the device in save mode. From now on, every MIC pulse is recorded
        /// </summary>
        private void EnterSaveMode()
        {
            _currentMode = TapeOperationMode.Save;
            _savePhase = SavePhase.None;
            _micBitState = true;
            _lastMicBitActivityCycle = _cpu.TotalExecutedCycles;
            _pilotPulseCount = 0;
            _prevDataPulse = MicPulseType.None;
            _dataBlockCount = 0;
            TapeProvider?.CreateTapeFile();
        }

        /// <summary>
        /// Leaves the save mode. Stops recording MIC pulses
        /// </summary>
        private void LeaveSaveMode()
        {
            _currentMode = TapeOperationMode.Passive;
            TapeProvider?.FinalizeTapeFile();
        }

        /// <summary>
        /// Puts the device in load mode. From now on, EAR pulses are played by a device
        /// </summary>
        private void EnterLoadMode()
        {
            _currentMode = TapeOperationMode.Load;

            var contentReader = TapeProvider?.GetTapeContent();
            if (contentReader == null) return;

            // --- Play the content
            _tapePlayer = new TapeFilePlayer(contentReader);
            _tapePlayer.ReadContent();
            _tapePlayer.InitPlay(_cpu.TotalExecutedCycles);
            _buzzer.SetTapeMode(true);
        }

        /// <summary>
        /// Leaves the load mode. Stops the device that playes EAR pulses
        /// </summary>
        private void LeaveLoadMode()
        {
            _currentMode = TapeOperationMode.Passive;
            _tapePlayer = null;
            TapeProvider?.Reset();
            _buzzer.SetTapeMode(false);
        }

        /// <summary>
        /// Loads the next TZX player block instantly without emulation
        /// EAR bit processing
        /// </summary>
        /// <returns>True, if fast load is operative</returns>
        private bool FastLoadFromTzx()
        {
            var c = _machine.Spectrum;

            var blockType = TapeFilePlayer.CurrentBlock.GetType();
            bool canFlash = TapeFilePlayer.CurrentBlock is ITapeData;

            // --- Check, if we can load the current block in a fast way
            if (!(TapeFilePlayer.CurrentBlock is ITapeData)
                || TapeFilePlayer.PlayPhase == PlayPhase.Completed)
            {
                // --- We cannot play this block
                return false;
            }

            var currentData = TapeFilePlayer.CurrentBlock as ITapeData;

            var regs = _cpu.Regs;

            //regs.AF = regs._AF_;
            //c.Set16BitAF(c.Get16BitAF_());
            _cpu.A = _cpu.A_s;
            _cpu.F = _cpu.F_s;

            // --- Check if the operation is LOAD or VERIFY
            var isVerify = (c.RegAF & 0xFF01) == 0xFF00;

            // --- At this point IX contains the address to load the data, 
            // --- DE shows the #of bytes to load. A contains 0x00 for header, 
            // --- 0xFF for data block
            var data = currentData.Data;
            if (data[0] != regs[_cpu.A])
            {
                // --- This block has a different type we're expecting
                regs[_cpu.A] = (byte)(regs[_cpu.A] ^ regs[_cpu.L]);
                regs[_cpu.F] &= (byte)ZXSpectrum.FlagsResetMask.Z;
                regs[_cpu.F] &= (byte)ZXSpectrum.FlagsResetMask.C;
                c.RegPC = _machine.RomData.LoadBytesInvalidHeaderAddress;

                // --- Get the next block
                TapeFilePlayer.NextBlock(_cpu.TotalExecutedCycles);
                return true;
            }

            // --- It is time to load the block
            var curIndex = 1;
            //var memory = _machine.me MemoryDevice;
            regs[_cpu.H] = regs[_cpu.A];
            while (c.RegDE > 0)
            {
                var de16 = c.RegDE;
                var ix16 = c.RegIX;
                if (curIndex > data.Length - 1)
                {
                    // --- No more data to read
                    //break;
                }

                regs[_cpu.L] = data[curIndex];
                if (isVerify && regs[_cpu.L] != _machine.ReadBus(c.RegIX))
                {
                    // --- Verify failed
                    regs[_cpu.A] = (byte)(_machine.ReadBus(c.RegIX) ^ regs[_cpu.L]);
                    regs[_cpu.F] &= (byte)ZXSpectrum.FlagsResetMask.Z;
                    regs[_cpu.F] &= (byte)ZXSpectrum.FlagsResetMask.C;
                    c.RegPC = _machine.RomData.LoadBytesInvalidHeaderAddress;
                    return true;
                }

                // --- Store the loaded data byte
                _machine.WriteBus(c.RegIX, (byte)regs[_cpu.L]);
                regs[_cpu.H] ^= regs[_cpu.L];
                curIndex++;
                //regs.IX++;
                //c.Set16BitIX((ushort)((int)c.Get16BitIX() + 1));
                c.RegIX++;
                //regs.DE--;
                //c.Set16BitDE((ushort)((int)c.Get16BitDE() - 1));
                //_cpu.Regs[_cpu.E]--;
                c.RegDE--;
                var te = c.RegDE;
            }

            // --- Check the parity byte at the end of the data stream
            if (curIndex > data.Length - 1 || regs[_cpu.H] != data[curIndex])
            {
                // --- Carry is reset to sign an error
                regs[_cpu.F] &= (byte)ZXSpectrum.FlagsResetMask.C;
            }
            else
            {
                // --- Carry is set to sign success
                regs[_cpu.F] |= (byte)ZXSpectrum.FlagsSetMask.C;
            }
            c.RegPC = _machine.RomData.LoadBytesResumeAddress;

            // --- Get the next block
            TapeFilePlayer.NextBlock(_cpu.TotalExecutedCycles);
            return true;
        }


        /// <summary>
        /// the EAR bit read from tape
        /// </summary>
        /// <param name="cpuCycles"></param>
        /// <returns></returns>
        public virtual bool GetEarBit(int cpuCycles)
        {
            if (_currentMode != TapeOperationMode.Load)
            {
                return true;
            }

            var earBit = _tapePlayer?.GetEarBit(cpuCycles) ?? true;
            _buzzer.ProcessPulseValue(true, earBit);
            return earBit;
        }

        /// <summary>
        /// Processes the mic bit change
        /// </summary>
        /// <param name="micBit"></param>
        public virtual void ProcessMicBit(bool micBit)
        {
            if (_currentMode != TapeOperationMode.Save
                || _micBitState == micBit)
            {
                return;
            }

            var length = _cpu.TotalExecutedCycles - _lastMicBitActivityCycle;

            // --- Classify the pulse by its width
            var pulse = MicPulseType.None;
            if (length >= TapeDataBlockPlayer.BIT_0_PL - SAVE_PULSE_TOLERANCE
                && length <= TapeDataBlockPlayer.BIT_0_PL + SAVE_PULSE_TOLERANCE)
            {
                pulse = MicPulseType.Bit0;
            }
            else if (length >= TapeDataBlockPlayer.BIT_1_PL - SAVE_PULSE_TOLERANCE
                && length <= TapeDataBlockPlayer.BIT_1_PL + SAVE_PULSE_TOLERANCE)
            {
                pulse = MicPulseType.Bit1;
            }
            if (length >= TapeDataBlockPlayer.PILOT_PL - SAVE_PULSE_TOLERANCE
                && length <= TapeDataBlockPlayer.PILOT_PL + SAVE_PULSE_TOLERANCE)
            {
                pulse = MicPulseType.Pilot;
            }
            else if (length >= TapeDataBlockPlayer.SYNC_1_PL - SAVE_PULSE_TOLERANCE
                     && length <= TapeDataBlockPlayer.SYNC_1_PL + SAVE_PULSE_TOLERANCE)
            {
                pulse = MicPulseType.Sync1;
            }
            else if (length >= TapeDataBlockPlayer.SYNC_2_PL - SAVE_PULSE_TOLERANCE
                     && length <= TapeDataBlockPlayer.SYNC_2_PL + SAVE_PULSE_TOLERANCE)
            {
                pulse = MicPulseType.Sync2;
            }
            else if (length >= TapeDataBlockPlayer.TERM_SYNC - SAVE_PULSE_TOLERANCE
                     && length <= TapeDataBlockPlayer.TERM_SYNC + SAVE_PULSE_TOLERANCE)
            {
                pulse = MicPulseType.TermSync;
            }
            else if (length < TapeDataBlockPlayer.SYNC_1_PL - SAVE_PULSE_TOLERANCE)
            {
                pulse = MicPulseType.TooShort;
            }
            else if (length > TapeDataBlockPlayer.PILOT_PL + 2 * SAVE_PULSE_TOLERANCE)
            {
                pulse = MicPulseType.TooLong;
            }

            _micBitState = micBit;
            _lastMicBitActivityCycle = _cpu.TotalExecutedCycles;

            // --- Lets process the pulse according to the current SAVE phase and pulse width
            var nextPhase = SavePhase.Error;
            switch (_savePhase)
            {
                case SavePhase.None:
                    if (pulse == MicPulseType.TooShort || pulse == MicPulseType.TooLong)
                    {
                        nextPhase = SavePhase.None;
                    }
                    else if (pulse == MicPulseType.Pilot)
                    {
                        _pilotPulseCount = 1;
                        nextPhase = SavePhase.Pilot;
                    }
                    break;
                case SavePhase.Pilot:
                    if (pulse == MicPulseType.Pilot)
                    {
                        _pilotPulseCount++;
                        nextPhase = SavePhase.Pilot;
                    }
                    else if (pulse == MicPulseType.Sync1 && _pilotPulseCount >= MIN_PILOT_PULSE_COUNT)
                    {
                        nextPhase = SavePhase.Sync1;
                    }
                    break;
                case SavePhase.Sync1:
                    if (pulse == MicPulseType.Sync2)
                    {
                        nextPhase = SavePhase.Sync2;
                    }
                    break;
                case SavePhase.Sync2:
                    if (pulse == MicPulseType.Bit0 || pulse == MicPulseType.Bit1)
                    {
                        // --- Next pulse starts data, prepare for receiving it
                        _prevDataPulse = pulse;
                        nextPhase = SavePhase.Data;
                        _bitOffset = 0;
                        _dataByte = 0;
                        _dataLength = 0;
                        _dataBuffer = new byte[DATA_BUFFER_LENGTH];
                    }
                    break;
                case SavePhase.Data:
                    if (pulse == MicPulseType.Bit0 || pulse == MicPulseType.Bit1)
                    {
                        if (_prevDataPulse == MicPulseType.None)
                        {
                            // --- We are waiting for the second half of the bit pulse
                            _prevDataPulse = pulse;
                            nextPhase = SavePhase.Data;
                        }
                        else if (_prevDataPulse == pulse)
                        {
                            // --- We received a full valid bit pulse
                            nextPhase = SavePhase.Data;
                            _prevDataPulse = MicPulseType.None;

                            // --- Add this bit to the received data
                            _bitOffset++;
                            _dataByte = (byte)(_dataByte * 2 + (pulse == MicPulseType.Bit0 ? 0 : 1));
                            if (_bitOffset == 8)
                            {
                                // --- We received a full byte
                                _dataBuffer[_dataLength++] = _dataByte;
                                _dataByte = 0;
                                _bitOffset = 0;
                            }
                        }
                    }
                    else if (pulse == MicPulseType.TermSync)
                    {
                        // --- We received the terminating pulse, the datablock has been completed
                        nextPhase = SavePhase.None;
                        _dataBlockCount++;

                        // --- Create and save the data block
                        var dataBlock = new TzxStandardSpeedDataBlock
                        {
                            Data = _dataBuffer,
                            DataLength = (ushort)_dataLength
                        };

                        // --- If this is the first data block, extract the name from the header
                        if (_dataBlockCount == 1 && _dataLength == 0x13)
                        {
                            // --- It's a header!
                            var sb = new StringBuilder(16);
                            for (var i = 2; i <= 11; i++)
                            {
                                sb.Append((char)_dataBuffer[i]);
                            }
                            var name = sb.ToString().TrimEnd();
                            TapeProvider?.SetName(name);
                        }
                        TapeProvider?.SaveTapeBlock(dataBlock);
                    }
                    break;
            }
            _savePhase = nextPhase;
        }

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("TapeDevice");
            ser.Sync("_micBitState", ref _micBitState);
            ser.Sync("_lastMicBitActivityCycle", ref _lastMicBitActivityCycle);
            ser.Sync("_pilotPulseCount", ref _pilotPulseCount);
            ser.Sync("_bitOffset", ref _bitOffset);
            ser.Sync("_dataByte", ref _dataByte);
            ser.Sync("_dataLength", ref _dataLength);
            ser.Sync("_dataBlockCount", ref _dataBlockCount);
            ser.Sync("_dataBuffer", ref _dataBuffer, false);
            ser.SyncEnum<TapeOperationMode>("_currentMode", ref _currentMode);
            ser.SyncEnum<SavePhase>("_savePhase", ref _savePhase);
            ser.SyncEnum<MicPulseType>("_prevDataPulse", ref _prevDataPulse);
            /*
        private TapeFilePlayer _tapePlayer;
        */

            ser.EndSection();
        }
    }

    /// <summary>
    /// This enum represents the operation mode of the tape
    /// </summary>
    public enum TapeOperationMode : byte
    {
        /// <summary>
        /// The tape device is passive
        /// </summary>
        Passive = 0,

        /// <summary>
        /// The tape device is saving information (MIC pulses)
        /// </summary>
        Save,

        /// <summary>
        /// The tape device generates EAR pulses from a player
        /// </summary>
        Load
    }

    /// <summary>
    /// This class represents a spectrum tape header
    /// </summary>
    public class SpectrumTapeHeader
    {
        private const int HEADER_LEN = 19;
        private const int TYPE_OFFS = 1;
        private const int NAME_OFFS = 2;
        private const int NAME_LEN = 10;
        private const int DATA_LEN_OFFS = 12;
        private const int PAR1_OFFS = 14;
        private const int PAR2_OFFS = 16;
        private const int CHK_OFFS = 18;

        /// <summary>
        /// The bytes of the header
        /// </summary>
        public byte[] HeaderBytes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public SpectrumTapeHeader()
        {
            HeaderBytes = new byte[HEADER_LEN];
            for (var i = 0; i < HEADER_LEN; i++) HeaderBytes[i] = 0x00;
            CalcChecksum();
        }

        /// <summary>
        /// Initializes a new instance with the specified header data.
        /// </summary>
        /// <param name="header">Header data</param>
        public SpectrumTapeHeader(byte[] header)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));
            if (header.Length != HEADER_LEN)
            {
                throw new ArgumentException($"Header must be exactly {HEADER_LEN} bytes long");
            }
            HeaderBytes = new byte[HEADER_LEN];
            header.CopyTo(HeaderBytes, 0);
            CalcChecksum();
        }

        /// <summary>
        /// Gets or sets the type of the header
        /// </summary>
        public byte Type
        {
            get { return HeaderBytes[TYPE_OFFS]; }
            set
            {
                HeaderBytes[TYPE_OFFS] = (byte)(value & 0x03);
                CalcChecksum();
            }
        }

        /// <summary>
        /// Gets or sets the program name
        /// </summary>
        public string Name
        {
            get
            {
                var name = new StringBuilder(NAME_LEN + 4);
                for (var i = NAME_OFFS; i < NAME_OFFS + NAME_LEN; i++)
                {
                    name.Append((char)HeaderBytes[i]);
                }
                return name.ToString().TrimEnd();
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value.Length > NAME_LEN) value = value.Substring(0, NAME_LEN);
                else if (value.Length < NAME_LEN) value = value.PadRight(NAME_LEN, ' ');
                for (var i = NAME_OFFS; i < NAME_OFFS + NAME_LEN; i++)
                {
                    HeaderBytes[i] = (byte)value[i - NAME_OFFS];
                }
                CalcChecksum();
            }
        }

        /// <summary>
        /// Gets or sets the Data Length
        /// </summary>
        public ushort DataLength
        {
            get { return GetWord(DATA_LEN_OFFS); }
            set { SetWord(DATA_LEN_OFFS, value); }
        }

        /// <summary>
        /// Gets or sets Parameter1
        /// </summary>
        public ushort Parameter1
        {
            get { return GetWord(PAR1_OFFS); }
            set { SetWord(PAR1_OFFS, value); }
        }

        /// <summary>
        /// Gets or sets Parameter2
        /// </summary>
        public ushort Parameter2
        {
            get { return GetWord(PAR2_OFFS); }
            set { SetWord(PAR2_OFFS, value); }
        }

        /// <summary>
        /// Gets the value of checksum
        /// </summary>
        public byte Checksum => HeaderBytes[CHK_OFFS];

        /// <summary>
        /// Calculate the checksum
        /// </summary>
        private void CalcChecksum()
        {
            var chk = 0x00;
            for (var i = 0; i < HEADER_LEN - 1; i++) chk ^= HeaderBytes[i];
            HeaderBytes[CHK_OFFS] = (byte)chk;
        }

        /// <summary>
        /// Gets the word value from the specified offset
        /// </summary>
        private ushort GetWord(int offset) =>
            (ushort)(HeaderBytes[offset] + 256 * HeaderBytes[offset + 1]);

        /// <summary>
        /// Sets the word value at the specified offset
        /// </summary>
        private void SetWord(int offset, ushort value)
        {
            HeaderBytes[offset] = (byte)(value & 0xff);
            HeaderBytes[offset + 1] = (byte)(value >> 8);
            CalcChecksum();
        }
    }

    /// <summary>
    /// This enum defines the MIC pulse types according to their widths
    /// </summary>
    public enum MicPulseType : byte
    {
        /// <summary>
        /// No pulse information
        /// </summary>
        None = 0,

        /// <summary>
        /// Too short to be a valid pulse
        /// </summary>
        TooShort,

        /// <summary>
        /// Too long to be a valid pulse
        /// </summary>
        TooLong,

        /// <summary>
        /// PILOT pulse (Length: 2168 cycles)
        /// </summary>
        Pilot,

        /// <summary>
        /// SYNC1 pulse (Length: 667 cycles)
        /// </summary>
        Sync1,

        /// <summary>
        /// SYNC2 pulse (Length: 735 cycles)
        /// </summary>
        Sync2,

        /// <summary>
        /// BIT0 pulse (Length: 855 cycles)
        /// </summary>
        Bit0,

        /// <summary>
        /// BIT1 pulse (Length: 1710 cycles)
        /// </summary>
        Bit1,

        /// <summary>
        /// TERM_SYNC pulse (Length: 947 cycles)
        /// </summary>
        TermSync
    }

    /// <summary>
    /// Represents the playing phase of the current block
    /// </summary>
    public enum PlayPhase
    {
        /// <summary>
        /// The player is passive
        /// </summary>
        None = 0,

        /// <summary>
        /// Pilot signals
        /// </summary>
        Pilot,

        /// <summary>
        /// Sync signals at the end of the pilot
        /// </summary>
        Sync,

        /// <summary>
        /// Bits in the data block
        /// </summary>
        Data,

        /// <summary>
        /// Short terminating sync signal before pause
        /// </summary>
        TermSync,

        /// <summary>
        /// Pause after the data block
        /// </summary>
        Pause,

        /// <summary>
        /// The entire block has been played back
        /// </summary>
        Completed
    }

    /// <summary>
    /// This enumeration defines the phases of the SAVE operation
    /// </summary>
    public enum SavePhase : byte
    {
        /// <summary>No SAVE operation is in progress</summary>
        None = 0,

        /// <summary>Emitting PILOT impulses</summary>
        Pilot,

        /// <summary>Emitting SYNC1 impulse</summary>
        Sync1,

        /// <summary>Emitting SYNC2 impulse</summary>
        Sync2,

        /// <summary>Emitting BIT0/BIT1 impulses</summary>
        Data,

        /// <summary>Unexpected pulse detected</summary>
        Error
    }
}
