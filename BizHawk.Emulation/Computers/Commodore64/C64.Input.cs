using BizHawk.Emulation.Computers.Commodore64.MOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class C64 : IEmulator
	{
		private PortAdapter inputAdapter0;
		private PortAdapter inputAdapter1;
		private byte[] joystickMatrix = new byte[2];
		private byte[] keyboardMatrix = new byte[8];

		private void InitInput()
		{
			inputAdapter0 = chips.cia0.Adapter0;
			inputAdapter1 = chips.cia0.Adapter1;
		}

		private void PollInput()
		{
			joystickMatrix[0] = 0xFF;
			joystickMatrix[0] &= controller["P1 Up"] ? (byte)0xFE : (byte)0xFF;
			joystickMatrix[0] &= controller["P1 Down"] ? (byte)0xFD : (byte)0xFF;
			joystickMatrix[0] &= controller["P1 Left"] ? (byte)0xFB : (byte)0xFF;
			joystickMatrix[0] &= controller["P1 Right"] ? (byte)0xF7 : (byte)0xFF;
			joystickMatrix[0] &= controller["P1 Button"] ? (byte)0xEF : (byte)0xFF;

			joystickMatrix[1] = 0xFF;
			joystickMatrix[1] &= controller["P2 Up"] ? (byte)0xFE : (byte)0xFF;
			joystickMatrix[1] &= controller["P2 Down"] ? (byte)0xFD : (byte)0xFF;
			joystickMatrix[1] &= controller["P2 Left"] ? (byte)0xFB : (byte)0xFF;
			joystickMatrix[1] &= controller["P2 Right"] ? (byte)0xF7 : (byte)0xFF;
			joystickMatrix[1] &= controller["P2 Button"] ? (byte)0xEF : (byte)0xFF;

			keyboardMatrix[0] = 0xFF;
			keyboardMatrix[0] &= controller["Key Insert/Delete"] ? (byte)0xFE : (byte)0xFF;
			keyboardMatrix[0] &= controller["Key Return"] ? (byte)0xFD : (byte)0xFF;
			keyboardMatrix[0] &= controller["Key Cursor Left/Right"] ? (byte)0xFB : (byte)0xFF;
			keyboardMatrix[0] &= controller["Key F7"] ? (byte)0xF7 : (byte)0xFF;
			keyboardMatrix[0] &= controller["Key F1"] ? (byte)0xEF : (byte)0xFF;
			keyboardMatrix[0] &= controller["Key F3"] ? (byte)0xDF : (byte)0xFF;
			keyboardMatrix[0] &= controller["Key F5"] ? (byte)0xBF : (byte)0xFF;
			keyboardMatrix[0] &= controller["Key Cursor Up/Down"] ? (byte)0x7F : (byte)0xFF;

			keyboardMatrix[1] = 0xFF;
			keyboardMatrix[1] &= controller["Key 3"] ? (byte)0xFE : (byte)0xFF;
			keyboardMatrix[1] &= controller["Key W"] ? (byte)0xFD : (byte)0xFF;
			keyboardMatrix[1] &= controller["Key A"] ? (byte)0xFB : (byte)0xFF;
			keyboardMatrix[1] &= controller["Key 4"] ? (byte)0xF7 : (byte)0xFF;
			keyboardMatrix[1] &= controller["Key Z"] ? (byte)0xEF : (byte)0xFF;
			keyboardMatrix[1] &= controller["Key S"] ? (byte)0xDF : (byte)0xFF;
			keyboardMatrix[1] &= controller["Key E"] ? (byte)0xBF : (byte)0xFF;
			keyboardMatrix[1] &= controller["Key Left Shift"] ? (byte)0x7F : (byte)0xFF;

			keyboardMatrix[2] = 0xFF;
			keyboardMatrix[2] &= controller["Key 5"] ? (byte)0xFE : (byte)0xFF;
			keyboardMatrix[2] &= controller["Key R"] ? (byte)0xFD : (byte)0xFF;
			keyboardMatrix[2] &= controller["Key D"] ? (byte)0xFB : (byte)0xFF;
			keyboardMatrix[2] &= controller["Key 6"] ? (byte)0xF7 : (byte)0xFF;
			keyboardMatrix[2] &= controller["Key C"] ? (byte)0xEF : (byte)0xFF;
			keyboardMatrix[2] &= controller["Key F"] ? (byte)0xDF : (byte)0xFF;
			keyboardMatrix[2] &= controller["Key T"] ? (byte)0xBF : (byte)0xFF;
			keyboardMatrix[2] &= controller["Key X"] ? (byte)0x7F : (byte)0xFF;

			keyboardMatrix[3] = 0xFF;
			keyboardMatrix[3] &= controller["Key 7"] ? (byte)0xFE : (byte)0xFF;
			keyboardMatrix[3] &= controller["Key Y"] ? (byte)0xFD : (byte)0xFF;
			keyboardMatrix[3] &= controller["Key G"] ? (byte)0xFB : (byte)0xFF;
			keyboardMatrix[3] &= controller["Key 8"] ? (byte)0xF7 : (byte)0xFF;
			keyboardMatrix[3] &= controller["Key B"] ? (byte)0xEF : (byte)0xFF;
			keyboardMatrix[3] &= controller["Key H"] ? (byte)0xDF : (byte)0xFF;
			keyboardMatrix[3] &= controller["Key U"] ? (byte)0xBF : (byte)0xFF;
			keyboardMatrix[3] &= controller["Key V"] ? (byte)0x7F : (byte)0xFF;

			keyboardMatrix[4] = 0xFF;
			keyboardMatrix[4] &= controller["Key 9"] ? (byte)0xFE : (byte)0xFF;
			keyboardMatrix[4] &= controller["Key I"] ? (byte)0xFD : (byte)0xFF;
			keyboardMatrix[4] &= controller["Key J"] ? (byte)0xFB : (byte)0xFF;
			keyboardMatrix[4] &= controller["Key 0"] ? (byte)0xF7 : (byte)0xFF;
			keyboardMatrix[4] &= controller["Key M"] ? (byte)0xEF : (byte)0xFF;
			keyboardMatrix[4] &= controller["Key K"] ? (byte)0xDF : (byte)0xFF;
			keyboardMatrix[4] &= controller["Key O"] ? (byte)0xBF : (byte)0xFF;
			keyboardMatrix[4] &= controller["Key N"] ? (byte)0x7F : (byte)0xFF;

			keyboardMatrix[5] = 0xFF;
			keyboardMatrix[5] &= controller["Key Plus"] ? (byte)0xFE : (byte)0xFF;
			keyboardMatrix[5] &= controller["Key P"] ? (byte)0xFD : (byte)0xFF;
			keyboardMatrix[5] &= controller["Key L"] ? (byte)0xFB : (byte)0xFF;
			keyboardMatrix[5] &= controller["Key Minus"] ? (byte)0xF7 : (byte)0xFF;
			keyboardMatrix[5] &= controller["Key Period"] ? (byte)0xEF : (byte)0xFF;
			keyboardMatrix[5] &= controller["Key Colon"] ? (byte)0xDF : (byte)0xFF;
			keyboardMatrix[5] &= controller["Key At"] ? (byte)0xBF : (byte)0xFF;
			keyboardMatrix[5] &= controller["Key Comma"] ? (byte)0x7F : (byte)0xFF;

			keyboardMatrix[6] = 0xFF;
			keyboardMatrix[6] &= controller["Key Pound"] ? (byte)0xFE : (byte)0xFF;
			keyboardMatrix[6] &= controller["Key Asterisk"] ? (byte)0xFD : (byte)0xFF;
			keyboardMatrix[6] &= controller["Key Semicolon"] ? (byte)0xFB : (byte)0xFF;
			keyboardMatrix[6] &= controller["Key Clear/Home"] ? (byte)0xF7 : (byte)0xFF;
			keyboardMatrix[6] &= controller["Key Right Shift"] ? (byte)0xEF : (byte)0xFF;
			keyboardMatrix[6] &= controller["Key Equal"] ? (byte)0xDF : (byte)0xFF;
			keyboardMatrix[6] &= controller["Key Up Arrow"] ? (byte)0xBF : (byte)0xFF;
			keyboardMatrix[6] &= controller["Key Slash"] ? (byte)0x7F : (byte)0xFF;

			keyboardMatrix[7] = 0xFF;
			keyboardMatrix[7] &= controller["Key 1"] ? (byte)0xFE : (byte)0xFF;
			keyboardMatrix[7] &= controller["Key Left Arrow"] ? (byte)0xFD : (byte)0xFF;
			keyboardMatrix[7] &= controller["Key Control"] ? (byte)0xFB : (byte)0xFF;
			keyboardMatrix[7] &= controller["Key 2"] ? (byte)0xF7 : (byte)0xFF;
			keyboardMatrix[7] &= controller["Key Space"] ? (byte)0xEF : (byte)0xFF;
			keyboardMatrix[7] &= controller["Key Commodore"] ? (byte)0xDF : (byte)0xFF;
			keyboardMatrix[7] &= controller["Key Q"] ? (byte)0xBF : (byte)0xFF;
			keyboardMatrix[7] &= controller["Key Run/Stop"] ? (byte)0x7F : (byte)0xFF;
		}

		private void WriteInputPort()
		{
			inputAdapter0.Data = 0xFF;
			inputAdapter1.Data = 0xFF;

			byte portA = inputAdapter0.Data;
			byte portB = inputAdapter1.Data;

			if ((portA & 0x01) == 0)
				portB &= keyboardMatrix[0];
			if ((portA & 0x02) == 0)
				portB &= keyboardMatrix[1];
			if ((portA & 0x04) == 0)
				portB &= keyboardMatrix[2];
			if ((portA & 0x08) == 0)
				portB &= keyboardMatrix[3];
			if ((portA & 0x10) == 0)
				portB &= keyboardMatrix[4];
			if ((portA & 0x20) == 0)
				portB &= keyboardMatrix[5];
			if ((portA & 0x40) == 0)
				portB &= keyboardMatrix[6];
			if ((portA & 0x80) == 0)
				portB &= keyboardMatrix[7];

			portA &= joystickMatrix[1];
			portB &= joystickMatrix[0];

			inputAdapter0.Data = portA;
			inputAdapter1.Data = portB;
		}
	}
}
