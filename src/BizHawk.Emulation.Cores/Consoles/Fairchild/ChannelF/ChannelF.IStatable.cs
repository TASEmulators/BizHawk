using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		private void SyncState(Serializer ser)
		{
			ser.BeginSection("ChannelF");
			ser.Sync(nameof(VRAM), ref VRAM, false);
			ser.Sync(nameof(latch_colour), ref latch_colour);
			ser.Sync(nameof(latch_x), ref latch_x);
			ser.Sync(nameof(latch_y), ref latch_y);

			ser.Sync(nameof(FrameClock), ref FrameClock);
			ser.Sync(nameof(_frame), ref _frame);
			ser.Sync(nameof(ClockPerFrame), ref ClockPerFrame);
			ser.Sync(nameof(_isLag), ref _isLag);
			ser.Sync(nameof(_lagCount), ref _lagCount);

			ser.Sync(nameof(tone), ref tone);
			ser.Sync(nameof(sampleBuffer), ref sampleBuffer, false);
			ser.Sync(nameof(toneBuffer), ref toneBuffer, false);
			ser.Sync(nameof(samplesPerFrame), ref samplesPerFrame);
			ser.Sync(nameof(cyclesPerSample), ref cyclesPerSample);
			ser.Sync(nameof(amplitude), ref amplitude);
			ser.Sync(nameof(rampCounter), ref rampCounter);
			ser.Sync(nameof(currTone), ref currTone);
			ser.Sync(nameof(samplePosition), ref samplePosition);

			ser.Sync(nameof(StateConsole), ref StateConsole, false);
			ser.Sync(nameof(StateRight), ref StateRight, false);
			ser.Sync(nameof(StateLeft), ref StateLeft, false);

			//ser.Sync(nameof(ControllersEnabled), ref ControllersEnabled);
			CPU.SyncState(ser);
			Cartridge.SyncState(ser);
			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}

			/*

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
				ser.BeginSection(nameof(ChannelF));
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
					ser.BeginSection(nameof(ChannelF));
					_machine.SyncState(ser);
					ser.Sync("Frame", ref _machine.FrameCount);
					ser.Sync("LagCount", ref _lagCount);
					ser.Sync("IsLag", ref _isLag);
					ser.EndSection();

					SyncAllByteArrayDomains();
				}
			}
			*/
		}
	}
}
