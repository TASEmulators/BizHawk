using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed partial class Intellivision : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		private DCFilter _dcfilter;

		public IAsyncSoundProvider SoundProvider
		{
			get { return _dcfilter; }
		}

		public ISyncSoundProvider SyncSoundProvider
		{
			get { return new FakeSyncSound(_dcfilter, 735); }
		}

		public bool StartAsyncSound()
		{
			return true;
		}

		public void EndAsyncSound()
		{

		}

		public ControllerDefinition ControllerDefinition
		{
			get { return ControllerDeck.Definition; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound)
		{
			if (Tracer.Enabled)
				_cpu.TraceCallback = (s) => Tracer.Put(s);
			else
				_cpu.TraceCallback = null;

			_frame++;
			// read the controller state here for now
			get_controller_state();

			
			_cpu.AddPendingCycles(14934 - 3791 - _cpu.GetPendingCycles());
			_stic.Sr1 = true;

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				_psg.generate_sound(cycles);
				Connect();
			}

			_stic.Background();
			_stic.Mobs();

			_stic.Sr1 = false;
			_cpu.AddPendingCycles(3791 - _cpu.GetPendingCycles());

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				_psg.generate_sound(cycles);
				Connect();
			}

		}

		private int _frame;
		public int Frame { get { return _frame; } }

		public string SystemId
		{
			get { return "INTV"; }
		}

		public bool DeterministicEmulation { get { return true; } }

		[FeatureNotImplemented]
		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			_frame = 0;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{

		}
	}
}
