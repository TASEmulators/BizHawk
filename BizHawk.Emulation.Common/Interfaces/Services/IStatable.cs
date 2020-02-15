using System.IO;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Binary save and load state only without any trappings.  At the moment, an emulator core should not implement this directly
	/// </summary>
	public interface IBinaryStateable
	{
		void SaveStateBinary(BinaryWriter writer);
		void LoadStateBinary(BinaryReader reader);
	}

	/// <summary>
	/// This service manages the logic of sending and receiving savestates from the core
	/// If this service is available, client apps will expose features for making savestates and that utilize savestates (such as rewind))
	/// If unavailable these options will not be exposed
	/// Additionally many tools depend on savestates such as TAStudio, these will only be available if this service is implemented
	/// </summary>
	public interface IStatable : IBinaryStateable, IEmulatorService
	{
		void SaveStateText(TextWriter writer);
		void LoadStateText(TextReader reader);

		/// <summary>
		/// save state binary to a byte buffer
		/// </summary>
		/// <returns>you may NOT modify this.  if you call SaveStateBinary() again with the same core, the old data MAY be overwritten.</returns>
		byte[] SaveStateBinary();
	}
}
