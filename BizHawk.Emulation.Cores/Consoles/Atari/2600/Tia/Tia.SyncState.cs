using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class TIA
	{
		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(TIA));
			_ball.SyncState(ser);
			_hmove.SyncState(ser);
			ser.Sync("hsyncCnt", ref _hsyncCnt);

			// add everything to the state 
			ser.Sync("Bus_State", ref BusState);

			ser.Sync("_ctrlPFDelay", ref _ctrlPFDelay);
			ser.Sync("_ctrlPFVal", ref _ctrlPFVal);
			ser.Sync("PF0_up", ref _pf0Update);
			ser.Sync("PF1_up", ref _pf1Update);
			ser.Sync("PF2_up", ref _pf2Update);
			ser.Sync("PF0_upper", ref _pf0Updater);
			ser.Sync("PF1_upper", ref _pf1Updater);
			ser.Sync("PF2_upper", ref _pf2Updater);
			ser.Sync("PF0_delay", ref _pf0DelayClock);
			ser.Sync("PF1_delay", ref _pf1DelayClock);
			ser.Sync("PF2_delay", ref _pf2DelayClock);
			ser.Sync("PF0_max", ref _pf0MaxDelay);
			ser.Sync("PF1_max", ref _pf1MaxDelay);
			ser.Sync("PF2_max", ref _pf2MaxDelay);

			ser.Sync("Enam0_delay", ref _enam0Delay);
			ser.Sync("Enam1_delay", ref _enam1Delay);
			ser.Sync("Enab_delay", ref _enambDelay);
			ser.Sync("Enam0_val", ref _enam0Val);
			ser.Sync("Enam1_val", ref _enam1Val);
			ser.Sync("Enab_val", ref _enambVal);

			ser.Sync("P0_stuff", ref _p0Stuff);
			ser.Sync("P1_stuff", ref _p1Stuff);
			ser.Sync("M0_stuff", ref _m0Stuff);
			ser.Sync("M1_stuf", ref _m1Stuff);
			ser.Sync("b_stuff", ref _bStuff);
			
			ser.Sync("_hmp0_no_tick", ref _hmp0_no_tick);
			ser.Sync("_hmp1_no_tick", ref _hmp1_no_tick);
			ser.Sync("_hmm0_no_tick", ref _hmm0_no_tick);
			ser.Sync("_hmm1_no_tick", ref _hmm1_no_tick);
			ser.Sync("_hmb_no_tick", ref _hmb_no_tick);

			ser.Sync("hmp0_delay", ref _hmp0Delay);
			ser.Sync("hmp0_val", ref _hmp0Val);
			ser.Sync("hmp1_delay", ref _hmp1Delay);
			ser.Sync("hmp1_val", ref _hmp1Val);
			ser.Sync("hmm0_delay", ref _hmm0Delay);
			ser.Sync("hmm0_val", ref _hmm0Val);
			ser.Sync("hmm1_delay", ref _hmm1Delay);
			ser.Sync("hmm1_val", ref _hmm1Val);
			ser.Sync("hmb_delay", ref _hmbDelay);
			ser.Sync("hmb_val", ref _hmbVal);

			ser.Sync("_nusiz0Delay", ref _nusiz0Delay);
			ser.Sync("_nusiz0Val", ref _nusiz0Val);
			ser.Sync("_nusiz1Delay", ref _nusiz1Delay);
			ser.Sync("_nusiz1Val", ref _nusiz1Val);

			ser.Sync("_hmClrDelay", ref _hmClrDelay);

			ser.Sync("PRG0_delay", ref _prg0Delay);
			ser.Sync("PRG1_delay", ref _prg1Delay);
			ser.Sync("PRG0_val", ref _prg0Val);
			ser.Sync("PRG1_val", ref _prg1Val);

			ser.Sync("Ticks", ref _doTicks);
			ser.Sync(nameof(hmove_cnt_up), ref hmove_cnt_up);

			ser.Sync("VBlankDelay", ref _vblankDelay);
			ser.Sync("VBlankValue", ref _vblankValue);

			// some of these things weren't in the state because they weren't needed if
			// states were always taken at frame boundaries
			ser.Sync("capChargeStart", ref _capChargeStart);
			ser.Sync("capCharging", ref _capCharging);
			ser.Sync("vblankEnabled", ref _vblankEnabled);
			ser.Sync("vsyncEnabled", ref _vsyncEnabled);
			ser.Sync("CurrentScanLine", ref _currentScanLine);
			ser.Sync(nameof(AudioClocks), ref AudioClocks);
			ser.Sync(nameof(New_Frame), ref New_Frame);

			ser.BeginSection("Audio");
			AUD.SyncState(ser);
			ser.EndSection();

			ser.BeginSection("Player0");
			_player0.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Player1");
			_player1.SyncState(ser);
			ser.EndSection();
			_playField.SyncState(ser);
			ser.EndSection();
		}
	}
}
