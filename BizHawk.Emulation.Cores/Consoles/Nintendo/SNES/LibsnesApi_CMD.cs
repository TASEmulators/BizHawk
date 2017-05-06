using System;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		public bool CMD_serialize(IntPtr data, int size)
		{
			comm->buf[0] = (uint)data.ToInt32();
			comm->buf_size[0] = size;
			Message(eMessage.eMessage_CMD_serialize);
			WaitForCMD();
			bool ret = comm->GetBool();
			return ret;
		}

		void WaitForCMD()
		{
			for (; ; )
			{
				if (comm->status == eStatus.eStatus_Idle)
					break;
				if (Handle_SIG(comm->reason)) continue;
				if (Handle_BRK(comm->reason)) continue;
			}
		}

		public bool CMD_unserialize(IntPtr data, int size)
		{
			comm->buf[0] = (uint)data.ToInt32();
			comm->buf_size[0] = size;
			Message(eMessage.eMessage_CMD_unserialize);
			WaitForCMD();
			bool ret = comm->GetBool();
			return ret;
		}

		public void CMD_init()
		{
			Message(eMessage.eMessage_CMD_init);
			WaitForCMD();
		}
		public void CMD_power()
		{
			Message(eMessage.eMessage_CMD_power);
			WaitForCMD();
		}
		public void CMD_reset()
		{
			Message(eMessage.eMessage_CMD_reset);
			WaitForCMD();
		}

		public void CMD_run()
		{
			Message(eMessage.eMessage_CMD_run);
			WaitForCMD();
		}

		public bool CMD_load_cartridge_super_game_boy(string rom_xml, byte[] rom_data, uint rom_size, byte[] dmg_data)
		{
			SetAscii(0, rom_xml ?? "", () =>
				SetBytes(1, rom_data, () =>
					SetBytes(2, dmg_data, () =>
					{
						Message(eMessage.eMessage_CMD_load_cartridge_sgb);
						WaitForCMD();
					})
				)
			);
			return comm->GetBool();
		}

		public bool CMD_load_cartridge_normal(byte[] rom_xml, byte[] rom_data)
		{
			//why don't we need this for the other loads? I dont know, our XML handling is really confusing
			string xml = rom_xml == null ? null : System.Text.Encoding.ASCII.GetString(rom_xml);

			SetAscii(0, xml ?? "", () =>
				SetBytes(1, rom_data, () =>
				{
					Message(eMessage.eMessage_CMD_load_cartridge_normal);
					WaitForCMD();
				})
			);
			return comm->GetBool();
		}

		public void CMD_term()
		{
			Message(eMessage.eMessage_CMD_term);
			WaitForCMD();
		}
		public void CMD_unload_cartridge()
		{
			Message(eMessage.eMessage_CMD_unload_cartridge);
			WaitForCMD();
		}

	}
}