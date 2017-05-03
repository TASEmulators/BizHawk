namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service provides the system by which a client can request SRam data to be stored by the client
	/// SaveRam encompasses things like battery backed ram, memory cards, and data saved to disk drives
	/// If available, save files will be automatically loaded when loading a ROM,
	/// In addition the client will provide features like SRAM-anchored movies, and ways to clear SaveRam
	/// </summary>
	public interface ISaveRam : IEmulatorService
	{
		/// <summary>
		/// Returns a copy of the SaveRAM. Editing it won't do you any good unless you later call StoreSaveRam()
		/// </summary>
		byte[] CloneSaveRam();

		/// <summary>
		/// store new SaveRAM to the emu core. the data should be the same size as the return from ReadSaveRam()
		/// </summary>
		void StoreSaveRam(byte[] data);

		/// <summary>
		/// Gets a value indicating whether or not SaveRAM has been modified since the last save
		/// </summary>
		bool SaveRamModified { get; }
	}
}
