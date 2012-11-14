using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class DataPortBus
	{
		private DataPortConnector[] connectors;
		private bool[] connected = new bool[2];
		private byte[] direction = new byte[2];
		private byte[] latch = new byte[2];
		private List<Action> writeHooks = new List<Action>();

		public DataPortBus()
		{
			connectors = new DataPortConnector[2];
			connectors[0] = new DataPortConnector(ReadData0, ReadDirection0, WriteData0, WriteDirection0);
			connectors[1] = new DataPortConnector(ReadData1, ReadDirection1, WriteData1, WriteDirection1);
			connected[0] = false;
			connected[1] = false;
			direction[0] = 0x00;
			direction[1] = 0x00;
			latch[0] = 0x00;
			latch[1] = 0x00;
		}

		public void AttachWriteHook(Action act)
		{
			writeHooks.Add(act);
		}

		public DataPortConnector Connect()
		{
			if (!connected[0])
			{
				connected[0] = true;
				direction[0] = 0xFF;
				return connectors[0];
			}
			else if (!connected[1])
			{
				connected[1] = true;
				direction[1] = 0xFF;
				return connectors[1];
			}
			throw new Exception("Two connections to this bus have already been established..");
		}

		public void Disconnect(DataPortConnector connector)
		{
			if (connector.Equals(connectors[0]))
			{
				connected[0] = false;
				latch[0] = 0;
				direction[0] = 0;
			}
			else if (connector.Equals(connectors[1]))
			{
				connected[1] = false;
				latch[1] = 0;
				direction[1] = 0;
			}
		}

		private byte ReadData0()
		{
			if (connected[1])
				return (byte)((~direction[0] & latch[1]) | (direction[0] & latch[0]));
			else
				return latch[0];
		}

		private byte ReadData1()
		{
			if (connected[0])
				return (byte)((~direction[1] & latch[0]) | (direction[1] & latch[1]));
			else
				return latch[1];
		}

		private byte ReadDirection0()
		{
			return direction[0];
		}

		private byte ReadDirection1()
		{
			return direction[1];
		}

		private void WriteData0(byte val)
		{
			latch[0] &= (byte)~direction[0];
			latch[0] |= (byte)(val & direction[0]);
			foreach (Action hook in writeHooks)
				hook();
		}

		private void WriteData1(byte val)
		{
			latch[1] &= (byte)~direction[1];
			latch[1] |= (byte)(val & direction[1]);
			foreach (Action hook in writeHooks)
				hook();
		}

		private void WriteDirection0(byte val)
		{
			direction[0] = val;
			foreach (Action hook in writeHooks)
				hook();
		}

		private void WriteDirection1(byte val)
		{
			direction[1] = val;
			foreach (Action hook in writeHooks)
				hook();
		}
	}

	public class DataPortConnector
	{
		private Func<byte> ReadData;
		private Func<byte> ReadDirection;
		private Action<byte> WriteData;
		private Action<byte> WriteDirection;

		public DataPortConnector(Func<byte> newReadData, Func<byte> newReadDirection, Action<byte> newWriteData, Action<byte> newWriteDirection)
		{
			ReadData = newReadData;
			ReadDirection = newReadDirection;
			WriteData = newWriteData;
			WriteDirection = newWriteDirection;
		}

		public byte Data
		{
			get
			{
				return ReadData();
			}
			set
			{
				WriteData(value);
			}
		}

		public byte Direction
		{
			get
			{
				return ReadDirection();
			}
			set
			{
				WriteDirection(value);
			}
		}

		public DataPortListener Listener()
		{
			return new DataPortListener(ReadData, ReadDirection);
		}
	}

	public class DataPortListener
	{
		private Func<byte> ReadData;
		private Func<byte> ReadDirection;

		public DataPortListener(Func<byte> newReadData, Func<byte> newReadDirection)
		{
			ReadData = newReadData;
			ReadDirection = newReadDirection;
		}

		public byte Data
		{
			get
			{
				return ReadData();
			}
		}

		public byte Direction
		{
			get
			{
				return ReadDirection();
			}
		}
	}
}
