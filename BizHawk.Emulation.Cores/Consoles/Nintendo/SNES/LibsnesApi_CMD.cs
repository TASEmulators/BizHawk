using System;

using BizHawk.Common;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate bool CMD_load_cart_sgb_t(
				string rom_xml_, byte* rom_data, uint rom_length,
				string dmg_xml_, byte* dmg_data, uint dmg_length);
		public CMD_load_cart_sgb_t CMD_load_cart_sgb;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void CMD_run_t();

		/// <summary>
		/// This is a high-level run command. It runs for one frame (right now) and blocks until the frame is done.
		/// If any BRK is received, it will be handled before returning from this function.
		/// </summary>
		public CMD_run_t CMD_run;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void CMD_init_t();
		public CMD_init_t CMD_init;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void CMD_power_t();
		public CMD_power_t CMD_power;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void CMD_reset_t();
		public CMD_reset_t CMD_reset;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void CMD_term_t();
		public CMD_term_t CMD_term;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate bool CMD_load_cartridge_t(IntPtr xml, IntPtr rom, uint size);
		CMD_load_cartridge_t unmanaged_CMD_load_cartridge;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void CMD_unload_cartridge_t();
		public CMD_unload_cartridge_t CMD_unload_cartridge;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate bool CMD_unserialize_t(int size, int destOfs);
		CMD_unserialize_t unmanaged_CMD_unserialize;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate bool CMD_serialize_t(int size, int destOfs);
		CMD_serialize_t unmanaged_CMD_serialize;

		void InitCMDFunctions()
		{
			instanceDll.Retrieve(out CMD_load_cart_sgb, "CMD_load_cart_sgb");
			instanceDll.Retrieve(out CMD_run, "CMD_run");
			instanceDll.Retrieve(out CMD_init, "CMD_init");
			instanceDll.Retrieve(out CMD_power, "CMD_power");
			instanceDll.Retrieve(out CMD_reset, "CMD_reset");
			instanceDll.Retrieve(out CMD_term, "CMD_term");
			instanceDll.Retrieve(out unmanaged_CMD_load_cartridge, "CMD_load_cartridge");
			instanceDll.Retrieve(out CMD_unload_cartridge, "CMD_unload_cartridge");
			instanceDll.Retrieve(out unmanaged_CMD_unserialize, "CMD_unserialize");
			instanceDll.Retrieve(out unmanaged_CMD_serialize, "CMD_serialize");
		}

		public bool CMD_serialize(IntPtr data, int size)
		{
			bool ret = unmanaged_CMD_serialize(size, 0);
			if (ret)
			{
				CopyMemory(data.ToPointer(), mmvaPtr, (ulong)size);
			}
			return ret;
		}

		public bool CMD_unserialize(IntPtr data, int size)
		{
			CopyMemory(mmvaPtr, data.ToPointer(), (ulong)size);
			return unmanaged_CMD_unserialize(size, 0);
		}

		public bool CMD_load_cartridge_super_game_boy(string rom_xml, byte[] rom_data, uint rom_size, string dmg_xml, byte[] dmg_data, uint dmg_size)
		{
			fixed (byte* rom_data_ = rom_data, dmg_data_ = dmg_data)
				return CMD_load_cart_sgb(rom_xml, rom_data_, rom_size, dmg_xml, dmg_data_, dmg_size);
		}

		public bool CMD_load_cartridge_normal(byte[] rom_xml, byte[] rom_data)
		{
			IntPtr xml = IntPtr.Zero;
			if (rom_xml != null)
			{
				xml = Marshal.AllocHGlobal(rom_xml.Length);
				for (int i = 0; i < rom_xml.Length; i++)
					Marshal.WriteByte(xml + i, rom_xml[i]);
			}
			IntPtr rom = Marshal.AllocHGlobal(rom_data.Length);
			for (int i = 0; i < rom_data.Length; i++)
				Marshal.WriteByte(rom + i, rom_data[i]);

			var ret = unmanaged_CMD_load_cartridge(xml, rom, (uint)rom_data.Length);
			Marshal.FreeHGlobal(xml);
			Marshal.FreeHGlobal(rom);
			return ret;
		}

	}
}