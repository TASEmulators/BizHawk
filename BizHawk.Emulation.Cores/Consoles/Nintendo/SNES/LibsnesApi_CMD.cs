using System;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		public bool CMD_serialize(IntPtr data, int size)
		{
			WritePipeMessage(eMessage.eMessage_CMD_serialize);
			bwPipe.Write(size);
			bwPipe.Write(0); //mapped memory location to serialize to
			bwPipe.Flush();
			WaitForCompletion(); //serialize/unserialize can cause traces to get called (because serialize can cause execution?)
			bool ret = brPipe.ReadInt32() != 0;
			if (ret)
			{
				CopyMemory(data.ToPointer(), mmvaPtr, (ulong)size);
			}
			return ret;
		}

		public bool CMD_unserialize(IntPtr data, int size)
		{
			WritePipeMessage(eMessage.eMessage_CMD_unserialize);
			CopyMemory(mmvaPtr, data.ToPointer(), (ulong)size);
			bwPipe.Write(size);
			bwPipe.Write(0); //mapped memory location to serialize from
			bwPipe.Flush();
			WaitForCompletion(); //serialize/unserialize can cause traces to get called (because serialize can cause execution?)
			bool ret = brPipe.ReadInt32() != 0;
			return ret;
		}

		public void CMD_init()
		{
			WritePipeMessage(eMessage.eMessage_CMD_init);
			WaitForCompletion();
		}
		public void CMD_power()
		{
			WritePipeMessage(eMessage.eMessage_CMD_power);
			WaitForCompletion();
		}
		public void CMD_reset()
		{
			WritePipeMessage(eMessage.eMessage_CMD_reset);
			WaitForCompletion();
		}

		/// <summary>
		/// This is a high-level run command. It runs for one frame (right now) and blocks until the frame is done.
		/// If any BRK is received, it will be handled before returning from this function.
		/// </summary>
		public void CMD_run()
		{
			WritePipeMessage(eMessage.eMessage_CMD_run);
			WaitForCompletion();
		}

		public bool CMD_load_cartridge_super_game_boy(string rom_xml, byte[] rom_data, uint rom_size, string dmg_xml, byte[] dmg_data, uint dmg_size)
		{
			WritePipeMessage(eMessage.eMessage_CMD_load_cartridge_super_game_boy);
			WritePipeString(rom_xml ?? "");
			WritePipeBlob(rom_data);
			WritePipeString(rom_xml ?? "");
			WritePipeBlob(dmg_data);
			//not a very obvious order.. because we do tons of work immediately after the last param goes down and need to answer messages
			WaitForCompletion();
			bool ret = brPipe.ReadInt32() != 0;
			return ret;
		}

		public bool CMD_load_cartridge_normal(byte[] rom_xml, byte[] rom_data)
		{
			WritePipeMessage(eMessage.eMessage_CMD_load_cartridge_normal);
			WritePipeBlob(rom_xml ?? new byte[0]);
			WritePipeBlob(rom_data ?? new byte[0]);
			//not a very obvious order.. because we do tons of work immediately after the last param goes down and need to answer messages
			WaitForCompletion();
			bool ret = brPipe.ReadInt32() != 0;
			return ret;
		}

		public void CMD_term()
		{
			WritePipeMessage(eMessage.eMessage_CMD_term);
			WaitForCompletion();
		}
		public void CMD_unload_cartridge()
		{
			WritePipeMessage(eMessage.eMessage_CMD_unload_cartridge);
			WaitForCompletion();
		}

	}
}