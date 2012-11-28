using BizHawk.Emulation.Computers.Commodore64.MOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Media
{
	public static class PRG
	{
		static public void Load(MOSPLA pla, byte[] prgFile)
		{
			uint length = (uint)prgFile.Length;
			if (length > 2)
			{
				ushort addr = (ushort)(prgFile[0] | (prgFile[1] << 8));
				uint offset = 2;
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
}
