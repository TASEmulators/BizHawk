using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class CartridgeChip
	{
		public int address;
		public int bank;
		public byte[] data;
		public ushort romMask;
		public int type;
	}

	public class Cartridge
	{
		public List<CartridgeChip> chips;
		public bool exRomPin;
		public bool gamePin;
		public int type;
		public bool valid;
		public int version;

		public Cartridge(byte[] rom)
		{
			chips = new List<CartridgeChip>();

			if (rom.Length >= 0x50)
			{
				MemoryStream source = new MemoryStream(rom);
				BinaryReader reader = new BinaryReader(source);
				string idString;

				// note: cartridge files store values big-endian.

				idString = new string(reader.ReadChars(16));
				if (idString == "C64 CARTRIDGE   ")
				{
					int headerLength = 0;
					headerLength = reader.ReadByte();
					headerLength <<= 8;
					headerLength |= reader.ReadByte();
					headerLength <<= 8;
					headerLength |= reader.ReadByte();
					headerLength <<= 8;
					headerLength |= reader.ReadByte();

					version = reader.ReadByte();
					version <<= 8;
					version |= reader.ReadByte();

					type = reader.ReadByte();
					type <<= 8;
					type |= reader.ReadByte();

					if (type != 0x0000)
					{
						// the emulator does not support anything other than type 0 right now
						valid = false;
						return;
					}

					exRomPin = (reader.ReadByte() == 1);
					gamePin = (reader.ReadByte() == 1);

					reader.ReadBytes(6); // reserved
					reader.ReadBytes(32); // name

					// skip the rest, don't need this info
					if (headerLength > 0x40)
					{
						reader.ReadBytes(headerLength - 0x40);
					}

					while (source.Position < rom.Length)
					{
						string chipID = new string(reader.ReadChars(4));

						if (chipID == "CHIP")
						{
							CartridgeChip chip = new CartridgeChip();
                            
							int packetLength;
							packetLength = reader.ReadByte();
							packetLength <<= 8;
							packetLength |= reader.ReadByte();
							packetLength <<= 8;
							packetLength |= reader.ReadByte();
							packetLength <<= 8;
							packetLength |= reader.ReadByte();
							packetLength -= 16;

							chip.type = reader.ReadByte();
							chip.type <<= 8;
							chip.type |= reader.ReadByte();

							chip.bank = reader.ReadByte();
							chip.bank <<= 8;
							chip.bank |= reader.ReadByte();

							chip.address = reader.ReadByte();
							chip.address <<= 8;
							chip.address |= reader.ReadByte();

							int size;
							size = reader.ReadByte();
							size <<= 8;
							size |= reader.ReadByte();

							chip.data = reader.ReadBytes(size);
							chip.romMask = (ushort)(size - 1);

							packetLength -= size;
							if (packetLength > 0)
							{
								// discard extra bytes
								reader.ReadBytes(packetLength);
							}

							chips.Add(chip);
						}
						else
						{
							break;
						}
					}

					valid = (chips.Count > 0);
				}
				reader.Close();
				source.Dispose();
			}
		}

		public byte Read(ushort addr)
		{
			CartridgeChip currentChip = chips[0];
			return currentChip.data[addr & currentChip.romMask];
		}

		public void Write(ushort addr, byte val)
		{
			// can't write to rom but we can process DE00/DF00 here
		}
	}
}
