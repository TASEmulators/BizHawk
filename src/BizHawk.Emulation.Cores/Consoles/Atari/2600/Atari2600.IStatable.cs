using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600
	{
		private void SyncState(Serializer ser)
		{
			ser.BeginSection("A2600");
			Cpu.SyncState(ser);
			ser.Sync("ram", ref _ram, false);
			ser.Sync("Lag", ref _lagCount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			ser.Sync(nameof(cyc_counter), ref cyc_counter);
			ser.Sync("leftDifficultySwitchPressed", ref _leftDifficultySwitchPressed);
			ser.Sync("rightDifficultySwitchPressed", ref _rightDifficultySwitchPressed);
			ser.Sync("leftDifficultySwitchHeld", ref _leftDifficultySwitchHeld);
			ser.Sync("rightDifficultySwitchHeld", ref _rightDifficultySwitchHeld);
			ser.Sync(nameof(unselect_reset), ref unselect_reset);

			_tia.SyncState(ser);
			_m6532.SyncState(ser);
			ser.BeginSection("Mapper");
			_mapper.SyncState(ser);
			ser.EndSection();
			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}
	}
}
