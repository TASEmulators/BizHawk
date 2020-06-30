using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Represents a beeper/buzzer device
	/// </summary>
	public interface IBeeperDevice
	{
		/// <summary>
		/// Initialisation
		/// </summary>
		void Init(int sampleRate, int tStatesPerFrame);

		/// <summary>
		/// Processes an incoming pulse value and adds it to the blipbuffer
		/// </summary>
		void ProcessPulseValue(bool pulse);

		/// <summary>
		/// State serialization
		/// </summary>
		void SyncState(Serializer ser);
	}
}
