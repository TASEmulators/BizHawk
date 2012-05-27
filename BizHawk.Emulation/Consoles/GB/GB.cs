using System;
using BizHawk.Emulation.CPUs.Z80GB;

/*
This Game Boy core was written using Imran Nazar's "GameBoy Emulation in
Javascript" series (http://imrannazar.com/GameBoy-Emulation-in-JavaScript) and
contains several comments from the articles.
*/
namespace BizHawk.Emulation.Consoles.GB
{
	public partial class GB/* : IEmulator, IVideoProvider */
	{
		private Z80 CPU;

		public GB(GameInfo game, byte[] rom, bool skipBIOS)
		{
			inBIOS = !skipBIOS;
			HardReset();
		}

		public void HardReset()
		{
			CPU = new CPUs.Z80GB.Z80();
			CPU.ReadMemory = ReadMemory;
			CPU.WriteMemory = WriteMemory;
		}
	}
}
