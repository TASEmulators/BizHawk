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
		/// This IS allowed to return null.
		/// Unfortunately, the core may think differently of a nonexisting (null) saveram vs a 0 size saveram.
		/// Frontend users of the ISaveRam should treat null as nonexisting (and thus not even write the file, so that the "does not exist" condition can be roundtripped and not confused with an empty file)
		/// </summary>
		byte[] CloneSaveRam();

		/// <summary>
		/// store new SaveRAM to the emu core. the data should be the same size as the return from ReadSaveRam()
		/// </summary>
		void StoreSaveRam(byte[] data);

		/// <summary>
		/// Gets a value indicating whether or not SaveRAM has been modified since the last save
		/// TODO: This is not the best interface. What defines a "save"? I suppose a Clone(), right? at least specify that here.
		/// Clone() should probably take an option that says whether to clear the dirty flag.
		/// And anyway, cores might not know if they can even track a functional dirty flag -- we should convey that fact somehow
		/// (reminder: do that with flags, so we don't have to change the interface 10000 times)
		/// Dirty SaveRAM can in principle be determined by the frontend in that case, but it could possibly be too slow for the file menu dropdown or other things
		/// </summary>
		bool SaveRamModified { get; }
	}
}
