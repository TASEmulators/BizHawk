using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public static class Prg
	{
		public static void Load(Chip90611401 pla, byte[] prgFile)
		{
			var length = prgFile.Length;
		    if (length <= 2)
		    {
		        return;
		    }

		    var addr = prgFile[0] | (prgFile[1] << 8);
		    var offset = 2;
		    unchecked
		    {
		        while (offset < length)
		        {
		            pla.Write(addr, prgFile[offset]);
		            offset++;
		            addr++;
		        }
		    }
		}
	}
}
