using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public unsafe partial class LibsnesCore : ISaveRam
	{
		public bool SaveRamModified =>
			Api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM) != 0
			|| Api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.SGB_CARTRAM) != 0;

		public byte[] CloneSaveRam()
		{
			byte* buf = Api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			var size = Api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			if (buf == null)
			{
				buf = Api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.SGB_CARTRAM);
				size = Api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.SGB_CARTRAM);
			}

			var ret = new byte[size];
			Marshal.Copy((IntPtr)buf, ret, 0, size);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			byte* buf = Api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			var size = Api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			if (buf == null)
			{
				buf = Api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.SGB_CARTRAM);
				size = Api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.SGB_CARTRAM);
			}

			if (size == 0)
			{
				return;
			}

			if (size != data.Length)
			{
				throw new InvalidOperationException("Somehow, we got a mismatch between saveram size and what bsnes says the saveram size is");
			}

			Marshal.Copy(data, 0, (IntPtr)buf, size);
		}
	}
}
