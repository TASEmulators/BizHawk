using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class DataPortBus
	{
		protected bool[] connected = new bool[2];
		protected DataPortConnector[] connectors;
		protected byte[] direction = new byte[2];
		protected DataPortConverter[] inputConverters;
		protected byte[] latch = new byte[2];
		protected DataPortConverter[] outputConverters;
		protected List<int> servingHooks = new List<int>();
		protected List<Action> writeHooks = new List<Action>();

		public DataPortBus()
		{
			inputConverters = new DataPortConverter[2];
			inputConverters[0] = new DataPortConverter();
			inputConverters[1] = new DataPortConverter();
			outputConverters = new DataPortConverter[2];
			outputConverters[0] = new DataPortConverter();
			outputConverters[1] = new DataPortConverter();
			connectors = new DataPortConnector[2];
			connectors[0] = new DataPortConnector(ReadData0, ReadDirection0, ReadLatch0, ReadRemoteLatch0, WriteData0, WriteDirection0);
			connectors[1] = new DataPortConnector(ReadData1, ReadDirection1, ReadLatch1, ReadRemoteLatch1, WriteData1, WriteDirection1);
			connected[0] = false;
			connected[1] = false;
			direction[0] = 0x00;
			direction[1] = 0x00;
			latch[0] = 0x00;
			latch[1] = 0x00;
		}

		public void AttachInputConverter(DataPortConnector connector, DataPortConverter converter)
		{
			if (connector.Equals(connectors[0]))
			{
				inputConverters[0] = converter;
			}
			else if (connector.Equals(connectors[1]))
			{
				inputConverters[1] = converter;
			}
		}

		public void AttachOutputConverter(DataPortConnector connector, DataPortConverter converter)
		{
			if (connector.Equals(connectors[0]))
			{
				outputConverters[0] = converter;
			}
			else if (connector.Equals(connectors[1]))
			{
				outputConverters[1] = converter;
			}
		}

		public void AttachWriteHook(Action act)
		{
			writeHooks.Add(act);
			servingHooks.Add(0);
		}

		protected void ClearHooks()
		{
			int count = servingHooks.Count;
			for (int i = 0; i < count; i++)
				servingHooks[i]--;
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

		public void Connect(DataPortConnector connection)
		{
			if (!connected[0])
			{
				connected[0] = true;
				connectors[0] = connection;
			}
			else if (!connected[1])
			{
				connected[1] = true;
				connectors[1] = connection;
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

		protected void ExecuteWriteHooks()
		{
			int count = servingHooks.Count;
			for (int i = 0; i < count; i++)
			{
				if (servingHooks[i] == 0)
				{
					servingHooks[i]++;
					writeHooks[i]();
				}
				else
				{
					servingHooks[i]++;
				}
			}
			ClearHooks();
		}

		public void LoadState(byte direction0, byte direction1, byte latch0, byte latch1)
		{
			direction[0] = direction0;
			direction[1] = direction1;
			latch[0] = latch0;
			latch[1] = latch1;
		}

		protected virtual byte ReadData0()
		{
			byte result;
			if (connected[1])
				result = (byte)((~direction[0] & latch[1]) | (direction[0] & latch[0]));
			else
				result = latch[0];
			return inputConverters[0].Convert(result, latch[1]);
		}

		protected virtual byte ReadData1()
		{
			byte result;
			if (connected[0])
				result = (byte)((~direction[1] & latch[0]) | (direction[1] & latch[1]));
			else
				result = latch[1];
			return inputConverters[1].Convert(result, latch[0]);
		}

		protected virtual byte ReadDirection0()
		{
			return direction[0];
		}

		protected virtual byte ReadDirection1()
		{
			return direction[1];
		}

		protected virtual byte ReadLatch0()
		{
			return latch[0];
		}

		protected virtual byte ReadLatch1()
		{
			return latch[1];
		}

		protected virtual byte ReadRemoteLatch0()
		{
			return latch[1];
		}

		protected virtual byte ReadRemoteLatch1()
		{
			return latch[0];
		}

		protected virtual void WriteData0(byte val)
		{
			byte result = latch[0];
			result &= (byte)~direction[0];
			result |= (byte)(val & direction[0]);
			latch[0] = outputConverters[0].Convert(result, latch[1]);
			ExecuteWriteHooks();
		}

		protected virtual void WriteData1(byte val)
		{
			byte result = latch[1];
			result &= (byte)~direction[1];
			result |= (byte)(val & direction[1]);
			latch[1] = outputConverters[1].Convert(result, latch[0]);
			ExecuteWriteHooks();
		}

		protected virtual void WriteDirection0(byte val)
		{
			direction[0] = val;
			ExecuteWriteHooks();
		}

		protected virtual void WriteDirection1(byte val)
		{
			direction[1] = val;
			ExecuteWriteHooks();
		}
	}

	public class DataPortConnector
	{
		private Func<byte> ReadData;
		private Func<byte> ReadDirection;
		private Func<byte> ReadLatch;
		private Func<byte> ReadRemoteLatch;
		private Action<byte> WriteData;
		private Action<byte> WriteDirection;

		public DataPortConnector()
		{
			ReadData = ReadDataDummy;
			ReadDirection = ReadDataDummy;
			ReadLatch = ReadDataDummy;
			ReadRemoteLatch = ReadDataDummy;
			WriteData = WriteDataDummy;
			WriteDirection = WriteDataDummy;
		}

		public DataPortConnector(DataPortConnector source)
		{
			ReadData = source.ReadData;
			ReadDirection = source.ReadDirection;
			ReadLatch = source.ReadLatch;
			ReadRemoteLatch = source.ReadRemoteLatch;
			WriteData = source.WriteData;
			WriteDirection = source.WriteDirection;
		}

		public DataPortConnector(Func<byte> newReadData, Func<byte> newReadDirection, Func<byte> newReadLatch, Func<byte> newReadRemoteLatch, Action<byte> newWriteData, Action<byte> newWriteDirection)
		{
			ReadData = newReadData;
			ReadDirection = newReadDirection;
			ReadLatch = newReadLatch;
			ReadRemoteLatch = newReadRemoteLatch;
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

		public byte Latch
		{
			get
			{
				return ReadLatch();
			}
		}

		public DataPortListener Listener()
		{
			return new DataPortListener(ReadData, ReadDirection);
		}

		private byte ReadDataDummy()
		{
			return 0x00;
		}

		public byte RemoteLatch
		{
			get
			{
				return ReadRemoteLatch();
			}
		}

		private void WriteDataDummy(byte val)
		{
			return;
		}
	}

	public class DataPortConverter
	{
		public virtual byte Convert(byte input, byte remote)
		{
			// the base converter transfers the values directly
			return input;
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
