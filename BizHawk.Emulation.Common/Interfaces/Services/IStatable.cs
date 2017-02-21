using System.IO;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service manages the logic of sending and receiving savestates from the core
	/// If this service is available, client apps will expose features for making savestates and that utilize savestates (such as rewind))
	/// If unavailable these options will not be exposed
	/// Additionally many tools depend on savestates such as TAStudio, these will only be available if this service is implemented
	/// </summary>
	public interface IStatable : IEmulatorService
	{
		/// <summary>
		/// true if the core would rather give a binary savestate than a text one.  both must function regardless
		/// </summary>
		bool BinarySaveStatesPreferred { get; }

		void SaveStateText(TextWriter writer);
		void LoadStateText(TextReader reader);

		void SaveStateBinary(BinaryWriter writer);
		void LoadStateBinary(BinaryReader reader);

		/// <summary>
		/// save state binary to a byte buffer
		/// </summary>
		/// <returns>you may NOT modify this.  if you call SaveStateBinary() again with the same core, the old data MAY be overwritten.</returns>
		byte[] SaveStateBinary();
	}
}
