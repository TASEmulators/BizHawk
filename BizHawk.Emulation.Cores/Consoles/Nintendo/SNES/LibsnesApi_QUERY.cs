using System;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		public int QUERY_get_memory_size(SNES_MEMORY id)
		{
			comm->value = (uint)id;
			Message(eMessage.eMessage_QUERY_get_memory_size);
			return (int)comm->value;
		}

		string QUERY_MemoryNameForId(SNES_MEMORY id)
		{
			comm->id = (uint)id;
			Message(eMessage.eMessage_QUERY_GetMemoryIdName);
			return comm->GetAscii();
		}

		public byte* QUERY_get_memory_data(SNES_MEMORY id)
		{
			string name = QUERY_MemoryNameForId(id);
			if (!SharedMemoryBlocks.ContainsKey(name)) return null;
			var smb = SharedMemoryBlocks[name];
			return (byte*)smb.Ptr;
		}

		public byte QUERY_peek(SNES_MEMORY id, uint addr)
		{
			comm->id = (uint)id;
			comm->addr = addr;
			Message(eMessage.eMessage_QUERY_peek);
			return (byte)comm->value;
		}
		public void QUERY_poke(SNES_MEMORY id, uint addr, byte val)
		{
			comm->id = (uint)id;
			comm->addr = addr;
			comm->value = (byte)val;
			Message(eMessage.eMessage_QUERY_poke);
		}

		public int QUERY_serialize_size()
		{
			for (; ; )
			{
				Message(eMessage.eMessage_QUERY_serialize_size);
				int ret = (int)comm->size;
				if (ret > 100)
				{
					return ret;
				}
				else Console.WriteLine("WHY????????");
			}
		}



		public void QUERY_set_color_lut(IntPtr colors)
		{
			comm->ptr = colors.ToPointer();
			Message(eMessage.eMessage_QUERY_set_color_lut);
		}

		public void QUERY_set_state_hook_exec(bool state)
		{
			comm->value = state ? 1u : 0u;
			Message(eMessage.eMessage_QUERY_state_hook_exec);
		}

		public void QUERY_set_state_hook_read(bool state)
		{
			comm->value = state ? 1u : 0u;
			Message(eMessage.eMessage_QUERY_state_hook_read);
		}

		public void QUERY_set_state_hook_write(bool state)
		{
			comm->value = state ? 1u : 0u;
			Message(eMessage.eMessage_QUERY_state_hook_write);
		}

		public void QUERY_set_trace_callback(int mask, snes_trace_t callback)
		{
			this.traceCallback = callback;
			comm->value = (uint)mask;
			Message(eMessage.eMessage_QUERY_enable_trace);
		}
		public void QUERY_set_scanlineStart(snes_scanlineStart_t scanlineStart)
		{
			this.scanlineStart = scanlineStart;
			comm->value = (scanlineStart != null) ? 1u : 0u;
			Message(eMessage.eMessage_QUERY_enable_scanline);
		}
		public void QUERY_set_audio_sample(snes_audio_sample_t audio_sample)
		{
			this.audio_sample = audio_sample;
			comm->value = (audio_sample!=null) ? 1u : 0u;
			Message(eMessage.eMessage_QUERY_enable_audio);
		}

		public void QUERY_set_layer_enable()
		{
			Message(eMessage.eMessage_QUERY_set_layer_enable);
		}

		public void QUERY_set_backdropColor(int backdropColor)
		{
			comm->value = (uint)backdropColor;
			Message(eMessage.eMessage_QUERY_set_backdropColor);
		}
		
		public int QUERY_peek_logical_register(SNES_REG reg)
		{
			comm->id = (uint)reg;
			Message(eMessage.eMessage_QUERY_peek_logical_register);
			return (int)comm->value;
		}

		public unsafe void QUERY_peek_cpu_regs(out CPURegs ret)
		{
			Message(eMessage.eMessage_QUERY_peek_cpu_regs);
			ret = comm->cpuregs;
		}

		public void QUERY_set_cdl(ICodeDataLog cdl)
		{
			for (int i = 0; i < 8; i++)
			{
				comm->cdl_ptr[i] = 0;
				comm->cdl_size[i] = 0;
			}

			if (cdl != null)
			{
				comm->cdl_ptr[0] = cdl.GetPin("CARTROM").ToInt64();
				comm->cdl_size[0] = cdl["CARTROM"].Length;
				if (cdl.Has("CARTRAM"))
				{
					comm->cdl_ptr[1] = cdl.GetPin("CARTRAM").ToInt64();
					comm->cdl_size[1] = cdl["CARTRAM"].Length;
				}

				comm->cdl_ptr[2] = cdl.GetPin("WRAM").ToInt64();
				comm->cdl_size[2] = cdl["WRAM"].Length;

				comm->cdl_ptr[3] = cdl.GetPin("APURAM").ToInt64();
				comm->cdl_size[3] = cdl["APURAM"].Length;

				if (cdl.Has("SGB_CARTROM"))
				{
					comm->cdl_ptr[4] = cdl.GetPin("SGB_CARTROM").ToInt64();
					comm->cdl_size[4] = cdl["SGB_CARTROM"].Length;

					if (cdl.Has("SGB_CARTRAM"))
					{
						comm->cdl_ptr[5] = cdl.GetPin("SGB_CARTRAM").ToInt64();
						comm->cdl_size[5] = cdl["SGB_CARTRAM"].Length;
					}

					comm->cdl_ptr[6] = cdl.GetPin("SGB_WRAM").ToInt64();
					comm->cdl_size[6] = cdl["SGB_WRAM"].Length;

					comm->cdl_ptr[7] = cdl.GetPin("SGB_HRAM").ToInt64();
					comm->cdl_size[7] = cdl["SGB_HRAM"].Length;
				}
			}

			Message(eMessage.eMessage_QUERY_set_cdl);
		}
		
	}
}
