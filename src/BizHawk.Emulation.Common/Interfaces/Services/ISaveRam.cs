namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service provides the system by which a client can request SaveRAM data to be stored by the client
	/// SaveRam encompasses things like battery backed ram, memory cards, and data saved to disk drives
	/// If available, save files will be automatically loaded when loading a ROM,
	/// In addition the client will provide features like SRAM-anchored movies, and ways to clear SaveRam
	/// </summary>
	public interface ISaveRam : IEmulatorService
	{
		/// <summary>
		/// Returns a copy of the SaveRAM. Editing it won't do you any good unless you later call StoreSaveRam()
		/// </summary>
		/// <param name="clearDirty">Whether the saveram should be considered in a clean state after this call for purposes of <see cref="SaveRamModified"/></param>
		byte[] CloneSaveRam(bool clearDirty = true);

		/// <summary>
		/// Store new SaveRAM to the emu core.
		/// </summary>
		/// <exception cref="Exception">The core may throw an exception if the given data is invalid.</exception>
		void StoreSaveRam(byte[] data);

		/// <summary>
		/// Gets a value indicating whether or not SaveRAM has been modified since the last call to either <see cref="StoreSaveRam"/> or <see cref="CloneSaveRam"/> (when passing true).
		/// Cores may choose to always return true or return true for any non-default saveram.
		/// This value should be considered a hint more than an absolute truth.
		/// </summary>
		bool SaveRamModified { get; }
	}
}
