using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components
{
	/// <summary>
	/// AY-3-891x PSG sound DSP (shared core).
	///
	/// Based heavily on the YM-2149F / AY-3-8910 emulator used in Unreal Speccy
	/// (Originally created under Public Domain license by SMT jan.2006)
	///
	/// https://github.com/mkoloberdin/unrealspeccy/blob/master/sndrender/sndchip.cpp
	/// https://github.com/mkoloberdin/unrealspeccy/blob/master/sndrender/sndchip.h
	///
	/// This class contains ONLY the chip DSP and rendering. Host-bus specifics (port
	/// address decode, machine cycle timing) live in the platform adapters that wrap it.
	/// </summary>
	public sealed class AY391x : ISoundProvider, IDisposable
	{
		private int _tStatesPerFrame;
		private int _sampleRate;
		private int _samplesPerFrame;
		private short[] _audioBuffer;

		// Band-limited output. The chip is stepped at base-tick resolution and each change in the summed
		// L/R output is fed to a per-channel BlipBuffer as a delta, so tone/noise transitions between output
		// samples are captured and band-limited (no aliasing) instead of point-sampled.
		private BlipBuffer _blipL;
		private BlipBuffer _blipR;
		private int _lastL;
		private int _lastR;
		private int _blipClock;                       // base-tick position within the current frame
		private int _baseTicksPerFrame;               // chip base ticks (clock/8) in one host frame
		private double _baseTickRate;                 // chip base-tick rate = ChipClockHz / 8

		/// <summary>
		/// Main constructor
		/// </summary>
		public AY391x()
		{
		}

		/// <summary>
		/// Initialises the AY chip. chipClockHz is the AY master clock; hostClockHz is the clock that
		/// tStatesPerFrame is measured in (e.g. the CPU clock on the Spectrum, the gate-array clock on the
		/// CPC), so the base-tick and sample geometry are derived correctly for any machine/clock.
		/// </summary>
		public void Init(int sampleRate, int tStatesPerFrame, int chipClockHz, int hostClockHz)
		{
			InitTiming(sampleRate, tStatesPerFrame, chipClockHz, hostClockHz);
			UpdateVolume();
			Reset();
		}

		/// <summary>
		/// AY mixer panning configuration
		/// </summary>
		public enum PanConfig
		{
			MONO = 0,
			ABC = 1,
			ACB = 2,
			BAC = 3,
			BCA = 4,
			CAB = 5,
			CBA = 6,
		}

		/// <summary>
		/// The AY panning configuration
		/// </summary>
		public PanConfig PanningConfiguration
		{
			get => _currentPanTab;
			set
			{
				if (value != _currentPanTab)
				{
					_currentPanTab = value;
					UpdateVolume();
				}
			}
		}

		/// <summary>
		/// The AY chip output volume
		/// (0 - 100)
		/// </summary>
		public int Volume
		{
			get => _volume;
			set
			{
				//value = Math.Max(0, value);
				//value = Math.Max(100, value);
				if (_volume == value)
				{
					return;
				}
				_volume = value;
				UpdateVolume();
			}
		}

		/// <summary>
		/// Sets the volume field WITHOUT rebuilding the volume tables.
		/// Used to replicate the legacy savestate-load behaviour (restore stored
		/// volume without a table rebuild).
		/// </summary>
		public void SetVolumeNoRebuild(int v) => _volume = v;

		/// <summary>
		/// The master clock frequency of the AY chip (Hz). Drives the base-tick rate (clock/8) and the
		/// per-frame timing; set via Init.
		/// </summary>
		public int ChipClockHz { get; set; } = 1773400;

		/// <summary>
		/// The specific chip in the AY-3-891x / YM2149 family. Governs the number of I/O ports
		/// (8910 = A+B, 8912 = A only, 8913 = none), the envelope DAC resolution (YM2149 = 32 levels vs the
		/// AY's 16), and register read-back (YM returns the written value; the AY masks undefined bits to 0).
		/// Both the ZX Spectrum 128 and the Amstrad CPC use the AY-3-8912.
		/// </summary>
		public enum Variant
		{
			AY_3_8910,
			AY_3_8912,
			AY_3_8913,
			YM2149,
		}

		private Variant _variant = Variant.AY_3_8912;

		public Variant ChipVariant
		{
			get => _variant;
			set
			{
				if (value == _variant) return;
				_variant = value;
				if (_volumeTables != null) UpdateVolume(); // envelope DAC resolution depends on the variant
			}
		}

		/// <summary>
		/// YM2149 /SEL pin held low: the input clock is internally divided by 2 before the prescaler (lets a
		/// YM run from a higher external clock). No effect on the AY parts. Set before Init.
		/// </summary>
		public bool HalfClock { get; set; }

		private bool HasPortA => _variant != Variant.AY_3_8913;
		private bool HasPortB => _variant is Variant.AY_3_8910 or Variant.YM2149;

		/// <summary>
		/// The currently selected register
		/// </summary>
		public int SelectedRegister
		{
			get => _activeRegister;
			set => _activeRegister = (byte)value;
		}

		/// <summary>
		/// Generalized bus alias - latch the active register
		/// </summary>
		public void LatchAddress(int reg) => SelectedRegister = reg & 0x0f;

		/// <summary>
		/// Generalized bus alias - write to the active register
		/// </summary>
		public void WriteData(int val) => PortWrite(val);

		/// <summary>
		/// Generalized bus alias - read from the active register
		/// </summary>
		public int ReadData() => PortRead();

		/// <summary>
		/// Optional I/O port A input callback (null on platforms that do not use it)
		/// </summary>
		public Func<byte> PortAInput;

		/// <summary>
		/// Optional I/O port B input callback (null on platforms that do not use it)
		/// </summary>
		public Func<byte> PortBInput;

		/// <summary>
		/// Port A direction (true = input)
		/// </summary>
		public bool PortAIsInput = true;

		/// <summary>
		/// Port B direction (true = input)
		/// </summary>
		public bool PortBIsInput = true;

		/// <summary>
		/// Used for snapshot generation
		/// </summary>
		public int[] ExportRegisters()
		{
			return _registers;
		}

		/// <summary>
		/// Resets the PSG
		/// </summary>
		public void Reset()
		{
			// all registers clear to 0 (datasheet reset)
			for (int i = 0; i < 16; i++)
				_registers[i] = 0;
			_activeRegister = 0;

			// seed the 17-bit noise LFSR (the chip powers up in a non-zero state); clear the generator,
			// counter and output state, and recompute the derived state from the cleared registers so the
			// chip is in a consistent, hardware-plausible reset (the old build left most of this stale).
			_noiseSeed = 0x1FFFF;
			_bitA = _bitB = _bitC = _bitN = 0;
			_eState = 0; _eDirection = 0;
			_countA = _countB = _countC = _countE = _countN = 0;
			_lastL = _lastR = 0;

			_dividerA = _dividerB = _dividerC = 0;   // tone periods 0
			_dividerE = 0;                            // envelope period 0
			_dividerN = 0;                            // noise period 0
			_bit0 = _bit1 = _bit2 = _bit3 = _bit4 = _bit5 = 0; // reg7 = 0 -> all channels enabled (active low)
			_eMaskA = _eMaskB = _eMaskC = 0;
			_vA = _vB = _vC = 1;                      // reg8-10 = 0 -> amplitude index 1 -> AYVolumes[1] = 0 (silent)
			PortAIsInput = PortBIsInput = true;
		}

		/// <summary>
		/// Reads the value from the currently selected register
		/// </summary>
		public int PortRead()
		{
			// I/O ports: a present, input-configured port with a host callback returns the pin state.
			if (_activeRegister == 14 && HasPortA && PortAInput != null)
			{
				return PortAIsInput ? PortAInput() : (byte)(PortAInput() & _registers[14]);
			}

			if (_activeRegister == 15 && HasPortB && PortBInput != null)
			{
				return PortBIsInput ? PortBInput() : (byte)(PortBInput() & _registers[15]);
			}

			// Register read-back: the YM2149 returns the raw written value; the AY parts read undefined bits
			// back as 0 (the stored value is already masked on write).
			return _variant == Variant.YM2149 ? _rawWrite[_activeRegister] : _registers[_activeRegister];
		}

		/// <summary>
		/// Writes to the currently selected register
		/// </summary>
		public void PortWrite(int value)
		{
			if (_activeRegister >= 0x10)
				return;

			_rawWrite[_activeRegister] = value & 0xFF; // kept for YM2149 register read-back (unmasked)

			byte val = (byte)value;
			if (_activeRegister is 1 or 3 or 5 or 13) val &= 0x0F;
			else if (_activeRegister is 6 or 8 or 9 or 10) val &= 0x1F;
			if (_activeRegister != 13 && _registers[_activeRegister] == val)
				return;

			_registers[_activeRegister] = val;

			switch (_activeRegister)
			{
				// Channel A (Combined Pitch)
				// (not written to directly)
				case 0:
				case 1:
					_dividerA = _registers[AY_A_FINE] | (_registers[AY_A_COARSE] << 8);
					break;
				// Channel B (Combined Pitch)
				// (not written to directly)
				case 2:
				case 3:
					_dividerB = _registers[AY_B_FINE] | (_registers[AY_B_COARSE] << 8);
					break;
				// Channel C (Combined Pitch)
				// (not written to directly)
				case 4:
				case 5:
					_dividerC = _registers[AY_C_FINE] | (_registers[AY_C_COARSE] << 8);
					break;
				// Noise Pitch
				case 6:
					_dividerN = val * 2;
					break;
				// Mixer
				case 7:
					_bit0 = 0 - ((val >> 0) & 1);
					_bit1 = 0 - ((val >> 1) & 1);
					_bit2 = 0 - ((val >> 2) & 1);
					_bit3 = 0 - ((val >> 3) & 1);
					_bit4 = 0 - ((val >> 4) & 1);
					_bit5 = 0 - ((val >> 5) & 1);
					PortAIsInput = (val & 0x40) == 0;
					PortBIsInput = (val & 0x80) == 0;
					break;
				// Channel Volumes
				case 8:
					_eMaskA = (val & 0x10) != 0 ? -1 : 0;
					_vA = ((val & 0x0F) * 2 + 1) & ~_eMaskA;
					break;
				case 9:
					_eMaskB = (val & 0x10) != 0 ? -1 : 0;
					_vB = ((val & 0x0F) * 2 + 1) & ~_eMaskB;
					break;
				case 10:
					_eMaskC = (val & 0x10) != 0 ? -1 : 0;
					_vC = ((val & 0x0F) * 2 + 1) & ~_eMaskC;
					break;
				// Envelope (Combined Duration)
				// (not written to directly)
				case 11:
				case 12:
					_dividerE = _registers[AY_E_FINE] | (_registers[AY_E_COARSE] << 8);
					break;
				// Envelope Shape
				case 13:
					// reset the envelope counter
					_countE = 0;

					if ((_registers[AY_E_SHAPE] & 4) != 0)
					{
						// attack
						_eState = 0;
						_eDirection = 1;
					}
					else
					{
						// decay
						_eState = 31;
						_eDirection = -1;
					}
					break;
				case 14:
					// IO Port - not implemented
					break;
			}
		}

		/// <summary>
		/// Start of frame
		/// </summary>
		public void StartFrame()
		{
			_blipClock = 0;
			BufferUpdate(0);
		}

		/// <summary>
		/// End of frame
		/// </summary>
		public void EndFrame()
		{
			BufferUpdate(_tStatesPerFrame);
		}

		/// <summary>
		/// Updates the audiobuffer based on the current frame t-state
		/// </summary>
		public void UpdateSound(int frameCycle)
		{
			BufferUpdate(frameCycle);
		}

		/// <summary>
		/// Register indicies
		/// </summary>
		private const int AY_A_FINE = 0;
		private const int AY_A_COARSE = 1;
		private const int AY_B_FINE = 2;
		private const int AY_B_COARSE = 3;
		private const int AY_C_FINE = 4;
		private const int AY_C_COARSE = 5;
		private const int AY_NOISEPITCH = 6;
		private const int AY_MIXER = 7;
		private const int AY_A_VOL = 8;
		private const int AY_B_VOL = 9;
		private const int AY_C_VOL = 10;
		private const int AY_E_FINE = 11;
		private const int AY_E_COARSE = 12;
		private const int AY_E_SHAPE = 13;
		private const int AY_PORT_A = 14;
		private const int AY_PORT_B = 15;

		/// <summary>
		/// The register array
		/// </summary>
		/*
            The AY-3-8910/8912 contains 16 internal registers as follows:

            Register    Function	                Range
            0	        Channel A fine pitch	    8-bit (0-255)
            1	        Channel A course pitch	    4-bit (0-15)
            2	        Channel B fine pitch	    8-bit (0-255)
            3	        Channel B course pitch	    4-bit (0-15)
            4	        Channel C fine pitch	    8-bit (0-255)
            5	        Channel C course pitch	    4-bit (0-15)
            6	        Noise pitch	                5-bit (0-31)
            7	        Mixer	                    8-bit (see below)
            8	        Channel A volume	        4-bit (0-15, see below)
            9	        Channel B volume	        4-bit (0-15, see below)
            10	        Channel C volume	        4-bit (0-15, see below)
            11	        Envelope fine duration	    8-bit (0-255)
            12	        Envelope course duration	8-bit (0-255)
            13	        Envelope shape	            4-bit (0-15)
            14	        I/O port A	                8-bit (0-255)
            15	        I/O port B	                8-bit (0-255) (Not present on the AY-3-8912)

            * The volume registers (8, 9 and 10) contain a 4-bit setting but if bit 5 is set then that channel uses the
                envelope defined by register 13 and ignores its volume setting.
            * The mixer (register 7) is made up of the following bits (low=enabled):

            Bit:        7	    6	    5	    4	    3	    2	    1	    0
            Register:   I/O	    I/O	    Noise	Noise	Noise	Tone	Tone	Tone
            Channel:    B       A	    C	    B	    A	    C	    B	    A

            The AY-3-8912 ignores bit 7 of this register.
        */
		private int[] _registers = new int[16];

		/// <summary>
		/// The last raw (unmasked) byte written to each register, used only for YM2149 register read-back.
		/// </summary>
		private int[] _rawWrite = new int[16];

		/// <summary>
		/// The currently selected register
		/// </summary>
		private byte _activeRegister;

		/// <summary>
		/// Channel generator state
		/// </summary>
		private int _bitA;
		private int _bitB;
		private int _bitC;

		/// <summary>
		/// Envelope state
		/// </summary>
		private int _eState;

		/// <summary>
		/// Envelope direction
		/// </summary>
		private int _eDirection;

		/// <summary>
		/// Noise seed
		/// </summary>
		private int _noiseSeed;

		/// <summary>
		/// Mixer state
		/// </summary>
		private int _bit0;
		private int _bit1;
		private int _bit2;
		private int _bit3;
		private int _bit4;
		private int _bit5;

		/// <summary>
		/// Noise generator state
		/// </summary>
		private int _bitN;

		/// <summary>
		/// Envelope masks
		/// </summary>
		private int _eMaskA;
		private int _eMaskB;
		private int _eMaskC;

		/// <summary>
		/// Amplitudes
		/// </summary>
		private int _vA;
		private int _vB;
		private int _vC;

		/// <summary>
		/// Channel gen counters
		/// </summary>
		private int _countA;
		private int _countB;
		private int _countC;

		/// <summary>
		/// Envelope gen counter
		/// </summary>
		private int _countE;

		/// <summary>
		/// Noise gen counter
		/// </summary>
		private int _countN;

		/// <summary>
		/// Channel gen dividers
		/// </summary>
		private int _dividerA;
		private int _dividerB;
		private int _dividerC;

		/// <summary>
		///  Envelope gen divider
		/// </summary>
		private int _dividerE;

		/// <summary>
		/// Noise gen divider
		/// </summary>
		private int _dividerN;

		/// <summary>
		/// Panning table list
		/// </summary>
		private static readonly List<uint[]> PanTabs = new List<uint[]>
		{
            // MONO
            new uint[] { 50,50, 50,50, 50,50 },
            // ABC
            new uint[] { 100,10,  66,66,   10,100 },
            // ACB
            new uint[] { 100,10,  10,100,  66,66 },
            // BAC
            new uint[] { 66,66,   100,10,  10,100 },
            // BCA
            new uint[] { 10,100,  100,10,  66,66 },
            // CAB
            new uint[] { 66,66,   10,100,  100,10 },
            // CBA
            new uint[] { 10,100,  66,66,   100,10 }
		};

		/// <summary>
		/// The currently selected panning configuration
		/// </summary>
		private PanConfig _currentPanTab = PanConfig.ABC;

		/// <summary>
		/// The current volume
		/// </summary>
		private int _volume = 75;

		/// <summary>
		/// Volume tables state
		/// </summary>
		private uint[][] _volumeTables;

		/// <summary>
		/// Volume table to be used
		/// </summary>
		private static readonly uint[] AYVolumes =
		{
			0x0000,0x0000,0x0340,0x0340,0x04C0,0x04C0,0x06F2,0x06F2,
			0x0A44,0x0A44,0x0F13,0x0F13,0x1510,0x1510,0x227E,0x227E,
			0x289F,0x289F,0x414E,0x414E,0x5B21,0x5B21,0x7258,0x7258,
			0x905E,0x905E,0xB550,0xB550,0xD7A0,0xD7A0,0xFFFF,0xFFFF,
		};

		/// <summary>
		/// The YM2149's 32-level (5-bit) envelope DAC, ~1.5 dB per step (the AY-3-8910 uses the 16-level table
		/// above, i.e. every other step, ~3 dB). Built from the logarithmic model (full scale = 0xFFFF); a
		/// 4-bit fixed volume still lands on the odd indices, so it stays 16-level as on hardware.
		/// </summary>
		private static readonly uint[] YMVolumes = BuildYmVolumes();

		private static uint[] BuildYmVolumes()
		{
			var t = new uint[32];
			for (int n = 1; n < 32; n++)
				t[n] = (uint)System.Math.Round(65535.0 * System.Math.Pow(10.0, -1.5 * (31 - n) / 20.0));
			return t; // t[0] = 0 (silent)
		}

		/// <summary>
		/// Forces an update of the volume tables
		/// </summary>
		private void UpdateVolume()
		{
			int upperFloor = 40000;
			var inc = (0xFFFF - upperFloor) / 100;

			var vol = inc * _volume; // ((ulong)0xFFFF * (ulong)_volume / 100UL) - 20000 ;
			var amps = _variant == Variant.YM2149 ? YMVolumes : AYVolumes;
			_volumeTables = new uint[6][];

			// parent array
			for (int j = 0; j < _volumeTables.Length; j++)
			{
				_volumeTables[j] = new uint[32];

				// child array
				for (int i = 0; i < _volumeTables[j].Length; i++)
				{
					_volumeTables[j][i] = (uint)(
						(PanTabs[(int)_currentPanTab][j] * amps[i] * vol) /
						(3 * 65535 * 100));
				}
			}
		}

		/// <summary>
		/// Initializes timing information for the frame
		/// </summary>
		private void InitTiming(int sampleRate, int frameTactCount, int chipClockHz, int hostClockHz)
		{
			_sampleRate = sampleRate;
			_tStatesPerFrame = frameTactCount;
			ChipClockHz = chipClockHz;

			// The AY tone/noise generators run at the chip base-tick rate = clock/8 (the /16 prescaler with a
			// toggling output). Derive the per-frame base-tick and output-sample counts from the real frame
			// duration (frameTactCount / hostClockHz) so pitch and speed are correct for any clock/machine.
			double frameSeconds = frameTactCount / (double)hostClockHz;
			// YM2149 /SEL low divides the input clock by 2 before the /8 base-tick prescaler.
			_baseTickRate = chipClockHz / (HalfClock ? 16.0 : 8.0);
			_baseTicksPerFrame = (int)System.Math.Round(_baseTickRate * frameSeconds);
			_samplesPerFrame = (int)System.Math.Round(sampleRate * frameSeconds);

			int cap = _samplesPerFrame + 64;
			_audioBuffer = new short[cap * 2];

			_blipL?.Dispose();
			_blipR?.Dispose();
			_blipL = new BlipBuffer(cap);
			_blipR = new BlipBuffer(cap);
			_blipL.SetRates(_baseTickRate, _sampleRate);
			_blipR.SetRates(_baseTickRate, _sampleRate);
			_lastL = _lastR = 0;
			_blipClock = 0;
		}

		/// <summary>
		/// Updates the audiobuffer based on the current frame t-state
		/// </summary>
		private void BufferUpdate(int cycle)
		{
			if (cycle > _tStatesPerFrame)
				cycle = _tStatesPerFrame;

			int targetTick = (_baseTicksPerFrame * cycle) / _tStatesPerFrame;

			while (_blipClock < targetTick)
			{
				if (++_countA >= _dividerA) { _countA = 0; _bitA ^= -1; }
				if (++_countB >= _dividerB) { _countB = 0; _bitB ^= -1; }
				if (++_countC >= _dividerC) { _countC = 0; _bitC ^= -1; }
				if (++_countN >= _dividerN)
				{
					_countN = 0;
					_noiseSeed = (_noiseSeed * 2 + 1) ^ (((_noiseSeed >> 16) ^ (_noiseSeed >> 13)) & 1);
					_bitN = 0 - ((_noiseSeed >> 16) & 1);
				}
				if (++_countE >= _dividerE)
				{
					_countE = 0;
					_eState += _eDirection;
					if ((_eState & ~31) != 0)
					{
						var sh = _registers[AY_E_SHAPE];
						if (sh is <= 7 or 9 or 15) _eState = _eDirection = 0;
						else if (sh is 8 or 12) _eState &= 31;
						else if (sh is 10 or 14) { _eDirection = -_eDirection; _eState += _eDirection; }
						else { _eState = 31; _eDirection = 0; }
					}
				}

				var mixA = ((_eMaskA & _eState) | _vA) & ((_bitA | _bit0) & (_bitN | _bit3));
				var mixB = ((_eMaskB & _eState) | _vB) & ((_bitB | _bit1) & (_bitN | _bit4));
				var mixC = ((_eMaskC & _eState) | _vC) & ((_bitC | _bit2) & (_bitN | _bit5));

				int l = (int)(_volumeTables[0][mixA] + _volumeTables[2][mixB] + _volumeTables[4][mixC]);
				int r = (int)(_volumeTables[1][mixA] + _volumeTables[3][mixB] + _volumeTables[5][mixC]);

				if (l != _lastL) { _blipL.AddDelta((uint)_blipClock, l - _lastL); _lastL = l; }
				if (r != _lastR) { _blipR.AddDelta((uint)_blipClock, r - _lastR); _lastR = r; }

				_blipClock++;
			}
		}

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
				throw new InvalidOperationException("Only Sync mode is supported.");
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			_blipL.EndFrame((uint)_blipClock);
			_blipR.EndFrame((uint)_blipClock);
			_blipL.Clear();
			_blipR.Clear();
			_blipClock = 0;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			_blipL.EndFrame((uint)_blipClock);
			_blipR.EndFrame((uint)_blipClock);
			int avail = _blipL.SamplesAvailable();
			_blipL.ReadSamplesLeft(_audioBuffer, avail);
			_blipR.ReadSamplesRight(_audioBuffer, avail);
			// return the actual clock-derived count; the host mixer resamples each source to its target
			nsamp = avail;
			samples = _audioBuffer;
			_blipClock = 0;
		}

		public int nullDump = 0;

		/// <summary>
		/// Releases the unmanaged BlipBuffer instances
		/// </summary>
		public void Dispose()
		{
			_blipL?.Dispose();
			_blipR?.Dispose();
			_blipL = null;
			_blipR = null;
		}

		/// <summary>
		/// State serialization
		/// </summary>
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("PSG-AY");

			ser.Sync(nameof(_tStatesPerFrame), ref _tStatesPerFrame);
			ser.Sync(nameof(_sampleRate), ref _sampleRate);
			ser.Sync(nameof(_samplesPerFrame), ref _samplesPerFrame);

			ser.Sync(nameof(_lastL), ref _lastL);
			ser.Sync(nameof(_lastR), ref _lastR);
			ser.Sync(nameof(_blipClock), ref _blipClock);

			ser.Sync(nameof(_registers), ref _registers, false);
			ser.Sync(nameof(_rawWrite), ref _rawWrite, false);
			ser.Sync(nameof(_activeRegister), ref _activeRegister);
			ser.Sync(nameof(_bitA), ref _bitA);
			ser.Sync(nameof(_bitB), ref _bitB);
			ser.Sync(nameof(_bitC), ref _bitC);
			ser.Sync(nameof(_eState), ref _eState);
			ser.Sync(nameof(_eDirection), ref _eDirection);
			ser.Sync(nameof(_noiseSeed), ref _noiseSeed);
			ser.Sync(nameof(_bit0), ref _bit0);
			ser.Sync(nameof(_bit1), ref _bit1);
			ser.Sync(nameof(_bit2), ref _bit2);
			ser.Sync(nameof(_bit3), ref _bit3);
			ser.Sync(nameof(_bit4), ref _bit4);
			ser.Sync(nameof(_bit5), ref _bit5);
			ser.Sync(nameof(PortAIsInput), ref PortAIsInput);
			ser.Sync(nameof(PortBIsInput), ref PortBIsInput);
			ser.Sync(nameof(_bitN), ref _bitN);
			ser.Sync(nameof(_eMaskA), ref _eMaskA);
			ser.Sync(nameof(_eMaskB), ref _eMaskB);
			ser.Sync(nameof(_eMaskC), ref _eMaskC);
			ser.Sync(nameof(_vA), ref _vA);
			ser.Sync(nameof(_vB), ref _vB);
			ser.Sync(nameof(_vC), ref _vC);
			ser.Sync(nameof(_countA), ref _countA);
			ser.Sync(nameof(_countB), ref _countB);
			ser.Sync(nameof(_countC), ref _countC);
			ser.Sync(nameof(_countE), ref _countE);
			ser.Sync(nameof(_countN), ref _countN);
			ser.Sync(nameof(_dividerA), ref _dividerA);
			ser.Sync(nameof(_dividerB), ref _dividerB);
			ser.Sync(nameof(_dividerC), ref _dividerC);
			ser.Sync(nameof(_dividerE), ref _dividerE);
			ser.Sync(nameof(_dividerN), ref _dividerN);
			ser.SyncEnum(nameof(_currentPanTab), ref _currentPanTab);
			ser.Sync(nameof(_volume), ref nullDump);

			for (int i = 0; i < 6; i++)
			{
				ser.Sync("volTable" + i, ref _volumeTables[i], false);
			}

			ser.EndSection();
		}
	}
}
