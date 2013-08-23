namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	This is another 8K bankswitching method with two 4K banks.  The rationale is that it's
	cheap and easy to implement with only a single 74HC153 or 253 dual 4:1 multiplexer.

	This multiplexer can act as a 1 bit latch AND the inverter for A12.

	To bankswitch, the following mask it used:

	A13           A0
	----------------
	0 1xxx xBxx xxxx

	Each bit corresponds to one of the 13 address lines.  a 0 or 1 means that bit must be
	0 or 1 to trigger the bankswitch. x is a bit that is not concidered (it can be either
	0 or 1 and is thus a "don't care" bit).

	B is the bank we will select.  sooo, accessing 0800 will select bank 0, and 0840
	will select bank 1.
	*/
	class m0840 : MapperBase 
	{

	}
}
