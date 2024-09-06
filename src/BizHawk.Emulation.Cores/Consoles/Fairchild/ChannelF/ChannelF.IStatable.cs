using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		private void SyncState(Serializer ser)
		{
			ser.BeginSection("ChannelF");
			ser.Sync(nameof(VRAM), ref VRAM, false);
			ser.Sync(nameof(_latch_colour), ref _latch_colour);
			ser.Sync(nameof(_latch_x), ref _latch_x);
			ser.Sync(nameof(_latch_y), ref _latch_y);
			ser.Sync(nameof(_pixelClocksRemaining), ref _pixelClocksRemaining);

			ser.Sync(nameof(FrameClock), ref FrameClock);
			ser.Sync(nameof(_frame), ref _frame);
			ser.Sync(nameof(_isLag), ref _isLag);
			ser.Sync(nameof(_lagCount), ref _lagCount);

			ser.Sync(nameof(_tone), ref _tone);
			ser.Sync(nameof(_sampleBuffer), ref _sampleBuffer, false);
			ser.Sync(nameof(_filteredSampleBuffer), ref _filteredSampleBuffer, false);
			ser.Sync(nameof(_toneBuffer), ref _toneBuffer, false);
			ser.Sync(nameof(_samplesPerFrame), ref _samplesPerFrame);
			ser.Sync(nameof(_cyclesPerSample), ref _cyclesPerSample);
			ser.Sync(nameof(_amplitude), ref _amplitude);
			ser.Sync(nameof(_rampCounter), ref _rampCounter);
			ser.Sync(nameof(_currTone), ref _currTone);
			ser.Sync(nameof(_samplePosition), ref _samplePosition);

			ser.Sync(nameof(StateConsole), ref StateConsole, false);
			ser.Sync(nameof(StateRight), ref StateRight, false);
			ser.Sync(nameof(StateLeft), ref StateLeft, false);

			ser.Sync(nameof(OutputLatch), ref OutputLatch, false);
			ser.Sync(nameof(LS368Enable), ref LS368Enable);

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
