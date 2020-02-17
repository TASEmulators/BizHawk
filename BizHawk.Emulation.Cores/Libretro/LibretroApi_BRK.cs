namespace BizHawk.Emulation.Cores.Libretro
{
	unsafe partial class LibretroApi
	{
		bool Handle_BRK(eMessage msg)
		{
			switch (msg)
			{
				default:
					return false;

			} //switch(msg)

			Message(eMessage.Resume);
			return true;
		}
	}
}