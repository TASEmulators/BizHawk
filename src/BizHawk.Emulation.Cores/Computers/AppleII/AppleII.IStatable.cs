using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : ITextStatable
	{
		public void SaveStateText(TextWriter writer)
		{
			SyncState(new AppleSerializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(new AppleSerializer(reader));
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			SyncState(new AppleSerializer(writer));
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			SyncState(new AppleSerializer(reader));
		}

		private void SyncState(AppleSerializer ser)
		{
			int version = 2;
			ser.BeginSection(nameof(AppleII));
			ser.Sync(nameof(version), ref version);
			ser.Sync("Frame", ref _frame);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("PrevDiskPressed", ref _prevPressed);
			ser.Sync("NextDiskPressed", ref _nextPressed);
			ser.Sync("CurrentDisk", ref _currentDisk);
			ser.Sync("WhiteAppleDown", ref Keyboard.WhiteAppleDown);
			ser.Sync("BlackAppleDown", ref Keyboard.BlackAppleDown);

			ser.BeginSection("Events");
			_machine.Events.Sync(ser);
			ser.EndSection();

			ser.BeginSection("Cpu");
			_machine.Cpu.Sync(ser);
			ser.EndSection();

			ser.BeginSection("Video");
			_machine.Video.Sync(ser);
			ser.EndSection();

			ser.BeginSection("Memory");
			_machine.Memory.Sync(ser);
			ser.EndSection();

			ser.BeginSection("NoSlotClock");
			_machine.NoSlotClock.Sync(ser);
			ser.EndSection();

			ser.BeginSection("DiskIIController");
			_machine.DiskIIController.Sync(ser);
			ser.EndSection();

			ser.EndSection();
		}

		public class AppleSerializer : Serializer, IComponentSerializer
		{
			public AppleSerializer(BinaryReader br) : base(br)
			{
			}

			public AppleSerializer(BinaryWriter bw) : base(bw)
			{
			}

			public AppleSerializer(TextReader tr) : base(tr)
			{
			}

			public AppleSerializer(TextWriter tw) : base(tw)
			{
			}
		}
	}
}
