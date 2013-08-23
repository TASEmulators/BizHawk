namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	MC (Megacart)
	-----

	This is the mapper for the "Chris Wilkson's Megacart".  

	Only four addresses are used to bankswitch on this one.

	Up to 128K of ROM and 64K of RAM can be accessed.

	1000-13FF is selected by address 3C
	1400-17FF is selected by address 3D
	1800-1BFF is selected by address 3E
	1C00-1FFF is selected by address 3F

	The value written determines what will be selected:

	00-7F written will select one of the 128 1K ROM banks
	80-FF written will select one of the 128 512 byte RAM banks

	When a RAM bank is selected, the lower 512 bytes is the write port, while
	the upper 512 bytes is the read port.

	On accessing address FFFC or FFFD, the last 1K bank points to the last bank in ROM,
	to allow for system initialization.  Jumping out of the last bank disables this.
	It's debatable how easy this system would be to implement on a real system.

	Detecting when to disable the last bank fixing is difficult.  The documentation
	says:

	"
	  Megacart Specification, Rev1.1
	  (c) 1997 Chris Wilkson
	  cwilkson@mit.edu

	  Because the console's memory is randomized at powerup, there is no way to
	  predict the data initially contained in the "hot addresses".  Therefore,
	  hardware will force slot 3 to always point to ROM block $FF immediately
	  after any read or write to the RESET vector at $FFFC-$FFFD.  Block $FF
	  must contain code to initialize the 4 memory slots to point to the desired
	  physical memory blocks before any other code can be executed.  After program
	  execution jumps out of the boot code, the hardware will release slot 3 and
	  it will function just like any other slot.
	"

	Unfortunately, there's not an easy way to detect this.  Just watching the address
	bus won't work easily: Writing anywhere outside the bank 1C00-1FFF (i.e. bank
	registers, RAM, TIA registers) will cause the switching to revert bank 3, crashing
	the system.

	The only way I can see it working is to disregard any access to addresses 3C-3F.

	Emulators have it easier: they can simply watch the program counter, vs. the 
	address bus.  An actual system doesn't have that luxury, unfortunately, so it must
	disregard accesses to 3C-3F instead.
	*/

	class mMC : MapperBase 
	{

	}
}
