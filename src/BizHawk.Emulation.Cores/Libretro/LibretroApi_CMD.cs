namespace BizHawk.Emulation.Cores.Libretro
{
	unsafe partial class LibretroApi
	{
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

		public void CMD_Run()
		{
			Message(eMessage.CMD_Run);
			WaitForCMD();
		}

		public void CMD_SetEnvironment()
		{
			Message(eMessage.CMD_SetEnvironment);
			WaitForCMD();
		}

		public bool CMD_LoadNoGame()
		{
			Message(eMessage.CMD_LoadNoGame);
			WaitForCMD();
			return true;
		}

		public bool CMD_LoadPath(string path)
		{
			SetAscii(BufId.Param0, path, ()=> {
				Message(eMessage.CMD_LoadPath);
				WaitForCMD();
			});
			return true;
		}

		public bool CMD_LoadData(byte[] data, string id)
		{
			SetAscii(BufId.Param0, id, () =>
			{
				SetBytes(BufId.Param1, data, () =>
				{
					Message(eMessage.CMD_LoadData);
					WaitForCMD();
				});
			});
			return true;
		}

		public uint CMD_UpdateSerializeSize()
		{
			Message(eMessage.CMD_UpdateSerializeSize);
			WaitForCMD();
			return (uint)comm->env.retro_serialize_size;
		}

		public bool CMD_Serialize(byte[] data)
		{
			bool ret = false;
			SetBytes(BufId.Param0, data, () =>
			{
				Message(eMessage.CMD_Serialize);
				WaitForCMD();
				ret = comm->GetBoolValue();
			});
			return ret;
		}

		public bool CMD_Unserialize(byte[] data)
		{
			bool ret = false;
			SetBytes(BufId.Param0, data, () =>
			{
				Message(eMessage.CMD_Unserialize);
				WaitForCMD();
				ret = comm->GetBoolValue();
			});
			return ret;
		}

	}
}