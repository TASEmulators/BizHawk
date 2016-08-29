using System;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;
namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate string QUERY_library_id_t();
		public QUERY_library_id_t QUERY_library_id;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint QUERY_library_revision_major_t();
		public QUERY_library_revision_major_t QUERY_library_revision_major;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint QUERY_library_revision_minor_t();
		public QUERY_library_revision_minor_t QUERY_library_revision_minor;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate sbyte QUERY_snes_get_region_t();
		public QUERY_snes_get_region_t QUERY_snes_get_region;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int QUERY_snes_get_memory_size_t(uint which);
		public QUERY_snes_get_memory_size_t QUERY_snes_get_memory_size;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate byte QUERY_peek_t(uint id, uint addr);
		public QUERY_peek_t QUERY_peek_managed;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_poke_t(uint id, uint addr, byte val);
		public QUERY_poke_t QUERY_poke_managed;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_snes_set_layer_enable_t(int layer, int priority, bool enable);
		public QUERY_snes_set_layer_enable_t QUERY_snes_set_layer_enable;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_set_state_hook_exec_t(bool enable);
		public QUERY_set_state_hook_exec_t QUERY_set_state_hook_exec;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_set_state_hook_read_t(bool state);
		public QUERY_set_state_hook_read_t QUERY_set_state_hook_read;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_set_state_hook_write_t(bool state);
		public QUERY_set_state_hook_write_t QUERY_set_state_hook_write;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_enable_trace_t(bool state);
		public QUERY_enable_trace_t QUERY_enable_trace;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_enable_audio_t(bool enable);
		public QUERY_enable_audio_t QUERY_enable_audio;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate string QUERY_get_memory_id_t(uint which);
		public QUERY_get_memory_id_t QUERY_get_memory_id;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_enable_scanline_t(bool enable);
		public QUERY_enable_scanline_t QUERY_enable_scanline;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int QUERY_peek_logical_register_t(int which);
		public QUERY_peek_logical_register_t QUERY_peek_logical_register_managed;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_set_backdropColor_t(int col);
		public QUERY_set_backdropColor_t QUERY_set_backdropColor;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint QUERY_snes_serialize_size_t();
		public QUERY_snes_serialize_size_t QUERY_snes_serialize_size;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_set_color_lut_t(IntPtr blob);
		public QUERY_set_color_lut_t QUERY_set_color_lut;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_peek_cpu_regs_t(CpuRegs* cpuregs);
		public QUERY_peek_cpu_regs_t QUERY_peek_cpu_regs;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void QUERY_set_cdl_t(int i, IntPtr block, int size);
		public QUERY_set_cdl_t managed_QUERY_set_cdl;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate byte QUERY_get_mapper_t();
		public QUERY_get_mapper_t managed_QUERY_get_mapper;

		void InitQueryFunctions()
		{
			instanceDll.Retrieve(out QUERY_library_id, "QUERY_library_id");
			instanceDll.Retrieve(out QUERY_library_revision_major, "QUERY_library_revision_major");
			instanceDll.Retrieve(out QUERY_library_revision_minor, "QUERY_library_revision_minor");
			instanceDll.Retrieve(out QUERY_snes_get_region, "QUERY_snes_get_region");
			instanceDll.Retrieve(out QUERY_snes_get_memory_size, "QUERY_snes_get_memory_size");
			instanceDll.Retrieve(out QUERY_peek_managed, "QUERY_peek");
			instanceDll.Retrieve(out QUERY_poke_managed, "QUERY_poke");
			instanceDll.Retrieve(out QUERY_snes_set_layer_enable, "QUERY_snes_set_layer_enable");
			instanceDll.Retrieve(out QUERY_set_state_hook_exec, "QUERY_set_state_hook_exec");
			instanceDll.Retrieve(out QUERY_set_state_hook_read, "QUERY_set_state_hook_read");
			instanceDll.Retrieve(out QUERY_set_state_hook_write, "QUERY_set_state_hook_write");
			instanceDll.Retrieve(out QUERY_enable_trace, "QUERY_enable_trace");
			instanceDll.Retrieve(out QUERY_enable_audio, "QUERY_enable_audio");
			instanceDll.Retrieve(out QUERY_get_memory_id, "QUERY_get_memory_id");
			instanceDll.Retrieve(out QUERY_enable_scanline, "QUERY_enable_scanline");
			instanceDll.Retrieve(out QUERY_peek_logical_register_managed, "QUERY_peek_logical_register");
			instanceDll.Retrieve(out QUERY_set_backdropColor, "QUERY_set_backdropColor");
			instanceDll.Retrieve(out QUERY_snes_serialize_size, "QUERY_snes_serialize_size");
			instanceDll.Retrieve(out QUERY_set_color_lut, "QUERY_set_color_lut");
			instanceDll.Retrieve(out QUERY_peek_cpu_regs, "QUERY_peek_cpu_regs");
			instanceDll.Retrieve(out managed_QUERY_set_cdl, "QUERY_set_cdl");
			instanceDll.Retrieve(out managed_QUERY_get_mapper, "QUERY_get_mapper");
		}
        
		public SNES_REGION QUERY_get_region()
		{
			return (SNES_REGION)QUERY_snes_get_region();
		}

		public SNES_MAPPER QUERY_get_mapper()
		{
			return (SNES_MAPPER)managed_QUERY_get_mapper();
		}

		public int QUERY_get_memory_size(SNES_MEMORY id)
		{
			return QUERY_snes_get_memory_size((uint)id);
		}

		string QUERY_MemoryNameForId(SNES_MEMORY id)
		{
			return QUERY_get_memory_id((uint)id);
		}

		public byte* QUERY_get_memory_data(SNES_MEMORY id)
		{
			string name = QUERY_MemoryNameForId(id);
			var smb = SharedMemoryBlocks[name];
			return (byte*)smb.Ptr;
		}

		public byte QUERY_peek(SNES_MEMORY id, uint addr)
		{
			return QUERY_peek_managed((uint)id, addr);
		}
		public void QUERY_poke(SNES_MEMORY id, uint addr, byte val)
		{
			QUERY_poke_managed((uint)id, addr, val);
		}


		public int QUERY_serialize_size()
		{
			for (; ; )
			{
				int ret = (int)QUERY_snes_serialize_size();
				if (ret > 100)
				{
					return ret;
				}
			}
		}


		int QUERY_poll_message()
		{
			return -1;
		}

		public bool QUERY_HasMessage { get { return QUERY_poll_message() != -1; } }

		/*
		public string QUERY_DequeueMessage()
		{
			WritePipeMessage(eMessage.eMessage_QUERY_dequeue_message);
			return ReadPipeString();
		}*/
        
        
		public void QUERY_set_trace_callback(snes_trace_t callback)
		{
			QUERY_enable_trace(callback != null);

		}
		public void QUERY_set_scanlineStart(snes_scanlineStart_t scanlineStart)
		{
			QUERY_enable_scanline(scanlineStart != null);
		}
		public void QUERY_set_audio_sample(snes_audio_sample_t audio_sample)
		{
			QUERY_enable_audio(audio_sample != null);
		}

		public void QUERY_set_layer_enable(int layer, int priority, bool enable)
		{
			QUERY_snes_set_layer_enable(layer, priority, enable);
		}

		public int QUERY_peek_logical_register(SNES_REG reg)
		{
			return QUERY_peek_logical_register_managed((int)reg);
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CpuRegs
		{
			public uint pc;
			public ushort a, x, y, z, s, d, vector; //7x
			public byte p, nothing;
			public uint aa, rd;
			public byte sp, dp, db, mdr;
			public const int SIZEOF = 32;
		}


		public void QUERY_set_cdl(CodeDataLog cdl)
		{
			if (cdl == null)
			{

				for (int i = 0; i < 4 * 2; i++)
					managed_QUERY_set_cdl(i, IntPtr.Zero, 0);
			}
			else
			{
				managed_QUERY_set_cdl(0, cdl.GetPin("CARTROM"), cdl["CARTROM"].Length);
                
				if (cdl.Has("CARTRAM"))
				{
					managed_QUERY_set_cdl(1, cdl.GetPin("CARTRAM"), cdl["CARTRAM"].Length);
				}
				else
				{
					managed_QUERY_set_cdl(1, IntPtr.Zero, 0);
				}

				managed_QUERY_set_cdl(2, cdl.GetPin("WRAM"), cdl["WRAM"].Length);

				managed_QUERY_set_cdl(3, cdl.GetPin("APURAM"), cdl["APURAM"].Length);
			}
		}
		
	}
}
