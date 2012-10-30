using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Concurrent;

namespace BizHawk.Emulation.CPUs.M6502
{
	/// <summary>
	/// maintains a managed 6502X and an unmanaged 6502X, running them alongside and ensuring consistency
	/// by taking savestates every cycle (!).  slow.
	/// </summary>
	public class MOS6502XDouble
	{
		MOS6502X m;
		MOS6502X_CPP n;

		public MOS6502XDouble(Action<System.Runtime.InteropServices.GCHandle> DisposeBuilder)
		{
			m = new MOS6502X(DisposeBuilder);
			n = new MOS6502X_CPP(DisposeBuilder);
			BCD_Enabled = true;
			m.SetCallbacks(
			delegate(ushort addr)
			{
				byte ret = ReadMemory(addr);
				reads.Enqueue(ret);
				return ret;
			},
			delegate(ushort addr)
			{
				byte ret = DummyReadMemory(addr);
				reads.Enqueue(ret);
				return ret;
			},
			delegate(ushort addr, byte value)
			{
				writes.Enqueue(value);
				WriteMemory(addr, value);
			}, DisposeBuilder);
			n.SetCallbacks(
			delegate(ushort addr)
			{
				if (reads.Count > 0)
					return reads.Dequeue();
				else
				{
					PreCrash();
					throw new Exception("native did extra read!");
				}
			},
			delegate(ushort addr)
			{
				if (reads.Count > 0)
					return reads.Dequeue();
				else
				{
					PreCrash();
					throw new Exception("native did extra read!");
				}
			},
			delegate(ushort addr, byte value)
			{
				if (writes.Count > 0)
				{
					byte test = writes.Dequeue();
					if (test != value)
					{
						PreCrash();
						throw new Exception(string.Format("writes were different! managed {0} native {1}", test, value));
					}
					// ignore because the write already happened
				}
				else
				{
					PreCrash();
					throw new Exception("native did extra write!");
				}
			}, DisposeBuilder);

			SyncUp();
		}
		Queue<byte> reads = new Queue<byte>();
		Queue<byte> writes = new Queue<byte>();

		private bool _BCD_Enabled;
		public bool BCD_Enabled { get { return _BCD_Enabled; } set { _BCD_Enabled = value; m.BCD_Enabled = value; n.BCD_Enabled = value; } }
		public bool debug { get; private set; }
		public bool throw_unhandled { get; private set; }

		public byte A { get; private set; }
		public byte X { get; private set; }
		public byte Y { get; private set; }
		byte _P;
		public byte P { get { return _P; } set { _P = value; m.P = value; n.P = value; SyncUp(); } }
		ushort _PC;
		public ushort PC { get { return _PC; } set { _PC = value; m.PC = value; n.PC = value; SyncUp(); } }
		byte _S;
		public byte S { get { return _S; } set { _S = value; m.S = value; n.S = value; SyncUp(); } }

		bool _IRQ;
		public bool IRQ { get { return _IRQ; } set { _IRQ = value; m.IRQ = value; n.IRQ = value; } }
		bool _NMI;
		public bool NMI { get { return _NMI; } set { _NMI = value; m.NMI = value; n.NMI = value; } }

		public int TotalExecutedCycles { get; private set; }

		public Func<ushort, byte> ReadMemory; //{ set { m.ReadMemory = value; n.ReadMemory = value; } }
		public Func<ushort, byte> DummyReadMemory; //{ set { m.DummyReadMemory = value; n.DummyReadMemory = value; } }
		public Action<ushort, byte> WriteMemory; //{ set { m.WriteMemory = value; n.WriteMemory = value; } }

		public void SetCallbacks
		(
			Func<ushort, byte> ReadMemory,
			Func<ushort, byte> DummyReadMemory,
			Action<ushort, byte> WriteMemory,
			Action<System.Runtime.InteropServices.GCHandle> DisposeBuilder
		)
		{
			this.ReadMemory = ReadMemory;
			this.DummyReadMemory = DummyReadMemory;
			this.WriteMemory = WriteMemory;
		}


		string oldleft = "";
		string oldright = "";
		void PreCrash()
		{
			System.Windows.Forms.MessageBox.Show(string.Format("about to crash! cached:\n==managed==\n{0}\n==native==\n{1}\n", oldleft, oldright));
		}
		void TestQueue()
		{
			if (reads.Count > 0)
			{
				PreCrash();
				throw new Exception("managed did extra read!");
			}
			if (writes.Count > 0)
			{
				PreCrash();
				throw new Exception("managed did extra write!");
			}
		}

		void SyncUp()
		{
			TestQueue();
			StringWriter sm = new StringWriter();
			Serializer ssm = new Serializer(sm);
			m.SyncState(ssm);
			sm.Flush();

			StringWriter sn = new StringWriter();
			Serializer ssn = new Serializer(sn);
			n.SyncState(ssn);
			sn.Flush();

			string sssm = sm.ToString();
			string sssn = sn.ToString();

			if (sssm != sssn)
			{
				PreCrash();
				throw new Exception(string.Format("save mismatch!\n==managed==\n{0}\n==native==\n{1}\n", sssm, sssn));
			}
			A = m.A;
			X = m.X;
			Y = m.Y;
			_P = m.P;
			_PC = m.PC;
			_S = m.S;
			_IRQ = m.IRQ;
			_NMI = m.NMI;
			oldleft = sssm;
			oldright = sssn;
		}

		public string State()
		{
			SyncUp();
			return m.State();
		}

		public void NESSoftReset()
		{
			m.NESSoftReset();
			n.NESSoftReset();
			SyncUp();
		}

		public void ExecuteOne()
		{
			m.ExecuteOne();
			n.ExecuteOne();
			SyncUp();
		}

		public void SyncState(Serializer ser)
		{
			SyncUp();
			m.SyncState(ser);
		}

		public string Disassemble(ushort pc, out int bytesToAdvance) { bytesToAdvance = 1; return "FOOBAR"; }

	}
}
