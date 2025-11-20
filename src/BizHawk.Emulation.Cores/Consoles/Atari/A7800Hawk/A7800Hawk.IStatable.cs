using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk
	{
		private void SyncState(Serializer ser)
		{
			ser.BeginSection("Atari7800");

			cpu.SyncState(ser);
			tia.SyncState(ser);
			maria.SyncState(ser);
			m6532.SyncState(ser);
			ser.BeginSection("Mapper");
			mapper.SyncState(ser);
			ser.EndSection();
			pokey.SyncState(ser);

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
			ser.Sync(nameof(_hsram), ref _hsram, false);

			ser.Sync(nameof(cycle), ref cycle);
			ser.Sync(nameof(cpu_cycle), ref cpu_cycle);
			ser.Sync(nameof(cpu_is_haltable), ref cpu_is_haltable);
			ser.Sync(nameof(cpu_is_halted), ref cpu_is_halted);
			ser.Sync(nameof(cpu_halt_pending), ref cpu_halt_pending);
			ser.Sync(nameof(cpu_resume_pending), ref cpu_resume_pending);

			ser.Sync(nameof(p1_state), ref p1_state);
			ser.Sync(nameof(p2_state), ref p2_state);
			ser.Sync(nameof(p1_fire), ref p1_fire);
			ser.Sync(nameof(p2_fire), ref p2_fire);
			ser.Sync(nameof(p1_fire_2x), ref p1_fire_2x);
			ser.Sync(nameof(p2_fire_2x), ref p2_fire_2x);
			ser.Sync(nameof(con_state), ref con_state);
			ser.Sync(nameof(left_toggle), ref left_toggle);
			ser.Sync(nameof(right_toggle), ref right_toggle);
			ser.Sync(nameof(left_was_pressed), ref left_was_pressed);
			ser.Sync(nameof(right_was_pressed), ref right_was_pressed);
			ser.Sync(nameof(p1_is_2button), ref p1_is_2button);
			ser.Sync(nameof(p2_is_2button), ref p2_is_2button);
			ser.Sync(nameof(p1_is_lightgun), ref p1_is_lightgun);
			ser.Sync(nameof(p2_is_lightgun), ref p2_is_lightgun);
			ser.Sync(nameof(p1_lightgun_x), ref p1_lightgun_x);
			ser.Sync(nameof(p1_lightgun_y), ref p1_lightgun_y);
			ser.Sync(nameof(p2_lightgun_x), ref p2_lightgun_x);
			ser.Sync(nameof(p2_lightgun_y), ref p2_lightgun_y);
			ser.Sync(nameof(lg_1_counting_down), ref lg_1_counting_down);
			ser.Sync(nameof(lg_1_counting_down_2), ref lg_1_counting_down_2);
			ser.Sync(nameof(lg_2_counting_down), ref lg_2_counting_down);
			ser.Sync(nameof(lg_2_counting_down_2), ref lg_2_counting_down_2);
			ser.Sync(nameof(lg_1_trigger_hit), ref lg_1_trigger_hit);
			ser.Sync(nameof(lg_2_trigger_hit), ref lg_2_trigger_hit);

			ser.Sync(nameof(slow_access), ref slow_access);
			ser.Sync(nameof(slow_countdown), ref slow_countdown);
			ser.Sync("small flag", ref small_flag);
			ser.Sync("pal kara", ref PAL_Kara);
			ser.Sync("Cart RAM", ref cart_RAM);
			ser.Sync(nameof(is_pokey), ref is_pokey);
			ser.Sync(nameof(is_pokey_450), ref is_pokey_450);


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
