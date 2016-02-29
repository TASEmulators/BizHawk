using System.IO;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Savestate handling methods
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
