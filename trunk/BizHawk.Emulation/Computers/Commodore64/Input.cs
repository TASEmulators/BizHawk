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
			{"Key Insert/Delete", "Key Return", "Key Cursor Left/Right", "Key F7", "Key F1", "Key F3", "Key F5", "Cursor Up/Down"},
			{"Key 3", "Key W", "Key A", "Key 4", "Key Z", "Key S", "Key E", "Key Left Shift"},

		};

		static string[,] joystickMatrix = new string[,]
		{
			{"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button"},
			{"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button"}
		};

		private IController controller;
		private byte[] joystickLatch = new byte[2];
		private byte keyboardColumnData;
		private byte[] keyboardLatch = new byte[8];
		private byte keyboardRowData;

		public Input(IController newController, Cia newCia)
		{
			controller = newController;

			// attach input to a CIA I/O port
			newCia.ports[0].WritePort = WritePortA;
			newCia.ports[1].WritePort = WritePortB;
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
			for (int i = 0; i < 2; i++)
				joystickLatch[i] = GetJoystickBits(i);
			for (int i = 0; i < 8; i++)
				keyboardLatch[i] = GetKeyboardBits(i);
		}

		public void WritePortA(byte data)
		{
			// keyboard matrix column select
			keyboardColumnData = data;
		}

		public void WritePortB(byte data)
		{
			// keyboard matrix row select
			keyboardRowData = data;
		}
	}
}