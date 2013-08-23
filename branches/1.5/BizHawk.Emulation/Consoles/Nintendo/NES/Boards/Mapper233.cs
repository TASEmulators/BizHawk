namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper233 : NES.NESBoardBase
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 233          =
		========================


		Example Game:
		--------------------------
		???? "42-in-1"  ????


		Notes:
		---------------------------
		Sources report this mapper as "42-in-1" with description layed out below.  I did not test this,
		since I could not find a copy of the ROM in question.  The only ROM I have that's marked as
		233 is "Unknown Multicart 1", and it does *not* follow the description in this doc at all.

		There is a "Super 42-in-1"... but that is mapper 226.  226, by the way, is strikingly similar
		to the below description.  I wonder if below description really applies to 233?



		Registers:
		---------------------------

		$8000-FFFF:  [MMOP PPPP]
		M = Mirroring
		O = PRG Mode
		P = PRG Page


		PRG Setup:
		---------------------------

					  $8000   $A000   $C000   $E000  
					+-------------------------------+
		PRG Mode 0: |            <$8000>            |
					+-------------------------------+
		PRG Mode 1: |     $8000     |     $8000     |
					+---------------+---------------+


		Mirroring:
		---------------------------

		'M' mirroring bits:
		%00 = See below
		%01 = Vert
		%10 = Horz
		%11 = 1ScB


		Mode %00 (almost, but not quite 1ScA):
		[  NTA  ][  NTA  ]
		[  NTA  ][  NTB  ]
		*/

		public int prg_page;
		public bool prg_mode;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER233":
					break;
				default:
					return false;
			}

			prg_mode = false;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_page", ref prg_page);
			ser.Sync("prg_mode", ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			prg_page = value & 0x1F;
			prg_mode = value.Bit(5);

			int mirror = value >> 6;
			switch (mirror)
			{
				case 0:
					SetMirroring(0, 0, 0, 1);
					break;
				case 1:
					SetMirrorType(EMirrorType.Vertical);
					break;
				case 2:
					SetMirrorType(EMirrorType.Horizontal);
					break;
				case 3:
					SetMirrorType(EMirrorType.OneScreenB);
					break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			if (prg_mode == false)
			{
				return ROM[((prg_page >> 1) * 0x8000) + addr];
			}
			else
			{
				return ROM[(prg_page * 0x4000) + (addr & 0x3FFF)];
			}
		}
	}
}
