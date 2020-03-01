using System;

namespace BizHawk.Emulation.Common
{
	public static class Metaspu
	{
		public static ISynchronizingAudioBuffer MetaspuConstruct(ESynchMethod method)
		{
			return method switch
			{
				ESynchMethod.Vecna => new VecnaSynchronizer(),
				ESynchMethod.Nitsuja => new NitsujaSynchronizer(),
				_ => new NitsujaSynchronizer()
			};
		}
	}

	public enum ESynchMethod
	{
		Nitsuja, // nitsuja's
		Vecna // vecna
	}
}