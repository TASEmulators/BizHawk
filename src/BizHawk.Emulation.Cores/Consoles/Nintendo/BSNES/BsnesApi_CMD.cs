using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Cores.Libretro;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class BsnesApi
	{
		private void WaitForCMD()
		{
			using (_exe.EnterExit())
			{
				for (;;)
				{
					if (_comm->status == eStatus.eStatus_Idle)
						break;
					if (Handle_SIG(_comm->reason)) continue;
					// if (Handle_BRK(_comm->reason)) continue;
				}
			}
		}

		public void CMD_init(ENTROPY entropy)
		{
			using (_exe.EnterExit())
			{
				_comm->value = (uint) entropy;
				_core.Message(eMessage.eMessage_CMD_init);
				WaitForCMD();
			}
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

		public bool CMD_load_cartridge_super_game_boy(string base_rom_path, byte[] rom_data, byte[] sgb_rom_data)
		{
			using (_exe.EnterExit())
			{
				SetAscii(0, base_rom_path ?? "", () =>
					SetBytes(1, rom_data, () =>
						SetBytes(2, sgb_rom_data, () =>
						{
							_core.Message(eMessage.eMessage_CMD_load_cartridge_sgb);
							WaitForCMD();
						})
					)
				);
				return _comm->GetBool();
			}
		}

		public bool CMD_load_cartridge_normal(string base_rom_path, byte[] rom_data)
		{
			using (_exe.EnterExit())
			{
				//why don't we need this for the other loads? I don't know, our XML handling is really confusing
				// string xml = rom_xml == null ? null : Encoding.ASCII.GetString(rom_xml);

				SetAscii(0, base_rom_path ?? "", () =>
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
