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

		public void QUERY_set_trace_callback(snes_trace_t callback)
		{
			//TODO
			//this.traceCallback = callback;
			//WritePipeMessage(eMessage.eMessage_QUERY_enable_trace);
			//bwPipe.Write(callback != null);
			//bwPipe.Flush();
		}
		public void QUERY_set_scanlineStart(snes_scanlineStart_t scanlineStart)
		{
			//TODO
			//this.scanlineStart = scanlineStart;
			//WritePipeMessage(eMessage.eMessage_QUERY_enable_scanline);
			//bwPipe.Write(scanlineStart != null);
			//bwPipe.Flush();
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
			//TODO
			//WritePipeMessage(eMessage.eMessage_QUERY_set_cdl);
			//if (cdl == null)
			//{
			//  for(int i=0;i<4*2;i++)
			//    WritePipePointer(IntPtr.Zero);
			//}
			//else
			//{
			//  WritePipePointer(cdl.GetPin("CARTROM"),false);
			//  bwPipe.Write(cdl["CARTROM"].Length);

			//  if (cdl.Has("CARTRAM"))
			//  {
			//    WritePipePointer(cdl.GetPin("CARTRAM"), false);
			//    bwPipe.Write(cdl["CARTRAM"].Length);
			//  }
			//  else
			//  {
			//    WritePipePointer(IntPtr.Zero);
			//    WritePipePointer(IntPtr.Zero);
			//  }
				
			//  WritePipePointer(cdl.GetPin("WRAM"));
			//  bwPipe.Write(cdl["WRAM"].Length);
				
			//  WritePipePointer(cdl.GetPin("APURAM"), false);
			//  bwPipe.Write(cdl["APURAM"].Length);
			//  bwPipe.Flush();
			//}
		}
		
	}
}
