using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : ITextStatable
	{
		public bool AvoidRewind => false;

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
			var oldCurrentDisk = CurrentDisk;
			ser.BeginSection(nameof(AppleII));
			ser.Sync(nameof(version), ref version);
			ser.Sync("Frame", ref _frame);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("PrevDiskPressed", ref _prevPressed);
			ser.Sync("NextDiskPressed", ref _nextPressed);
			ser.Sync("CurrentDisk", ref _currentDisk);
			ser.Sync("WhiteAppleDown", ref Keyboard.WhiteAppleDown);
			ser.Sync("BlackAppleDown", ref Keyboard.BlackAppleDown);
			ser.Sync("ClockTime", ref _clockTime);
			ser.Sync("ClockRemainder", ref _clockRemainder);

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

			ser.BeginSection("Keyboard");
			_machine.Memory.Keyboard.Sync(ser);
			ser.EndSection();

			ser.BeginSection("NoSlotClock");
			_machine.NoSlotClock.Sync(ser);
			ser.EndSection();

			// disk change, we need to swap disks so SyncDelta works later
			if (CurrentDisk != oldCurrentDisk)
			{
				_machine.DiskIIController.Drive1.InsertDisk("junk" + _romSet[CurrentDisk].Extension, (byte[])_romSet[CurrentDisk].Data.Clone(), false);
			}

			ser.BeginSection("DiskIIController");
			_machine.DiskIIController.Sync(ser);
			ser.EndSection();

			ser.BeginSection("InactiveDisks");
			for (var i = 0; i < DiskCount; i++)
			{
				// the current disk is handled in DiskIIController
				if (i != CurrentDisk)
				{
					ser.Sync($"DiskDelta{i}", ref _diskDeltas[i], useNull: true);
				}
			}
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
