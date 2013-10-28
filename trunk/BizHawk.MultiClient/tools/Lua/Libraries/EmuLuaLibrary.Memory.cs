using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		#region Memory Library Helpers

		private int M_R_S_LE(int addr, int size)
		{
			return U2S(M_R_U_LE(addr, size), size);
		}

		private uint M_R_U_LE(int addr, int size)
		{
			uint v = 0;
			for (int i = 0; i < size; ++i)
				v |= M_R_U8(addr + i) << 8 * i;
			return v;
		}

		private int M_R_S_BE(int addr, int size)
		{
			return U2S(M_R_U_BE(addr, size), size);
		}

		private uint M_R_U_BE(int addr, int size)
		{
			uint v = 0;
			for (int i = 0; i < size; ++i)
				v |= M_R_U8(addr + i) << 8 * (size - 1 - i);
			return v;
		}

		private void M_W_S_LE(int addr, int v, int size)
		{
			M_W_U_LE(addr, (uint)v, size);
		}

		private void M_W_U_LE(int addr, uint v, int size)
		{
			for (int i = 0; i < size; ++i)
				M_W_U8(addr + i, (v >> (8 * i)) & 0xFF);
		}

		private void M_W_S_BE(int addr, int v, int size)
		{
			M_W_U_BE(addr, (uint)v, size);
		}

		private void M_W_U_BE(int addr, uint v, int size)
		{
			for (int i = 0; i < size; ++i)
				M_W_U8(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF);
		}

		private uint M_R_U8(int addr)
		{
			return Global.Emulator.MemoryDomains[CurrentMemoryDomain].PeekByte(addr);
		}

		private void M_W_U8(int addr, uint v)
		{
			Global.Emulator.MemoryDomains[CurrentMemoryDomain].PokeByte(addr, (byte)v);
		}

		#endregion

		public string memory_getmemorydomainlist()
		{
			return Global.Emulator.MemoryDomains.Aggregate("", (current, t) => current + (t.Name + '\n'));
		}

		public string memory_getcurrentmemorydomain()
		{
			return Global.Emulator.MemoryDomains[CurrentMemoryDomain].Name;
		}

		public int memory_getcurrentmemorydomainsize()
		{
			return Global.Emulator.MemoryDomains[CurrentMemoryDomain].Size;
		}

		public uint memory_readbyte(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U8(addr);
		}

		public float memory_readfloat(object lua_addr, bool bigendian)
		{
			int addr = LuaInt(lua_addr);
			uint val = Global.Emulator.MemoryDomains[CurrentMemoryDomain].PeekDWord(addr, bigendian ? Endian.Big : Endian.Little);

			byte[] bytes = BitConverter.GetBytes(val);
			float _float = BitConverter.ToSingle(bytes, 0);
			return _float;
		}

		public void memory_writebyte(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U8(addr, v);
		}

		public void memory_writefloat(object lua_addr, object lua_v, bool bigendian)
		{
			int addr = LuaInt(lua_addr);
			float dv = (float)(double)lua_v;
			byte[] bytes = BitConverter.GetBytes(dv);
			uint v = BitConverter.ToUInt32(bytes, 0);
			Global.Emulator.MemoryDomains[CurrentMemoryDomain].PokeDWord(addr, v, bigendian ? Endian.Big : Endian.Little);
		}

		public bool memory_usememorydomain(object lua_input)
		{
			if (lua_input.GetType() != typeof(string))
				return false;

			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
			{
				if (Global.Emulator.MemoryDomains[x].Name == lua_input.ToString())
				{
					CurrentMemoryDomain = x;
					return true;
				}
			}

			return false;
		}


		public int memory_read_s8(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return (sbyte)M_R_U8(addr);
		}

		public uint memory_read_u8(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U8(addr);
		}

		public int memory_read_s16_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_LE(addr, 2);
		}

		public int memory_read_s24_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_LE(addr, 3);
		}

		public int memory_read_s32_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_LE(addr, 4);
		}

		public uint memory_read_u16_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_LE(addr, 2);
		}

		public uint memory_read_u24_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_LE(addr, 3);
		}

		public uint memory_read_u32_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_LE(addr, 4);
		}

		public int memory_read_s16_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_BE(addr, 2);
		}

		public int memory_read_s24_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_BE(addr, 3);
		}

		public int memory_read_s32_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_BE(addr, 4);
		}

		public uint memory_read_u16_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_BE(addr, 2);
		}

		public uint memory_read_u24_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_BE(addr, 3);
		}

		public uint memory_read_u32_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_BE(addr, 4);
		}

		public void memory_write_s8(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_U8(addr, (uint)v);
		}

		public void memory_write_u8(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U8(addr, v);
		}

		public void memory_write_s16_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_LE(addr, v, 2);
		}

		public void memory_write_s24_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_LE(addr, v, 3);
		}

		public void memory_write_s32_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_LE(addr, v, 4);
		}

		public void memory_write_u16_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_LE(addr, v, 2);
		}

		public void memory_write_u24_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_LE(addr, v, 3);
		}

		public void memory_write_u32_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_LE(addr, v, 4);
		}

		public void memory_write_s16_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_BE(addr, v, 2);
		}

		public void memory_write_s24_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_BE(addr, v, 3);
		}

		public void memory_write_s32_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_BE(addr, v, 4);
		}

		public void memory_write_u16_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_BE(addr, v, 2);
		}

		public void memory_write_u24_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_BE(addr, v, 3);
		}

		public void memory_write_u32_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_BE(addr, v, 4);
		}
	}
}
