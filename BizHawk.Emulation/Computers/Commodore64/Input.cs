using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class Input
	{
		static string[,] keyboardMatrix = new string[,]
		{
			{"Key Insert/Delete", "Key Return", "Key Cursor Left/Right", "Key F7", "Key F1", "Key F3", "Key F5", "Key Cursor Up/Down"},
			{"Key 3", "Key W", "Key A", "Key 4", "Key Z", "Key S", "Key E", "Key Left Shift"},
			{"Key 5", "Key R", "Key D", "Key 6", "Key C", "Key F", "Key T", "Key X"},
			{"Key 7", "Key Y", "Key G", "Key 8", "Key B", "Key H", "Key U", "Key V"},
			{"Key 9", "Key I", "Key J", "Key 0", "Key M", "Key K", "Key O", "Key N"},
			{"Key Plus", "Key P", "Key L", "Key Minus", "Key Period", "Key Colon", "Key At", "Key Comma"},
			{"Key Pound", "Key Asterisk", "Key Semicolon", "Key Clear/Home", "Key Right Shift", "Key Equal", "Key Up Arrow", "Key Slash"},
			{"Key 1", "Key Left Arrow", "Key Control", "Key 2", "Key Space", "Key Commodore", "Key Q", "Key Run/Stop"}
		};

		static string[,] joystickMatrix = new string[,]
		{
			{"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button"},
			{"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button"}
		};

		public IController controller;
		public bool restorePressed;

		private byte[] joystickLatch = new byte[2];
		private byte keyboardColumnData = 0xFF;
		private byte[] keyboardLatch = new byte[8];
		private byte keyboardRowData = 0xFF;
		private DataPortConnector[] ports;

		public Input(DataPortConnector[] newPorts)
		{
			ports = newPorts;

			// set full output
			ports[0].Direction = 0xFF;
			ports[1].Direction = 0xFF;
		}

		private byte GetJoystickBits(int index)
		{
			byte result = 0xE0;
			result |= controller[joystickMatrix[index, 0]] ? (byte)0x00 : (byte)0x01;
			result |= controller[joystickMatrix[index, 1]] ? (byte)0x00 : (byte)0x02;
			result |= controller[joystickMatrix[index, 2]] ? (byte)0x00 : (byte)0x04;
			result |= controller[joystickMatrix[index, 3]] ? (byte)0x00 : (byte)0x08;
			result |= controller[joystickMatrix[index, 4]] ? (byte)0x00 : (byte)0x10;
			return result;
		}

		private byte GetKeyboardBits(int row)
		{
			byte result;
			result = controller[keyboardMatrix[row, 0]] ? (byte)0x00 : (byte)0x01;
			result |= controller[keyboardMatrix[row, 1]] ? (byte)0x00 : (byte)0x02;
			result |= controller[keyboardMatrix[row, 2]] ? (byte)0x00 : (byte)0x04;
			result |= controller[keyboardMatrix[row, 3]] ? (byte)0x00 : (byte)0x08;
			result |= controller[keyboardMatrix[row, 4]] ? (byte)0x00 : (byte)0x10;
			result |= controller[keyboardMatrix[row, 5]] ? (byte)0x00 : (byte)0x20;
			result |= controller[keyboardMatrix[row, 6]] ? (byte)0x00 : (byte)0x40;
			result |= controller[keyboardMatrix[row, 7]] ? (byte)0x00 : (byte)0x80;
			return result;
		}

		public void Poll()
		{
			restorePressed = controller["Key Restore"];

			for (int i = 0; i < 2; i++)
				joystickLatch[i] = GetJoystickBits(i);
			for (int i = 0; i < 8; i++)
				keyboardLatch[i] = GetKeyboardBits(i);
			UpdatePortData();
		}

		private void UpdatePortData()
		{
			int keyboardShift = keyboardColumnData;
			byte port0result = 0xFF;
			byte port1result = 0xFF;

			port0result = (byte)(joystickLatch[1]);

			for (int i = 0; i < 8; i++)
			{
				if ((keyboardShift & 0x01) == 0x00)
				{
					port1result &= keyboardLatch[i];
				}
				keyboardShift >>= 1;
			}
			port1result &= joystickLatch[0];

			ports[0].Data = port0result;
			ports[1].Data = port1result;
		}

		public void WritePortA()
		{
			// keyboard matrix column select
			keyboardColumnData = ports[0].RemoteLatch;
			UpdatePortData();
		}

		public void WritePortB()
		{
			// keyboard matrix row select
			keyboardRowData = ports[1].RemoteLatch;
			UpdatePortData();
		}
	}
}