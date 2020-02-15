using System.IO;
using BizHawk.Common.BufferExtensions;

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
		/// <summary>
		/// save state binary to a byte buffer
		/// </summary>
		/// <returns>you may NOT modify this.  if you call SaveStateBinary() again with the same core, the old data MAY be overwritten.</returns>
		byte[] SaveStateBinary();
	}

	/// <summary>
	/// Allows a core to opt into text savestates.
	/// If a core does not implement this interface, a default implementation
	/// will be used that doesn't yield anything useful in the states, just binary as text
	/// </summary>
	public interface ITextStatable : IStatable
	{
		void SaveStateText(TextWriter writer);

		void LoadStateText(TextReader reader);
	}

	public static class StatableExtensions
	{
		public static void SaveStateText(this IStatable core, TextWriter writer)
		{
			if (core is ITextStatable textCore)
			{
				textCore.SaveStateText(writer);
			}

			var temp = core.SaveStateBinary();
			temp.SaveAsHexFast(writer);
		}

		public static void LoadStateText(this IStatable core, TextReader reader)
		{
			if (core is ITextStatable textCore)
			{
				textCore.LoadStateText(reader);
			}

			string hex = reader.ReadLine();
			if (hex != null)
			{
				var state = new byte[hex.Length / 2];
				state.ReadFromHexFast(hex);
				using var ms = new MemoryStream(state);
				using var br = new BinaryReader(ms);
				core.LoadStateBinary(br);
			}
		}
	}
}
