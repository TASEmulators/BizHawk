namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	FE (Activision special)
	-----

	Activision used this method on only three games:  Decathlon, Robot Tank, and the
	prototype Thwocker.  This mapper is one of the more interesting ones in that it uses
	stack access to select banks.  It is  composed of two 4K banks, similar to F8.  
	Unlike F8, however, switching occurs when the stack is accessed.

	This mapper allows for "automatic" bankswitching to occur, using JSR and RTS.  The
	addresses for all JSRs and RTS' are either Fxxx or Dxxx, and the mapper uses this to
	figure out which bank it should be going to.

	The cycles of a JSR are as such:

	1: opcode fetch
	2: fetch low byte of address
	3: read 100,s  : garbage fetch
	4: write 100,s : PCH, decrement S
	5: write 100,s : PCL, decrement S
	6: fetch high byte of address

	The cycles of an RTS are as such:

	1: opcode fetch
	2: fetch next opcode (and throw it away)
	3: read 100,S : increment S
	4: read 100,S : pull PCL from stack, increment S
	5: read 100,S : pull PCH from stack

	The chip can determine what instruction is being executed by watching the data and
	address bus.

	It watches for 20 (JSR) and 60 (RTS), and accesses to 100-1ff:

	(opcode cycles)

	20        (opcode)
	add low   (new add low)
	stack     (garbage read)
	stack     (push PCH)
	stack     (push PCL)
	add high  (new add hi)    : latch D5.  This is the NEW bank we need to be in.

	60        (opcode)
	xx        (garbage fetch)
	stack
	stack     (pull PCL)
	stack     (pull PCH)      : latch D5.  This is the NEW bank we need to be in.


	Using emulators or similar there is a large cheat that can be used.  A13 can be used
	to simply select which 8K bank to be in.
	*/
	
	class mFE : MapperBase 
	{

	}
}
