using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components
{
	[CLSCompliant(false)]
	public interface IPCEngineSoundDebuggable : ISpecializedEmulatorService
	{
#pragma warning disable CA1715 // breaks IInterface convention
		public interface ChannelData
#pragma warning restore CA1715
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
