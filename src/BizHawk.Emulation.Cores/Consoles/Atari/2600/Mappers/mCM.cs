using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	Spectra-video Compumate Add-on Kevtris Documentation

	This is more than just a cartridge mapper- it's also a "computer" add-on.  
	There's two 8K EPROMs soldered on top of each other.  There's two short
	wires with DB-9's on them which you plug into the two controller ports.
	A 42 or so key membrane keyboard with audio in and audio out, and 1K of RAM.

	Port A on the RIOT is used to run most of the functions on the Compumate:

	7       0
	---------
	ACRE 31BB

	A - Audio input from tape player
	C - Audio out to tape player and 4017 CLK
	R - 4017 RST, and RAM direction. (high = write, low = read)
	E - RAM enable. 1 = disable RAM, 0 = enable RAM
	3 - Row 3 of keyboard
	1 - Row 1 of keyboard 
	B - 2 bit ROM bank number

	All bits are outputs except for the 2 row inputs from the keyboard.

	Unlike most things, the Compumate uses all three of the TIA inputs on each
	joystick port (paddles and fire).

	TIA inputs:

	0 - function key
	1 - pulled high thru 20K resistor
	2 - pulled high thru 20K resistor
	3 - shift key
	4 - Row 0
	5 - Row 2


	Memory Map:
	-----------

	1000-1FFF : selectable 4K ROM bank (selected by D0, D1 on portA)

	On power up, the port is all 1's, so the last bank of ROM is enabled, RAM is
	disabled.

	when RAM is enabled:

	1000-17FF : 2K of RAM.  It's mapped into 1000-17FF.  Unlike most 2600 carts,
	bit 5 of portA controls if the RAM is readable or writable.  When it's high,
	the RAM is write only.  When it's low, it is read only. There's no separate
	read and write ports.


	Keyboard:
	---------

	The keyboard's composed of a 4017 1 of 10 counter, driving the 10 columns of
	the keyboard.  It has 4 rows.  The 4 row outputs are buffered by inverters.

	Bit 5 of portA controls the reset line on the 4017.  Pulling it high will reset
	scanning to column 0.  Pulling it low will allow the counter to be clocked.

	Bit 6 of portA clocks the 4017.  Each rising edge advances the column one
	count.

	There's 10 columns labeled 0-9, and 4 rows, labeled 0-3.

							 Column

	  0     1     2     3     4     5     6     7     8     9
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+
	| 7 | | 6 | | 8 | | 2 | | 3 | | 0 | | 9 | | 5 | | 1 | | 4 |  0
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ 
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ 
	| U | | Y | | I | | W | | E | | P | | O | | T | | Q | | R |  1
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+     Row
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+
	| J | | H | | K | | S | | D | |ent| | L | | G | | A | | F |  2
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ 
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+
	| M | | N | | < | | X | | C | |spc| | > | | B | | Z | | V |  3
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ 

	Function and Shift are separate keys that are read by 2 of the paddle inputs.
	These two buttons pull the specific paddle input low when pressed.

	Because the inputs are inverted, a low indicates a pressed button, and a high 
	is an unpressed one.

	The audio input/output are designed to drive a tape player.  The audio output is 
	buffered through an inverter and 2 resistors and a capacitor to reduce the level
	to feed it into the tape player.

	The audio input is passed through a .1uf capacitor and is pulled to 1/2 supply
	by two 20K resistors, then it goes through a hex inverting schmitt trigger to
	square it up.  This then runs into bit 7 of portA.
	*/

	/*
	* SpectraVideo Compumate Add-on Stella Documentation
	Cartridge class used for SpectraVideo Compumate bankswitched games.

	This is more than just a cartridge mapper - it's also a "computer" add-on.  
	There's two 8K EPROMs soldered on top of each other.  There's two short
	wires with DB-9's on them which you plug into the two controller ports.
	A 42 or so key membrane keyboard with audio in and audio out, and 2K of RAM.

	There are 4 4K banks selectable at $1000 - $1FFF, and 2K RAM at
	$1800 - $1FFF (R/W 'line' is available at SWCHA D5, so there's no separate
	read and write ports).

	Bankswitching is done though the controller ports
	SWCHA: D7 = Audio input from tape player
			D6 = Audio out to tape player and 4017 CLK
				1 -> increase key column (0 to 9)
			D5 = 4017 RST, and RAM direction. (high = write, low = read)
				1 -> reset key column to 0 (if D4 = 0)
				0 -> enable RAM writing (if D4 = 1)
			D4 = RAM enable: 1 = disable RAM, 0 = enable RAM
			D3 = keyboard row 3 input (0 = key pressed)
			D2 = keyboard row 1 input (0 = key pressed)
			D1 = bank select high bit
			D0 = bank select low bit

	INPUT0: D7 = FUNC key input (0 on startup / 1 = key pressed)
	INPUT1: D7 = always HIGH input (pulled high thru 20K resistor)
	INPUT2: D7 = always HIGH input (pulled high thru 20K resistor)
	INPUT3: D7 = SHIFT key input (0 on startup / 1 = key pressed)
	INPUT4: D7 = keyboard row 0 input (0 = key pressed)
	INPUT5: D7 = keyboard row 2 input (0 = key pressed)

	The keyboard's composed of a 4017 1 of 10 counter, driving the 10 columns of
	the keyboard.  It has 4 rows.  The 4 row outputs are buffered by inverters.

	Bit 5 of portA controls the reset line on the 4017.  Pulling it high will reset
	scanning to column 0.  Pulling it low will allow the counter to be clocked.

	Bit 6 of portA clocks the 4017.  Each rising edge advances the column one
	count.

	There's 10 columns labeled 0-9, and 4 rows, labeled 0-3.

							Column

	0     1     2     3     4     5     6     7     8     9
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+
	| 7 | | 6 | | 8 | | 2 | | 3 | | 0 | | 9 | | 5 | | 1 | | 4 |  0
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ 
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ 
	| U | | Y | | I | | W | | E | | P | | O | | T | | Q | | R |  1
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+     Row
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+
	| J | | H | | K | | S | | D | |ent| | L | | G | | A | | F |  2
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ 
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+
	| M | | N | | < | | X | | C | |spc| | > | | B | | Z | | V |  3
	+---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ 

	Function and Shift are separate keys that are read by 2 of the paddle inputs.
	These two buttons pull the specific paddle input low when pressed.

	Because the inputs are inverted, a low indicates a pressed button, and a high 
	is an unpressed one.

	The audio input/output are designed to drive a tape player.  The audio output is 
	buffered through an inverter and 2 resistors and a capacitor to reduce the level
	to feed it into the tape player.

	The audio input is passed through a .1uf capacitor and is pulled to 1/2 supply
	by two 20K resistors, then it goes through a hex inverting schmitt trigger to
	square it up.  This then runs into bit 7 of portA.
	*/
	internal sealed class mCM : MapperBase
	{
		// TODO: PokeMem
		private byte[] _ram = new byte[2048];
		private int _bank4K = 3; // On Start up, controller port is all 1's, so start on the last bank, flags enabled
		private bool _disableRam = true;
		private bool _writeMode;
		private int _column;
		private bool _funcKey;
		private bool _shiftKey;

		public mCM(Atari2600 core) : base(core)
		{
		}

		public override byte[] CartRam => _ram;

		public override void HardReset()
		{
			_ram = new byte[2048];
			_bank4K = 3;
			_disableRam = true;
			_writeMode = true;
			_column = 0;
			_funcKey = false;
			_shiftKey = false;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("cartRam", ref _ram, false);
			ser.Sync("bank4k", ref _bank4K);
			ser.Sync("column", ref _column);
			ser.Sync("disableRam", ref _disableRam);
			ser.Sync("writeMode", ref _writeMode);
			ser.Sync("FuncKey", ref _funcKey);
			ser.Sync("ShiftKey", ref _shiftKey);

			base.SyncState(ser);
		}

		public override byte ReadMemory(ushort addr)
		{ 
			// A unique feature of the keyboard is that it changes the operation of inputs 0-3 
			// by holding them high in the no-button-pressed state.
			// However exposing this behaviour to the rest of the system would be overly cumbersome
			// so instead we bypass these cases here
			if ((addr & 0x000F) == 8 && (addr & 0x1080) == 0 && addr < 1000)
			{
				// if func key pressed
				if (_funcKey)
				{
					return 0x80;
				}

				return 0;
			}

			if ((addr & 0x000F) == 9 && (addr & 0x1080) == 0 && addr < 1000)
			{
				return 0x80;
			}

			if ((addr & 0x000F) == 0xA && (addr & 0x1080) == 0 && addr < 1000)
			{
				return 0x80;
			}

			if ((addr & 0x000F) == 0xB && (addr & 0x1080) == 0 && addr < 1000)
			{
				// if shift key pressed
				if (_shiftKey)
				{
					return 0x80;
				}

				return 0;
			}

			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			// Lower 2K is always the first 2K of the ROM bank
			// Upper 2K is also Rom if ram is enabled
			if (addr < 0x1800 || _disableRam)
			{
				return Core.Rom[(_bank4K << 12) + (addr & 0xFFF)];
			}

			// 2K of RAM
			if (!_writeMode)
			{
				return _ram[addr & 0x7FF];
			}

			// Attempting to read while in write mode
			throw new Exception("this hasn't been tested");
		}

		public override byte PeekMemory(ushort addr) => ReadMemory(addr);

		public override void WriteMemory(ushort addr, byte value)
			=> WriteMem(addr, value, false);

		public override void PokeMemory(ushort addr, byte value)
			=> WriteMem(addr, value, true);

		private void WriteMem(ushort addr, byte value, bool poke)
		{
			////var isPortA = false; // adelikat: Commented out this variable to remove a warning.  Should this be deleted or is this supposed to be actually used?

			if ((addr & 0x0200) == 0) // If the RS bit is not set, this is a ram write
			{
			}
			else
			{
				// If bit 0x0010 is set, and bit 0x0004 is set, this is a timer write
				if ((addr & 0x0014) == 0x0014)
				{
				}

				// If bit 0x0004 is not set, bit 0x0010 is ignored and
				// these are register writes
				else if ((addr & 0x0004) == 0)
				{
					var registerAddr = (ushort)(addr & 0x0007);

					if (registerAddr == 0x00)
					{
						if (addr != 640 && addr >= 0x280) // register addresses are only above 0x280
						{
							// Write Output reg A
							////isPortA = true;
						}
					}
				}
			}

			if (addr == 0x280 && !poke) // Stella uses only 280
				////if (isPortA && !poke)
			{
				var bit5 = value.Bit(5);
				var bit4 = value.Bit(4);
				
				// D5 RAM direction. (high = write, low = read)
				// 0 -> enable RAM writing (if D4 = 1)
				// D4 = RAM enable: 1 = disable RAM, 0 = enable RAM
				_disableRam = bit4;

				_writeMode = (value & 0x30) == 0x20;

				_bank4K = value & 0x03;

				// D6 = 1 -> increase key column (0 to 9)
				// D5 = 1 -> reset key column to 0 (if D4 = 0)
				if (bit5  && !bit4)
				{
					_column = 0;
				}

				if (value.Bit(6))
				{
					_column = (_column + 1) % 10;
				}
			}

			if (addr >= 0x1800)
			{
				if (!_disableRam && _writeMode)
				{
					_ram[addr & 0x7FF] = value;
				}
			}

			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
		}
	}
}
