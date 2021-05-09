using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class BsnesApi
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

		private string QUERY_MemoryNameForId(SNES_MEMORY id)
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
			_sharedMemoryBlocks.TryGetValue(name, out var ret);
			return (byte*)ret;
		}

		// unused but supposedly to be used in the graphics debugger code. make that work pls ty
		public void QUERY_set_backdropColor(int backdropColor)
		{
			using (_exe.EnterExit())
			{
				_comm->value = (uint)backdropColor;
				_core.Message(eMessage.eMessage_QUERY_set_backdropColor);
			}
		}
	}
}
