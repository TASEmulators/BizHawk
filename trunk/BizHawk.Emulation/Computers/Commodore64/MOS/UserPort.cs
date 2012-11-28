using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public class UserPort
	{
		private Func<int, byte> peekMemory;
		private Action<int, byte> pokeMemory;
		private Func<ushort, byte> readMemory;
		private Action<ushort, byte> writeMemory;

		public UserPort()
		{
			// start up with no media connected
			Disconnect();
		}

		public void Connect(Func<int, byte> funcPeek, Action<int, byte> actPoke, Func<ushort, byte> funcRead, Action<ushort, byte> actWrite)
		{
			peekMemory = funcPeek;
			pokeMemory = actPoke;
			readMemory = funcRead;
			writeMemory = actWrite;
		}

		public void Disconnect()
		{
			peekMemory = DummyPeek;
			pokeMemory = DummyPoke;
			readMemory = DummyRead;
			writeMemory = DummyWrite;
		}

		private byte DummyPeek(int addr)
		{
			return 0xFF;
		}

		private void DummyPoke(int addr, byte val)
		{
			// do nothing
		}

		private byte DummyRead(ushort addr)
		{
			return 0xFF;
		}

		private void DummyWrite(ushort addr, byte val)
		{
			// do nothing
		}

		public void HardReset()
		{
			// note: this will not disconnect any attached media
		}

		public byte Peek(int addr)
		{
			return peekMemory(addr);
		}

		public void Poke(int addr, byte val)
		{
			pokeMemory(addr, val);
		}

		public byte Read(ushort addr)
		{
			return readMemory(addr);
		}

		public void Write(ushort addr, byte val)
		{
			writeMemory(addr, val);
		}
	}
}
