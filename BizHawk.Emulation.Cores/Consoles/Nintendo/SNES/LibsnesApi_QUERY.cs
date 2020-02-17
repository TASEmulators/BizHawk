using System;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		public int QUERY_get_memory_size(SNES_MEMORY id)
		{
			using (_exe.EnterExit())
			{
				_comm->value = (uint)id;
				_core.Message(eMessage.eMessage_QUERY_get_memory_size);
				return (int)_comm->value;
			}
		}

		string QUERY_MemoryNameForId(SNES_MEMORY id)
		{
			using (_exe.EnterExit())
			{
				_comm->id = (uint)id;
				_core.Message(eMessage.eMessage_QUERY_GetMemoryIdName);
				return _comm->GetAscii();
			}
		}

		public byte* QUERY_get_memory_data(SNES_MEMORY id)
		{
			string name = QUERY_MemoryNameForId(id);
			IntPtr ret;
			_sharedMemoryBlocks.TryGetValue(name, out ret);
			return (byte*)ret;
		}

		public byte QUERY_peek(SNES_MEMORY id, uint addr)
		{
			using (_exe.EnterExit())
			{
				_comm->id = (uint)id;
				_comm->addr = addr;
				_core.Message(eMessage.eMessage_QUERY_peek);
				return (byte)_comm->value;
			}
		}
		public void QUERY_poke(SNES_MEMORY id, uint addr, byte val)
		{
			using (_exe.EnterExit())
			{
				_comm->id = (uint)id;
				_comm->addr = addr;
				_comm->value = (byte)val;
				_core.Message(eMessage.eMessage_QUERY_poke);
			}
		}

		public void QUERY_set_color_lut(IntPtr colors)
		{
			using (_exe.EnterExit())
			{
				_comm->ptr = colors.ToPointer();
				_core.Message(eMessage.eMessage_QUERY_set_color_lut);
			}
		}

		public void QUERY_set_state_hook_exec(bool state)
		{
			using (_exe.EnterExit())
			{
				_comm->value = state ? 1u : 0u;
				_core.Message(eMessage.eMessage_QUERY_state_hook_exec);
			}
		}

		public void QUERY_set_state_hook_read(bool state)
		{
			using (_exe.EnterExit())
			{
				_comm->value = state ? 1u : 0u;
				_core.Message(eMessage.eMessage_QUERY_state_hook_read);
			}
		}

		public void QUERY_set_state_hook_write(bool state)
		{
			using (_exe.EnterExit())
			{
				_comm->value = state ? 1u : 0u;
				_core.Message(eMessage.eMessage_QUERY_state_hook_write);
			}
		}

		public void QUERY_set_trace_callback(int mask, snes_trace_t callback)
		{
			using (_exe.EnterExit())
			{
				this.traceCallback = callback;
				_comm->value = (uint)mask;
				_core.Message(eMessage.eMessage_QUERY_enable_trace);
			}
		}
		public void QUERY_set_scanlineStart(snes_scanlineStart_t scanlineStart)
		{
			using (_exe.EnterExit())
			{
				this.scanlineStart = scanlineStart;
				_comm->value = (scanlineStart != null) ? 1u : 0u;
				_core.Message(eMessage.eMessage_QUERY_enable_scanline);
			}
		}
		public void QUERY_set_audio_sample(snes_audio_sample_t audio_sample)
		{
			using (_exe.EnterExit())
			{
				this.audio_sample = audio_sample;
				_comm->value = (audio_sample != null) ? 1u : 0u;
				_core.Message(eMessage.eMessage_QUERY_enable_audio);
			}
		}

		public void QUERY_set_layer_enable()
		{
			_core.Message(eMessage.eMessage_QUERY_set_layer_enable);
		}

		public void QUERY_set_backdropColor(int backdropColor)
		{
			using (_exe.EnterExit())
			{
				_comm->value = (uint)backdropColor;
				_core.Message(eMessage.eMessage_QUERY_set_backdropColor);
			}
		}

		public int QUERY_peek_logical_register(SNES_REG reg)
		{
			using (_exe.EnterExit())
			{
				_comm->id = (uint)reg;
				_core.Message(eMessage.eMessage_QUERY_peek_logical_register);
				return (int)_comm->value;
			}
		}

		public unsafe void QUERY_peek_cpu_regs(out CPURegs ret)
		{
			using (_exe.EnterExit())
			{
				_core.Message(eMessage.eMessage_QUERY_peek_cpu_regs);
				ret = _comm->cpuregs;
			}
		}

		public void QUERY_set_cdl(ICodeDataLog cdl)
		{
			using (_exe.EnterExit())
			{
				for (int i = 0; i < 8; i++)
				{
					_comm->cdl_ptr[i] = 0;
					_comm->cdl_size[i] = 0;
				}

				if (cdl != null)
				{
					_comm->cdl_ptr[0] = cdl.GetPin("CARTROM").ToInt64();
					_comm->cdl_size[0] = cdl["CARTROM"].Length;
					if (cdl.Has("CARTRAM"))
					{
						_comm->cdl_ptr[1] = cdl.GetPin("CARTRAM").ToInt64();
						_comm->cdl_size[1] = cdl["CARTRAM"].Length;
					}

					_comm->cdl_ptr[2] = cdl.GetPin("WRAM").ToInt64();
					_comm->cdl_size[2] = cdl["WRAM"].Length;

					_comm->cdl_ptr[3] = cdl.GetPin("APURAM").ToInt64();
					_comm->cdl_size[3] = cdl["APURAM"].Length;

					if (cdl.Has("SGB_CARTROM"))
					{
						_comm->cdl_ptr[4] = cdl.GetPin("SGB_CARTROM").ToInt64();
						_comm->cdl_size[4] = cdl["SGB_CARTROM"].Length;

						if (cdl.Has("SGB_CARTRAM"))
						{
							_comm->cdl_ptr[5] = cdl.GetPin("SGB_CARTRAM").ToInt64();
							_comm->cdl_size[5] = cdl["SGB_CARTRAM"].Length;
						}

						_comm->cdl_ptr[6] = cdl.GetPin("SGB_WRAM").ToInt64();
						_comm->cdl_size[6] = cdl["SGB_WRAM"].Length;

						_comm->cdl_ptr[7] = cdl.GetPin("SGB_HRAM").ToInt64();
						_comm->cdl_size[7] = cdl["SGB_HRAM"].Length;
					}
				}

				_core.Message(eMessage.eMessage_QUERY_set_cdl);
			}
		}
	}
}
