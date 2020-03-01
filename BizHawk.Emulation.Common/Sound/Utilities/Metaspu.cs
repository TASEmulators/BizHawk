namespace BizHawk.Emulation.Common
{
	public static class Metaspu
	{
		public static ISynchronizingAudioBuffer MetaspuConstruct()
		{
			return new VecnaSynchronizer();
		}
	}
}