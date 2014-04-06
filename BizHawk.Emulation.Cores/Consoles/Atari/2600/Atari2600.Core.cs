using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600
	{
		private TIA _tia;
		private DCFilter _dcfilter;
		private MapperBase _mapper;
		public byte[] Ram;

		public byte[] Rom { get; private set; }
		public MOS6502X Cpu { get; private set; }
		public M6532 M6532 { get; private set; }

		public byte BaseReadMemory(ushort addr)
		{
			addr = (ushort)(addr & 0x1FFF);
			if ((addr & 0x1080) == 0)
			{
				return _tia.ReadMemory(addr, false);
			}
			
			if ((addr & 0x1080) == 0x0080)
			{
				return M6532.ReadMemory(addr, false);
			}
			
			return Rom[addr & 0x0FFF];
		}

		public byte BasePeekMemory(ushort addr)
		{
			addr = (ushort)(addr & 0x1FFF);
			if ((addr & 0x1080) == 0)
			{
				return _tia.ReadMemory(addr, true);
			}
			
			if ((addr & 0x1080) == 0x0080)
			{
				return M6532.ReadMemory(addr, true);
			}
			
			return Rom[addr & 0x0FFF];
		}

		public void BaseWriteMemory(ushort addr, byte value)
		{
			addr = (ushort)(addr & 0x1FFF);
			if ((addr & 0x1080) == 0)
			{
				_tia.WriteMemory(addr, value);
			}
			else if ((addr & 0x1080) == 0x0080)
			{
				M6532.WriteMemory(addr, value);
			}
			else
			{
				Console.WriteLine("ROM write(?):  " + addr.ToString("x"));
			}
		}

		public byte ReadMemory(ushort addr)
		{
			var temp = _mapper.ReadMemory((ushort)(addr & 0x1FFF));

			CoreComm.MemoryCallbackSystem.CallRead(addr);

			return temp;
		}

		public byte PeekMemory(ushort addr)
		{
			var temp = _mapper.ReadMemory((ushort)(addr & 0x1FFF));

			return temp;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			_mapper.WriteMemory((ushort)(addr & 0x1FFF), value);

			CoreComm.MemoryCallbackSystem.CallWrite(addr);
		}

		public void ExecFetch(ushort addr)
		{
			CoreComm.MemoryCallbackSystem.CallExecute(addr);
		}

		public void RebootCore()
		{
			// Regenerate mapper here to make sure its state is entirely clean
			switch (this._game.GetOptionsDict()["m"])
			{
				case "2IN1":
				case "4IN1":
				case "8IN1":
				case "16IN1":
				case "32IN1":
					_mapper = new Multicart();
					break;
				case "AR":
					_mapper = new mAR();
					break;
				case "4K":
					_mapper = new m4K();
					break;
				case "2K":
					_mapper = new m2K();
					break;
				case "CV":
					_mapper = new mCV();
					break;
				case "DPC":
					_mapper = new mDPC();
					break;
				case "DPC+":
					_mapper = new mDPCPlus();
					break;
				case "F8":
					_mapper = new mF8();
					break;
				case "F8SC":
					_mapper = new mF8SC();
					break;
				case "F6":
					_mapper = new mF6();
					break;
				case "F6SC":
					_mapper = new mF6SC();
					break;
				case "F4":
					_mapper = new mF4();
					break;
				case "F4SC":
					_mapper = new mF4SC();
					break;
				case "FE":
					_mapper = new mFE();
					break;
				case "E0":
					_mapper = new mE0();
					break;
				case "3F":
					_mapper = new m3F();
					break;
				case "FA":
					_mapper = new mFA();
					break;
				case "FA2":
					_mapper = new mFA2();
					break;
				case "E7":
					_mapper = new mE7();
					break;
				case "F0":
					_mapper = new mF0();
					break;
				case "UA":
					_mapper = new mUA();
					break;

				// Homebrew mappers
				case "3E":
					_mapper = new m3E();
					break;
				case "0840":
					_mapper = new m0840();
					break;
				case "MC":
					_mapper = new mMC();
					break;
				case "EF":
					_mapper = new mEF();
					break;
				case "EFSC":
					_mapper = new mEFSC();
					break;
				case "X07":
					_mapper = new mX07();
					break;
				case "4A50":
					_mapper = new m4A50();
					break;
				case "SB":
					_mapper = new mSB();
					break;

				default:
					throw new InvalidOperationException("mapper not supported: " + _game.GetOptionsDict()["m"]);
			}

			_mapper.Core = this;

			_lagcount = 0;
			Cpu = new MOS6502X
			{
				ReadMemory = this.ReadMemory,
				WriteMemory = this.WriteMemory,
				PeekMemory = this.PeekMemory,
				DummyReadMemory = this.ReadMemory,
				OnExecFetch = this.ExecFetch
			};

			_tia = new TIA(this);

			// dcfilter coefficent is from real observed hardware behavior: a latched "1" will fully decay by ~170 or so tia sound cycles
			_dcfilter = DCFilter.AsISoundProvider(_tia, 256);

			M6532 = new M6532(this);

			// Set up the system state here. for instance..
			// Read from the reset vector for where to start
			Cpu.PC = (ushort)(ReadMemory(0x1FFC) + (ReadMemory(0x1FFD) << 8)); // set the initial PC

			// Show mapper class on romstatusdetails
			CoreComm.RomStatusDetails =
				string.Format(
					"{0}\r\nSHA1:{1}\r\nMD5:{2}\r\nMapper Impl \"{3}\"",
					this._game.Name,
					Util.Hash_SHA1(Rom), 
					Util.Hash_MD5(Rom),
					_mapper.GetType());
		}

		public void HardReset()
		{
			Ram = new byte[128];
			_mapper.HardReset();

			Cpu = new MOS6502X
			{
				ReadMemory = this.ReadMemory,
				WriteMemory = this.WriteMemory,
				PeekMemory = this.PeekMemory,
				DummyReadMemory = this.ReadMemory,
				OnExecFetch = this.ExecFetch
			};

			_tia.Reset();

			M6532 = new M6532(this);
			Cpu.PC = (ushort)(ReadMemory(0x1FFC) + (ReadMemory(0x1FFD) << 8)); // set the initial PC
		}

		private bool _hardResetSignal;

		public void FrameAdvance(bool render, bool rendersound)
		{
			_frame++;
			_islag = true;
			_tia.FrameComplete = false;
			while (_tia.FrameComplete == false)
			{
				_tia.Execute(1);
				_tia.Execute(1);
				_tia.Execute(1);

				M6532.Timer.Tick();
				if (CoreComm.Tracer.Enabled)
				{
					CoreComm.Tracer.Put(Cpu.TraceState());
				}

				Cpu.ExecuteOne();
				_mapper.ClockCpu();
			}

			if (_hardResetSignal)
			{
				HardReset();
			}

			_hardResetSignal = Controller["Power"];

			if (_islag)
			{
				LagCount++;
			}
		}

		public byte ReadControls1(bool peek)
		{
			CoreComm.InputCallback.Call();
			byte value = 0xFF;

			if (Controller["P1 Up"]) { value &= 0xEF; }
			if (Controller["P1 Down"]) { value &= 0xDF; }
			if (Controller["P1 Left"]) { value &= 0xBF; }
			if (Controller["P1 Right"]) { value &= 0x7F; }
			if (Controller["P1 Button"]) { value &= 0xF7; }
			
			if (!peek)
			{
				_islag = false;
			}

			return value;
		}

		public byte ReadControls2(bool peek)
		{
			CoreComm.InputCallback.Call();
			byte value = 0xFF;

			if (Controller["P2 Up"]) { value &= 0xEF; }
			if (Controller["P2 Down"]) { value &= 0xDF; }
			if (Controller["P2 Left"]) { value &= 0xBF; }
			if (Controller["P2 Right"]) { value &= 0x7F; }
			if (Controller["P2 Button"]) { value &= 0xF7; }
			
			if (!peek)
			{
				_islag = false;
			}

			return value;
		}

		public byte ReadConsoleSwitches(bool peek)
		{
			byte value = 0xFF;
			bool select = Controller["Select"];
			bool reset = Controller["Reset"];

			if (reset) { value &= 0xFE; }
			if (select) { value &= 0xFD; }
			if (SyncSettings.BW) { value &= 0xF7; }
			if (SyncSettings.LeftDifficulty) { value &= 0xBF; }
			if (SyncSettings.RightDifficulty) { value &= 0x7F; }
			
			if (!peek)
			{
				_islag = false;
			}

			return value;
		}
	}
}