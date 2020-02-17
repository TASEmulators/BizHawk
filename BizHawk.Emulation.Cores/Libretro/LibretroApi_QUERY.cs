using System;

namespace BizHawk.Emulation.Cores.Libretro
{
	unsafe partial class LibretroApi
	{
		public Tuple<IntPtr, ulong> QUERY_GetMemory(RETRO_MEMORY mem)
		{
			comm->value = (uint)mem;
			Message(eMessage.QUERY_GetMemory);
			return Tuple.Create(new IntPtr((long)comm->buf[(int)BufId.Param0]), comm->buf_size[(int)BufId.Param0]);
		}
	}
}