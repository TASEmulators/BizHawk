using System.IO;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CPCHawk: Core Class
	/// * IStatable *
	/// </summary>
	public partial class AmstradCPC
	{
		private void SyncState(Serializer ser)
		{
			byte[] core = null;
			if (ser.IsWriter)
			{
				var ms = new MemoryStream();
				ms.Close();
				core = ms.ToArray();
			}

			if (ser.IsWriter)
			{
				ser.SyncEnum(nameof(_machineType), ref _machineType);

				_cpu.SyncState(ser);
				ser.BeginSection(nameof(AmstradCPC));
				_machine.SyncState(ser);
				ser.Sync("Frame", ref _machine.FrameCount);
				ser.Sync("LagCount", ref _lagCount);
				ser.Sync("IsLag", ref _isLag);
				ser.EndSection();
			}

			if (ser.IsReader)
			{
				var tmpM = _machineType;
				ser.SyncEnum(nameof(_machineType), ref _machineType);
				if (tmpM != _machineType && _machineType.ToString() != "72")
				{
					string msg = "SAVESTATE FAILED TO LOAD!!\n\n";
					msg += "Current Configuration: " + tmpM.ToString();
					msg += "\n";
					msg += "Saved Configuration:    " + _machineType.ToString();
					msg += "\n\n";
					msg += "If you wish to load this SaveState ensure that you have the correct machine configuration selected, reboot the core, then try again.";
					CoreComm.ShowMessage(msg);
					_machineType = tmpM;
				}
				else
				{
					_cpu.SyncState(ser);
					ser.BeginSection(nameof(AmstradCPC));
					_machine.SyncState(ser);
					ser.Sync("Frame", ref _machine.FrameCount);
					ser.Sync("LagCount", ref _lagCount);
					ser.Sync("IsLag", ref _isLag);
					ser.EndSection();

					SyncAllByteArrayDomains();
				}
			}
		}
	}
}
