using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Represents a PSG device (in this case an AY-3-891x)
	/// </summary>
	public interface IPSG : ISoundProvider, IPortIODevice
	{
		/// <summary>
		/// Initlization routine
		/// </summary>
		void Init(int sampleRate, int tStatesPerFrame);

		/// <summary>
		/// Activates a register
		/// </summary>
		int SelectedRegister { get; set; }

		int[] ExportRegisters();

		/// <summary>
		/// Writes to the PSG
		/// </summary>
		void PortWrite(int value);

		/// <summary>
		/// Reads from the PSG
		/// </summary>
		int PortRead();


		/// <summary>
		/// Resets the PSG
		/// </summary>
		void Reset();

		/// <summary>
		/// The volume of the AY chip
		/// </summary>
		int Volume { get; set; }

		/// <summary>
		/// Called at the start of a frame
		/// </summary>
		void StartFrame();

		/// <summary>
		/// called at the end of a frame
		/// </summary>
		void EndFrame();

		/// <summary>
		/// Updates the sound based on number of frame cycles
		/// </summary>
		void UpdateSound(int frameCycle);

		/// <summary>
		/// IStatable serialization
		/// </summary>
		void SyncState(Serializer ser);
	}
}
