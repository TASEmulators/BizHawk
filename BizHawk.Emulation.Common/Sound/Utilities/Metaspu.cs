namespace BizHawk.Emulation.Common
{
	public static class Metaspu
	{
		public static ISynchronizingAudioBuffer MetaspuConstruct(ESynchMethod method)
		{
			return new VecnaSynchronizer();
		}
	}

	public enum ESynchMethod
	{
		Vecna // vecna
	}
}