using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components
{
	public interface IPCEngineSoundDebuggable : ISpecializedEmulatorService
	{
		public interface ChannelData
		{
			bool DDA { get; }

			bool Enabled { get; }

			ushort Frequency { get; }

			bool NoiseChannel { get; }

			byte Volume { get; }

			short[] CloneWaveform();
		}

		ReadOnlySpan<ChannelData> GetPSGChannelData();

		void SetChannelMuted(int channelIndex, bool newIsMuted);
	}
}
