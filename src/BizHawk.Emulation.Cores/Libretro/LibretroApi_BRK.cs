namespace BizHawk.Emulation.Cores.Libretro
{
	unsafe partial class LibretroApi
	{
		private bool Handle_BRK(eMessage msg)
		{
			switch (msg)
			{
				default:
					return false;

			} //switch(msg)

			// TODO: do we want this ever?
#if false
			Message(eMessage.Resume);
			return true;
#endif
		}
	}
}