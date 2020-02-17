using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// Represents a beeper/buzzer device
	/// </summary>
	public interface IBeeperDevice
	{
		/// <summary>
		/// Initialization
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
