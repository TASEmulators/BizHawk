using System;

namespace BizHawk.Emulation.Common
{
	public static class Metaspu
	{
		public static ISynchronizingAudioBuffer MetaspuConstruct(ESynchMethod method)
		{
			switch (method)
			{
				case ESynchMethod.Nitsuja:
					return new NitsujaSynchronizer();
				case ESynchMethod.Vecna:
					return new VecnaSynchronizer();
				default:
					return new NitsujaSynchronizer();
			}
		}
	}

	public enum ESynchMethod
	{
		Nitsuja, // nitsuja's
		Vecna // vecna
	}
}