using System;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		public string QUERY_library_id()
		{
			WritePipeMessage(eMessage.eMessage_QUERY_library_id);
			return ReadPipeString();
		}

		public uint QUERY_library_revision_major()
		{
			WritePipeMessage(eMessage.eMessage_QUERY_library_revision_major);
			return brPipe.ReadUInt32();
		}

		public uint QUERY_library_revision_minor()
		{
			WritePipeMessage(eMessage.eMessage_QUERY_library_revision_minor);
			return brPipe.ReadUInt32();
		}

		public SNES_REGION QUERY_get_region()
		{
			WritePipeMessage(eMessage.eMessage_QUERY_get_region);
			return (SNES_REGION)brPipe.ReadByte();
		}

		public SNES_MAPPER QUERY_get_mapper()
		{
			WritePipeMessage(eMessage.eMessage_QUERY_get_mapper);
			return (SNES_MAPPER)brPipe.ReadByte();
		}

		public int QUERY_get_memory_size(SNES_MEMORY id)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_get_memory_size);
			bwPipe.Write((int)id);
			bwPipe.Flush();
			return brPipe.ReadInt32();
		}

		string QUERY_MemoryNameForId(SNES_MEMORY id)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_GetMemoryIdName);
			bwPipe.Write((uint)id);
			bwPipe.Flush();
			return ReadPipeString();
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
			WritePipeMessage(eMessage.eMessage_QUERY_peek);
			bwPipe.Write((uint)id);
			bwPipe.Write(addr);
			bwPipe.Flush();
			return brPipe.ReadByte();
		}
		public void QUERY_poke(SNES_MEMORY id, uint addr, byte val)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_poke);
			bwPipe.Write((uint)id);
			bwPipe.Write(addr);
			bwPipe.Write(val);
			bwPipe.Flush();
		}

		public int QUERY_serialize_size()
		{
			for (; ; )
			{
				WritePipeMessage(eMessage.eMessage_QUERY_serialize_size);
				int ret = brPipe.ReadInt32();
				if (ret > 100)
				{
					return ret;
				}
			}
		}


		int QUERY_poll_message()
		{
			WritePipeMessage(eMessage.eMessage_QUERY_poll_message);
			return brPipe.ReadInt32();
		}

		public bool QUERY_HasMessage { get { return QUERY_poll_message() != -1; } }


		public string QUERY_DequeueMessage()
		{
			WritePipeMessage(eMessage.eMessage_QUERY_dequeue_message);
			return ReadPipeString();
		}


		public void QUERY_set_color_lut(IntPtr colors)
		{
			int len = 4 * 16 * 32768;
			byte[] buf = new byte[len];
			Marshal.Copy(colors, buf, 0, len);

			WritePipeMessage(eMessage.eMessage_QUERY_set_color_lut);
			WritePipeBlob(buf);
		}

		public void QUERY_set_state_hook_exec(bool state)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_state_hook_exec);
			bwPipe.Write(state);
		}

		public void QUERY_set_state_hook_read(bool state)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_state_hook_read);
			bwPipe.Write(state);
		}

		public void QUERY_set_state_hook_write(bool state)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_state_hook_write);
			bwPipe.Write(state);
		}

		public void QUERY_set_trace_callback(snes_trace_t callback)
		{
			this.traceCallback = callback;
			WritePipeMessage(eMessage.eMessage_QUERY_enable_trace);
			bwPipe.Write(callback != null);
			bwPipe.Flush();
		}
		public void QUERY_set_scanlineStart(snes_scanlineStart_t scanlineStart)
		{
			this.scanlineStart = scanlineStart;
			WritePipeMessage(eMessage.eMessage_QUERY_enable_scanline);
			bwPipe.Write(scanlineStart != null);
			bwPipe.Flush();
		}
		public void QUERY_set_audio_sample(snes_audio_sample_t audio_sample)
		{
			this.audio_sample = audio_sample;
			WritePipeMessage(eMessage.eMessage_QUERY_enable_audio);
			bwPipe.Write(audio_sample != null);
			bwPipe.Flush();
		}

		public void QUERY_set_layer_enable(int layer, int priority, bool enable)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_set_layer_enable);
			bwPipe.Write(layer);
			bwPipe.Write(priority);
			bwPipe.Write(enable);
			bwPipe.Flush();
		}

		public void QUERY_set_backdropColor(int backdropColor)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_set_backdropColor);
			bwPipe.Write(backdropColor);
			bwPipe.Flush();
		}
		
		public int QUERY_peek_logical_register(SNES_REG reg)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_peek_logical_register);
			bwPipe.Write((int)reg);
			bwPipe.Flush();
			return brPipe.ReadInt32();
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

		public unsafe void QUERY_peek_cpu_regs(out CpuRegs ret)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_peek_cpu_regs);
			//bwPipe.Flush();
			byte[] temp = new byte[CpuRegs.SIZEOF];
			brPipe.Read(temp, 0, CpuRegs.SIZEOF);
			fixed(CpuRegs* ptr = &ret)
				Marshal.Copy(temp, 0, new IntPtr(ptr), CpuRegs.SIZEOF);
		}

		public void QUERY_set_cdl(ICodeDataLog cdl)
		{
			WritePipeMessage(eMessage.eMessage_QUERY_set_cdl);
			if (cdl == null)
			{
				for(int i=0;i<4*2;i++)
					WritePipePointer(IntPtr.Zero);
			}
			else
			{
				WritePipePointer(cdl.GetPin("CARTROM"),false);
				bwPipe.Write(cdl["CARTROM"].Length);

				if (cdl.Has("CARTRAM"))
				{
					WritePipePointer(cdl.GetPin("CARTRAM"), false);
					bwPipe.Write(cdl["CARTRAM"].Length);
				}
				else
				{
					WritePipePointer(IntPtr.Zero);
					WritePipePointer(IntPtr.Zero);
				}
				
				WritePipePointer(cdl.GetPin("WRAM"));
				bwPipe.Write(cdl["WRAM"].Length);
				
				WritePipePointer(cdl.GetPin("APURAM"), false);
				bwPipe.Write(cdl["APURAM"].Length);
				bwPipe.Flush();
			}
		}
		
	}
}
