using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk : ITextStatable
	{
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
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			cpu.SyncState(ser);
			tia.SyncState(ser);
			maria.SyncState(ser);
			m6532.SyncState(ser);
			mapper.SyncState(ser);
			pokey.SyncState(ser);

			ser.BeginSection("Atari7800");
			ser.Sync("Lag", ref _lagCount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _isLag);
			_controllerDeck.SyncState(ser);

			ser.Sync(nameof(A7800_control_register), ref A7800_control_register);
			ser.Sync(nameof(_isPAL), ref _isPAL);

			ser.Sync(nameof(Maria_regs), ref Maria_regs, false);
			ser.Sync(nameof(RAM), ref RAM, false);
			ser.Sync(nameof(RAM_6532), ref RAM_6532, false);
			ser.Sync(nameof(hs_bios_mem), ref hs_bios_mem, false);

			ser.Sync(nameof(cycle), ref cycle);
			ser.Sync(nameof(cpu_cycle), ref cpu_cycle);
			ser.Sync(nameof(cpu_is_haltable), ref cpu_is_haltable);
			ser.Sync(nameof(cpu_is_halted), ref cpu_is_halted);
			ser.Sync(nameof(cpu_halt_pending), ref cpu_halt_pending);
			ser.Sync(nameof(cpu_resume_pending), ref cpu_resume_pending);

			ser.Sync(nameof(slow_access), ref slow_access);
			ser.Sync(nameof(slow_access), ref slow_countdown);
			ser.Sync("small flag", ref small_flag);
			ser.Sync("pal kara", ref PAL_Kara);
			ser.Sync("Cart RAM", ref cart_RAM);
			ser.Sync(nameof(is_pokey), ref is_pokey);
			ser.Sync(nameof(left_toggle), ref left_toggle);
			ser.Sync(nameof(right_toggle), ref right_toggle);
			ser.Sync(nameof(left_was_pressed), ref left_was_pressed);
			ser.Sync(nameof(right_was_pressed), ref right_was_pressed);

			ser.Sync(nameof(temp_s_tia), ref temp_s_tia);
			ser.Sync(nameof(temp_s_pokey), ref temp_s_pokey);
			ser.Sync(nameof(samp_l), ref samp_l);
			ser.Sync(nameof(samp_c), ref samp_c);
			ser.Sync(nameof(master_audio_clock), ref master_audio_clock);
			ser.Sync(nameof(temp), ref temp);

			ser.EndSection();
		}
	}
}
