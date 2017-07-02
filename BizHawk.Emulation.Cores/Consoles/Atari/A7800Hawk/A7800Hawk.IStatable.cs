using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk : IStatable
	{
		public bool BinarySaveStatesPreferred => true;

		public void SaveStateText(TextWriter writer)
		{
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(new Serializer(br));
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			byte[] core = null;
			if (ser.IsWriter)
			{
				var ms = new MemoryStream();
				ms.Close();
				core = ms.ToArray();
			}
			cpu.SyncState(ser);
			tia.SyncState(ser);
			maria.SyncState(ser);
			m6532.SyncState(ser);

			ser.BeginSection("Atari7800");
			ser.Sync("core", ref core, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			_controllerDeck.SyncState(ser);

			ser.Sync("IsLag", ref A7800_control_register);
			ser.Sync("IsLag", ref _isPAL);

			ser.Sync("IsLag", ref TIA_regs, false);
			ser.Sync("IsLag", ref Maria_regs, false);
			ser.Sync("IsLag", ref RAM, false);
			ser.Sync("IsLag", ref regs_6532, false);
			ser.Sync("IsLag", ref hs_bios_mem, false);

			ser.Sync("IsLag", ref cycle);
			ser.Sync("IsLag", ref cpu_cycle);
			ser.Sync("IsLag", ref cpu_is_haltable);
			ser.Sync("IsLag", ref cpu_is_halted);
			ser.Sync("IsLag", ref cpu_halt_pending);
			ser.Sync("IsLag", ref cpu_resume_pending);

			ser.Sync("IsLag", ref slow_access);


			ser.EndSection();
		}
	}
}
