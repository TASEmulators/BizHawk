namespace BizHawk.Emulation.Common
{
	public interface ISaveRam : IEmulator, ICoreService
	{
		/// <summary>
		/// return a copy of the saveram.  editing it won't do you any good unless you later call StoreSaveRam()
		/// </summary>
		byte[] CloneSaveRam();

		/// <summary>
		/// store new saveram to the emu core.  the data should be the same size as the return from ReadSaveRam()
		/// </summary>
		void StoreSaveRam(byte[] data);

		/// <summary>
		/// Whether or not Save ram has been modified since the last save
		/// </summary>
		bool SaveRamModified { get; }
	}
}
