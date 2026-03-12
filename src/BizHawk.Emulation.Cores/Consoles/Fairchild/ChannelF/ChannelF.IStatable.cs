using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		private void SyncState(Serializer ser)
		{
			ser.BeginSection("ChannelF");
			ser.Sync(nameof(_vram), ref _vram, false);
			ser.Sync(nameof(_latchColour), ref _latchColour);
			ser.Sync(nameof(_latchX), ref _latchX);
			ser.Sync(nameof(_latchY), ref _latchY);
			ser.Sync(nameof(_pixelClockCounter), ref _pixelClockCounter);
			ser.Sync(nameof(_pixelClocksRemaining), ref _pixelClocksRemaining);

			ser.Sync(nameof(_frameClock), ref _frameClock);
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

			ser.Sync(nameof(_stateConsole), ref _stateConsole, false);
			ser.Sync(nameof(_stateRight), ref _stateRight, false);
			ser.Sync(nameof(_stateLeft), ref _stateLeft, false);

			ser.Sync(nameof(_outputLatch), ref _outputLatch, false);
			ser.Sync(nameof(LS368Enable), ref LS368Enable);

			_cpu.SyncState(ser);
			_cartridge.SyncState(ser);
			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}
	}
}
