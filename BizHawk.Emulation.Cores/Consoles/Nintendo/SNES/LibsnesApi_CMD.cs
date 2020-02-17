using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		void WaitForCMD()
		{
			using (_exe.EnterExit())
			{
				for (;;)
				{
					if (_comm->status == eStatus.eStatus_Idle)
						break;
					if (Handle_SIG(_comm->reason)) continue;
					if (Handle_BRK(_comm->reason)) continue;
				}
			}
		}

		public void CMD_init()
		{
			_core.Message(eMessage.eMessage_CMD_init);
			WaitForCMD();
		}
		public void CMD_power()
		{
			_core.Message(eMessage.eMessage_CMD_power);
			WaitForCMD();
		}
		public void CMD_reset()
		{
			_core.Message(eMessage.eMessage_CMD_reset);
			WaitForCMD();
		}

		public void CMD_run()
		{
			_core.Message(eMessage.eMessage_CMD_run);
			WaitForCMD();
		}

		public bool CMD_load_cartridge_super_game_boy(string rom_xml, byte[] rom_data, uint rom_size, byte[] dmg_data)
		{
			using (_exe.EnterExit())
			{
				SetAscii(0, rom_xml ?? "", () =>
					SetBytes(1, rom_data, () =>
						SetBytes(2, dmg_data, () =>
						{
							_core.Message(eMessage.eMessage_CMD_load_cartridge_sgb);
							WaitForCMD();
						})
					)
				);
				return _comm->GetBool();
			}
		}

		public bool CMD_load_cartridge_normal(byte[] rom_xml, byte[] rom_data)
		{
			using (_exe.EnterExit())
			{
				//why don't we need this for the other loads? I dont know, our XML handling is really confusing
				string xml = rom_xml == null ? null : System.Text.Encoding.ASCII.GetString(rom_xml);

				SetAscii(0, xml ?? "", () =>
					SetBytes(1, rom_data, () =>
					{
						_core.Message(eMessage.eMessage_CMD_load_cartridge_normal);
						WaitForCMD();
					})
				);
				return _comm->GetBool();
			}
		}
	}
}