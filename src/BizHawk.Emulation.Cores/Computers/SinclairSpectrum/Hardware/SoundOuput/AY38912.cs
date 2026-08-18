using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// AY-3-8912 Emulated Device (ZX Spectrum adapter)
	///
	/// This is now a THIN ADAPTER around the shared <see cref="AY391x"/> DSP core.
	/// It provides the ZX-bus-specific port decode (0xFFFD / 0xBFFD) and the ZX
	/// settings glue (pan config / volume), delegating all sound generation and
	/// state to the shared core.
	/// </summary>
	public class AY38912 : IPSG
	{
		/// <summary>
		/// The emulated machine (passed in via constructor)
		/// </summary>
		private readonly SpectrumBase _machine;

		/// <summary>
		/// The shared PSG DSP core
		/// </summary>
		private readonly AY391x _core = new AY391x();

		/// <summary>
		/// Main constructor
		/// </summary>
		public AY38912(SpectrumBase machine)
		{
			_machine = machine;
		}

		/// <summary>
		/// Initialises the AY chip
		/// </summary>
		public void Init(int sampleRate, int tStatesPerFrame)
		{
			// The Spectrum AY runs at CPU/2; tStatesPerFrame is measured in CPU T-states. Passing the actual
			// per-model CPU clock makes the AY pitch correct for every model (incl. Pentagon, whose AY is
			// 1.792 MHz, not the 1.7734 MHz the old build hard-coded).
			int cpuClock = _machine.ULADevice.ClockSpeed;
			_core.Init(sampleRate, tStatesPerFrame, cpuClock / 2, cpuClock);
		}

		/// <summary>
		/// |11-- ---- ---- --0-|	-	IN	-	Read value of currently selected register
		/// </summary>
		public bool ReadPort(ushort port, ref int value)
		{
			if (!port.Bit(1))
			{
				if ((port >> 14) == 3)
				{
					// port read is addressing this device
					value = _core.PortRead();
					return true;
				}
			}

			// port read is not addressing this device
			return false;
		}

		/// <summary>
		/// |11-- ---- ---- --0-|	-	OUT	-	Register Select
		/// |10-- ---- ---- --0-|	-	OUT	-	Write value to currently selected register
		/// </summary>
		public bool WritePort(ushort port, int value)
		{
			if (!port.Bit(1))
			{
				if ((port >> 14) == 3)
				{
					// register select
					_core.SelectedRegister = value & 0x0f;
					return true;
				}
				else if ((port >> 14) == 2)
				{
					// Update the audiobuffer based on the current CPU cycle
					// (this process the previous data BEFORE writing to the currently selected register)
					_core.UpdateSound((int)_machine.CurrentFrameCycle);

					// write to register
					_core.PortWrite(value);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// AY mixer panning configuration
		/// </summary>
		public enum AYPanConfig
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
		public AYPanConfig PanningConfiguration
		{
			get => (AYPanConfig)(int)_core.PanningConfiguration;
			set => _core.PanningConfiguration = (AY391x.PanConfig)(int)value;
		}

		/// <summary>
		/// The AY chip output volume
		/// (0 - 100)
		/// </summary>
		public int Volume
		{
			get => _core.Volume;
			set => _core.Volume = value;
		}

		/// <summary>
		/// The currently selected register
		/// </summary>
		public int SelectedRegister
		{
			get => _core.SelectedRegister;
			set => _core.SelectedRegister = value;
		}

		/// <summary>
		/// Used for snapshot generation
		/// </summary>
		public int[] ExportRegisters()
		{
			return _core.ExportRegisters();
		}

		/// <summary>
		/// Resets the PSG
		/// </summary>
		public void Reset()
		{
			_core.Reset();
		}

		/// <summary>
		/// Reads the value from the currently selected register
		/// </summary>
		public int PortRead()
		{
			return _core.PortRead();
		}

		/// <summary>
		/// Writes to the currently selected register
		/// </summary>
		public void PortWrite(int value)
		{
			_core.PortWrite(value);
		}

		/// <summary>
		/// Start of frame
		/// </summary>
		public void StartFrame()
		{
			_core.StartFrame();
		}

		/// <summary>
		/// End of frame
		/// </summary>
		public void EndFrame()
		{
			_core.EndFrame();
		}

		/// <summary>
		/// Updates the audiobuffer based on the current frame t-state
		/// </summary>
		public void UpdateSound(int frameCycle)
		{
			_core.UpdateSound(frameCycle);
		}

		public bool CanProvideAsync => _core.CanProvideAsync;

		public SyncSoundMode SyncMode => _core.SyncMode;

		public void SetSyncMode(SyncSoundMode mode)
		{
			_core.SetSyncMode(mode);
		}

		public void GetSamplesAsync(short[] samples)
		{
			_core.GetSamplesAsync(samples);
		}

		public void DiscardSamples()
		{
			_core.DiscardSamples();
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			_core.GetSamplesSync(out samples, out nsamp);
		}

		/// <summary>
		/// State serialization
		/// </summary>
		public void SyncState(Serializer ser)
		{
			_core.SyncState(ser);

			if (ser.IsReader)
				_core.SetVolumeNoRebuild(_machine.Spectrum.Settings.AYVolume);
		}

		/// <summary>
		/// Releases the shared PSG core (and its unmanaged BlipBuffers). Called from
		/// ZXSpectrum.Dispose via the IPSG (: IDisposable) AYDevice.
		/// </summary>
		public void Dispose()
		{
			_core.Dispose();
		}
	}
}
