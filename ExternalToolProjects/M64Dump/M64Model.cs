using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Experiment.M64Dump
{
	public struct M64Model
	{
		/// <remarks>a strange layout; bits are <c>0bABCDABCDABCDXXXX</c> where the 3 bits A are for gamepad 1, B for gamepad 2, etc.</remarks>
		[StructLayout(LayoutKind.Explicit)]
		public struct GamepadSetupBitfield
		{
			private const ushort MASK_CONNECTED = 0x1;

			private const ushort MASK_MEMPAK = 0x10;

			private const ushort MASK_RUMBLEPAK = 0x100;

			public bool Connected
			{
				get => (Data & MASK_CONNECTED) is not 0;
				set => Data &= value ? MASK_CONNECTED : unchecked((ulong) ~MASK_CONNECTED);
			}

			public ulong Data;

			public bool HasMemPak
			{
				get => (Data & MASK_MEMPAK) is not 0;
				set => Data &= value ? MASK_MEMPAK : unchecked((ulong) ~MASK_MEMPAK);
			}

			public bool HasRumblePak
			{
				get => (Data & MASK_RUMBLEPAK) is not 0;
				set => Data &= value ? MASK_RUMBLEPAK : unchecked((ulong) ~MASK_RUMBLEPAK);
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct GamepadStateBitfield
		{
			private const ushort MASK_RESET = 0x00C0;

			public bool A
			{
				get => GetButton(15);
				set => SetButton(15, value);
			}

			[FieldOffset(2)]
			public sbyte AnalogStickX;

			[FieldOffset(3)]
			public sbyte AnalogStickY;

			public bool B
			{
				get => GetButton(14);
				set => SetButton(14, value);
			}

			[FieldOffset(0)]
			public ushort Buttons;

			public bool CDown
			{
				get => GetButton(2);
				set => SetButton(2, value);
			}

			public bool CLeft
			{
				get => GetButton(1);
				set => SetButton(1, value);
			}

			public bool CRight
			{
				get => GetButton(0);
				set => SetButton(0, value);
			}

			public bool CUp
			{
				get => GetButton(3);
				set => SetButton(3, value);
			}

			[FieldOffset(0)]
			public ulong Data;

			public bool DDown
			{
				get => GetButton(10);
				set => SetButton(10, value);
			}

			public bool DLeft
			{
				get => GetButton(9);
				set => SetButton(9, value);
			}

			public bool DRight
			{
				get => GetButton(8);
				set => SetButton(8, value);
			}

			public bool DUp
			{
				get => GetButton(11);
				set => SetButton(11, value);
			}

			public bool L
			{
				get => GetButton(5);
				set => SetButton(5, value);
			}

			public bool R
			{
				get => GetButton(4);
				set => SetButton(4, value);
			}

			public bool Reset
			{
				get => (Buttons & MASK_RESET) is not 0;
				set => Buttons &= value ? MASK_RESET : unchecked((ushort) ~MASK_RESET);
			}

			public bool Start
			{
				get => GetButton(12);
				set => SetButton(12, value);
			}

			public bool Z
			{
				get => GetButton(13);
				set => SetButton(13, value);
			}

			private bool GetButton(int pos)
				=> (Buttons & (1U << pos)) is not 0;

			private void SetButton(int pos, bool value)
				=> Buttons &= unchecked((ushort) (value ? (1U << pos) : ~(1U << pos)));
		}

		public enum MovieStartType : ushort
		{
			Savestate = 0x01,
			PowerOn = 0x02,
			EEPROM = 0x04,
		}

		private const int HEADER_LENGTH = 0x400;

		public readonly string Author;

		public readonly byte Framerate;

		public readonly IList<GamepadStateBitfield> Latches;

		public readonly IReadOnlyList<GamepadSetupBitfield> Players;

		public readonly uint RerecordCount;

		public readonly byte[] RomHeader;

		public MovieStartType StartType => MovieStartType.PowerOn;

		public readonly uint VBlankCount;

		public M64Model(
			string author,
			uint framerate,
			IReadOnlyList<GamepadSetupBitfield> players,
			uint rerecordCount,
			byte[] romHeader,
			uint vblankCount)
		{
			Author = author;
			Framerate = (byte) framerate;
			Latches = new List<GamepadStateBitfield>();
			Players = players.Count switch
			{
				> 4 => throw new ArgumentException("too many players, max player count is 4", nameof(players)),
				4 => players,
				_ => players.Concat(new GamepadSetupBitfield[4]).Take(4).ToList()
			};
			RerecordCount = rerecordCount;
			if (romHeader.Length < 0x100) throw new ArgumentException("header is first 0x100 bytes", nameof(romHeader));
			RomHeader = romHeader;
			VBlankCount = vblankCount;
		}

		public byte[] Serialise()
		{
			var output = new byte[HEADER_LENGTH + 4 * Latches.Count];
			void WriteAt(int offset, byte[] bytes)
				=> Array.Copy(bytes, 0, output, offset, bytes.Length);
			WriteAt(0x000, new byte[] { 0x4D, 0x36, 0x34, 0x1A, 0x03, 0x00, 0x00, 0x00 }); // magic bytes + schema revision
			WriteAt(0x008, new byte[4]); //TODO "movie 'uid' - identifies the movie-savestate relationship, also used as the recording time in Unix epoch format"; CPP's dump script has this zeroed
			WriteAt(0x00C, BitConverter.GetBytes(VBlankCount));
			WriteAt(0x010, BitConverter.GetBytes(RerecordCount));
			output[0x014] = Framerate;
			output[0x015] = (byte) Players.Count(static bf => bf.Connected);
			WriteAt(0x018, BitConverter.GetBytes(Latches.Count));
			WriteAt(0x01C, BitConverter.GetBytes((ushort) StartType));
			WriteAt(0x020, BitConverter.GetBytes(Players.Select(static (bf, i) => bf.Data << i).Aggregate(static (a, b) => a | b)));
			// see https://n64brew.dev/wiki/ROM_Header
			Array.Copy(RomHeader, 0x20, output, 0x0C4, length: 20);
			Array.Copy(RomHeader, 0x10, output, 0x0E4, length: 4);
			Array.Copy(RomHeader, 0x3E, output, 0x0E8, length: 2);
			WriteAt(0x122, new byte[64]); //TODO ASCII string: name of video plugin used when recording, directly from plugin; CPP's dump script has this zeroed
			WriteAt(0x162, new byte[64]); //TODO ASCII string: name of sound plugin used when recording, directly from plugin; CPP's dump script has this zeroed
			WriteAt(0x1A2, new byte[64]); //TODO ASCII string: name of input plugin used when recording, directly from plugin; CPP's dump script has this zeroed
			WriteAt(0x1E2, new byte[64]); //TODO ASCII string: name of rsp plugin used when recording, directly from plugin; CPP's dump script has this zeroed
			WriteAt(0x222, Encoding.UTF8.GetBytes(Author).Take(222).ToArray()); // 0x222 + 222 = 0x300 cool
			WriteAt(0x300, new byte[256]); //TODO UTF-8 string: author movie description info; CPP's dump script has this zeroed
			for (var i = 0; i < Latches.Count; i++) WriteAt(HEADER_LENGTH + 4 * i, BitConverter.GetBytes(Latches[i].Data));
			return output;
		}
	}
}
