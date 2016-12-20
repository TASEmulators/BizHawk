using BizHawk.Emulation.Cores.Sony.PSX;
using Newtonsoft.Json;
using System;
using System.IO;

namespace BizHawk.Client.Common
{
	[ImportExtension(".pjm")]
	public class PJMImport : MovieImporter
	{
		protected override void RunImport()
		{
			Bk2Movie movie = Result.Movie;
			MiscHeaderInfo info;

			movie.HeaderEntries[HeaderKeys.PLATFORM] = "PSX";

			using (var fs = SourceFile.OpenRead())
			{
				using (var br = new BinaryReader(fs))
				{
					info = parseHeader(movie, "PJM ", br);

					fs.Seek(info.controllerDataOffset, SeekOrigin.Begin);

					if (info.binaryFormat)
					{
						parseBinaryInputLog(br, movie, info);
					}
					else
					{
						parseTextInputLog(br, movie, info);
					}
				}
			}

			movie.Save();
		}

		protected MiscHeaderInfo parseHeader(Bk2Movie movie, string expectedMagic, BinaryReader br)
		{
			var info = new MiscHeaderInfo();

			string magic = new string(br.ReadChars(4));
			if (magic != expectedMagic)
			{
				Result.Errors.Add("Not a " + expectedMagic + "file: invalid magic number in file header.");
				return info;
			}

			UInt32 movieVersionNumber = br.ReadUInt32();
			if (movieVersionNumber != 2)
			{
				Result.Warnings.Add(String.Format("Unexpected movie version: got {0}, expecting 2", movieVersionNumber));
			}

			// 008: UInt32 emulator version.
			br.ReadUInt32();

			byte flags = br.ReadByte();
			byte flags2 = br.ReadByte();
			if ((flags & 0x02) != 0)
			{
				Result.Errors.Add("Movie starts from savestate; this is currently unsupported.");
			}
			if ((flags & 0x04) != 0)
			{
				movie.HeaderEntries[HeaderKeys.PAL] = "1";
			}
			if ((flags & 0x08) != 0)
			{
				Result.Errors.Add("Movie contains embedded memory cards; this is currently unsupported.");
			}
			if ((flags & 0x10) != 0)
			{
				Result.Errors.Add("Movie contains embedded cheat list; this is currently unsupported.");
			}
			if ((flags & 0x20) != 0 || (flags2 & 0x06) != 0)
			{
				Result.Errors.Add("Movie relies on emulator hacks; this is currently unsupported.");
			}
			if ((flags & 0x40) != 0)
			{
				info.binaryFormat = false;
			}
			if ((flags & 0x80) != 0 || (flags2 & 0x01) != 0)
			{
				Result.Errors.Add("Movie uses multitap; this is currently unsupported.");
				return info;
			}

			// Player 1 controller type
			switch (br.ReadByte())
			{
				// It seems to be inconsistent in the files I looked at which of these is used
				// to mean no controller present.
				case 0:
				case 8:
					info.player1Type = OctoshockDll.ePeripheralType.None;
					break;
				case 4:
					info.player1Type = OctoshockDll.ePeripheralType.Pad;
					break;
				case 7:
					info.player1Type = OctoshockDll.ePeripheralType.DualShock;
					break;
				default:
					Result.Errors.Add("Movie has unrecognised controller type for Player 1.");
					return info;
			}

			// Player 2 controller type
			switch (br.ReadByte())
			{
				case 0:
				case 8:
					info.player1Type = OctoshockDll.ePeripheralType.None;
					break;
				case 4:
					info.player1Type = OctoshockDll.ePeripheralType.Pad;
					break;
				case 7:
					info.player1Type = OctoshockDll.ePeripheralType.DualShock;
					break;
				default:
					Result.Errors.Add("Movie has unrecognised controller type for Player 2.");
					return info;
			}

			Octoshock.SyncSettings syncsettings = new Octoshock.SyncSettings();
			syncsettings.FIOConfig.Devices8 = 
				new[] { 
					info.player1Type,
					OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None,
					info.player2Type,
					OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None
				};

			// Annoying kludge to force the json serializer to serialize the type name for "o" object.
			// For just the "o" object to have type information, it must be cast to a superclass such
			// that the TypeNameHandling.Auto decides to serialize the type as well as the object
			// contents.  As such, the object cast is NOT redundant
			var jsonSettings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto
			};
			movie.SyncSettingsJson = JsonConvert.SerializeObject(new { o = (object)syncsettings }, jsonSettings);

			info.frameCount = br.ReadUInt32();
			UInt32 rerecordCount = br.ReadUInt32();
			movie.HeaderEntries[HeaderKeys.RERECORDS] = rerecordCount.ToString();

			// 018: UInt32 savestateOffset
			// 01C: UInt32 memoryCard1Offset
			// 020: UInt32 memoryCard2Offset
			// 024: UInt32 cheatListOffset

			// 028: UInt32 cdRomIdOffset
			// Source format is just the first up-to-8 alphanumeric characters of the CD label, 
			// so not so useful.

			br.ReadBytes(20);

			info.controllerDataOffset = br.ReadUInt32();

			UInt32 authorNameLength = br.ReadUInt32();
			char[] authorName = br.ReadChars((int)authorNameLength);

			movie.HeaderEntries[HeaderKeys.AUTHOR] = new string(authorName);

			info.parseSuccessful = true;
			return info;
		}

		protected void parseBinaryInputLog(BinaryReader br, Bk2Movie movie, MiscHeaderInfo info)
		{
			Octoshock.SyncSettings settings = new Octoshock.SyncSettings();
			SimpleController controllers = new SimpleController();
			settings.FIOConfig.Devices8 =
				new[] { 
					info.player1Type,
					OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None,
					info.player2Type,
					OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None
				};
			controllers.Definition = Octoshock.CreateControllerDefinition(settings);

			string[] buttons = { "Select", "L3", "R3", "Start", "Up", "Right", "Down", "Left",
									"L2", "R2", "L1", "R1", "Triangle", "Circle", "Cross", "Square"};

			bool isCdTrayOpen = false;
			int cdNumber = 1;

			for (int frame = 0; frame < info.frameCount; ++frame)
			{
				if (info.player1Type != OctoshockDll.ePeripheralType.None)
				{
					UInt16 controllerState = br.ReadUInt16();

					// As L3 and R3 don't exist on a standard gamepad, handle them separately later.  Unfortunately
					// due to the layout, we handle select separately too first.
					controllers["P1 Select"] = (controllerState & 0x1) != 0;

					for (int button = 3; button < buttons.Length; button++)
					{
						controllers["P1 " + buttons[button]] = (((controllerState >> button) & 0x1) != 0);
						if (((controllerState >> button) & 0x1) != 0 && button > 15)
						{
							continue;
						}
					}

					if (info.player1Type == OctoshockDll.ePeripheralType.DualShock)
					{
						controllers["P1 L3"] = (controllerState & 0x2) != 0;
						controllers["P1 R3"] = (controllerState & 0x4) != 0;
						Tuple<string, float> leftX = new Tuple<string, float>("P1 LStick X", (float)br.ReadByte());
						Tuple<string, float> leftY = new Tuple<string, float>("P1 LStick Y", (float)br.ReadByte());
						Tuple<string, float> rightX = new Tuple<string, float>("P1 RStick X", (float)br.ReadByte());
						Tuple<string, float> rightY = new Tuple<string, float>("P1 RStick Y", (float)br.ReadByte());

						controllers.AcceptNewFloats(new[] { leftX, leftY, rightX, rightY });
					}
				}

				if (info.player2Type != OctoshockDll.ePeripheralType.None)
				{
					UInt16 controllerState = br.ReadUInt16();
					for (int button = 0; button < buttons.Length; button++)
					{
						controllers["P2 " + buttons[button]] = (((controllerState >> button) & 0x1) != 0);
						if (((controllerState >> button) & 0x1) != 0 && button > 15)
						{
							continue;
						}
					}

					if (info.player2Type == OctoshockDll.ePeripheralType.DualShock)
					{
						Tuple<string, float> leftX = new Tuple<string, float>("P2 LStick X", (float)br.ReadByte());
						Tuple<string, float> leftY = new Tuple<string, float>("P2 LStick Y", (float)br.ReadByte());
						Tuple<string, float> rightX = new Tuple<string, float>("P2 RStick X", (float)br.ReadByte());
						Tuple<string, float> rightY = new Tuple<string, float>("P2 RStick Y", (float)br.ReadByte());

						controllers.AcceptNewFloats(new[] { leftX, leftY, rightX, rightY });
					}
				}

				byte controlState = br.ReadByte();
				controllers["Reset"] = (controlState & 0x02) != 0;
				if ((controlState & 0x04) != 0)
				{
					if (isCdTrayOpen)
					{
						controllers["Close"] = true;
						cdNumber++;
					}
					else
					{
						controllers["Open"] = true;
					}
					isCdTrayOpen = !isCdTrayOpen;
				}
				else
				{
					controllers["Close"] = false;
					controllers["Open"] = false;
				}

				Tuple<string, float> discSelect = new Tuple<string, float>("Disc Select", cdNumber);
				controllers.AcceptNewFloats(new[] { discSelect });

				if ((controlState & 0xFC) != 0)
				{
					Result.Warnings.Add("Ignored toggle hack flag on frame " + frame.ToString());
				}

				movie.AppendFrame(controllers);
			}
		}

		protected void parseTextInputLog(BinaryReader br, Bk2Movie movie, MiscHeaderInfo info)
		{
			Octoshock.SyncSettings settings = new Octoshock.SyncSettings();
			SimpleController controllers = new SimpleController();
			settings.FIOConfig.Devices8 =
				new[] { 
					info.player1Type,
					OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None,
					info.player2Type,
					OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None,OctoshockDll.ePeripheralType.None
				};
			controllers.Definition = Octoshock.CreateControllerDefinition(settings);

			string[] buttons = { "Select", "L3", "R3", "Start", "Up", "Right", "Down", "Left",
			                     "L2", "R2", "L1", "R1", "Triangle", "Circle", "Cross", "Square"};

			bool isCdTrayOpen = false;
			int cdNumber = 1;

			for (int frame = 0; frame < info.frameCount; ++frame)
			{
				if (info.player1Type != OctoshockDll.ePeripheralType.None)
				{
					// As L3 and R3 don't exist on a standard gamepad, handle them separately later.  Unfortunately
					// due to the layout, we handle select separately too first.
					controllers["P1 Select"] = br.ReadChar() != '.';

					if (info.player1Type == OctoshockDll.ePeripheralType.DualShock)
					{
						controllers["P1 L3"] = br.ReadChar() != '.';
						controllers["P1 R3"] = br.ReadChar() != '.';
					}

					for (int button = 3; button < buttons.Length; button++)
					{
						controllers["P1 " + buttons[button]] = br.ReadChar() != '.';
					}

					if (info.player1Type == OctoshockDll.ePeripheralType.DualShock)
					{
						// The analog controls are encoded as four space-separated numbers with a leading space
						string leftXRaw = new string(br.ReadChars(4)).Trim();
						string leftYRaw = new string(br.ReadChars(4)).Trim();
						string rightXRaw = new string(br.ReadChars(4)).Trim();
						string rightYRaw = new string(br.ReadChars(4)).Trim();


						Tuple<string, float> leftX = new Tuple<string, float>("P1 LStick X", float.Parse(leftXRaw));
						Tuple<string, float> leftY = new Tuple<string, float>("P1 LStick Y", float.Parse(leftYRaw));
						Tuple<string, float> rightX = new Tuple<string, float>("P1 RStick X", float.Parse(rightXRaw));
						Tuple<string, float> rightY = new Tuple<string, float>("P1 RStick Y", float.Parse(rightYRaw));

						controllers.AcceptNewFloats(new[] { leftX, leftY, rightX, rightY });
					}
				}

				// Each controller is terminated with a pipeline.
				br.ReadChar();

				if (info.player2Type != OctoshockDll.ePeripheralType.None)
				{
					// As L3 and R3 don't exist on a standard gamepad, handle them separately later.  Unfortunately
					// due to the layout, we handle select separately too first.
					controllers["P2 Select"] = br.ReadChar() != '.';

					if (info.player2Type == OctoshockDll.ePeripheralType.DualShock)
					{
						controllers["P2 L3"] = br.ReadChar() != '.';
						controllers["P2 R3"] = br.ReadChar() != '.';
					}

					for (int button = 3; button < buttons.Length; button++)
					{
						controllers["P2 " + buttons[button]] = br.ReadChar() != '.';
					}

					if (info.player2Type == OctoshockDll.ePeripheralType.DualShock)
					{
						// The analog controls are encoded as four space-separated numbers with a leading space
						string leftXRaw = new string(br.ReadChars(4)).Trim();
						string leftYRaw = new string(br.ReadChars(4)).Trim();
						string rightXRaw = new string(br.ReadChars(4)).Trim();
						string rightYRaw = new string(br.ReadChars(4)).Trim();


						Tuple<string, float> leftX = new Tuple<string, float>("P2 LStick X", float.Parse(leftXRaw));
						Tuple<string, float> leftY = new Tuple<string, float>("P2 LStick Y", float.Parse(leftYRaw));
						Tuple<string, float> rightX = new Tuple<string, float>("P2 RStick X", float.Parse(rightXRaw));
						Tuple<string, float> rightY = new Tuple<string, float>("P2 RStick Y", float.Parse(rightYRaw));

						controllers.AcceptNewFloats(new[] { leftX, leftY, rightX, rightY });
					}
				}

				// Each controller is terminated with a pipeline.
				br.ReadChar();

				byte controlState = br.ReadByte();
				controllers["Reset"] = (controlState & 0x02) != 0;
				if ((controlState & 0x04) != 0)
				{
					if (isCdTrayOpen)
					{
						controllers["Close"] = true;
						cdNumber++;
					}
					else
					{
						controllers["Open"] = true;
					}
					isCdTrayOpen = !isCdTrayOpen;
				}
				else
				{
					controllers["Close"] = false;
					controllers["Open"] = false;
				}

				Tuple<string, float> discSelect = new Tuple<string, float>("Disc Select", cdNumber);
				controllers.AcceptNewFloats(new[] { discSelect });

				if ((controlState & 0xFC) != 0)
				{
					Result.Warnings.Add("Ignored toggle hack flag on frame " + frame.ToString());
				}

				// Each controller is terminated with a pipeline.
				br.ReadChar();

				movie.AppendFrame(controllers);
			}
		}

		protected class MiscHeaderInfo
		{
			public bool binaryFormat = true;
			public UInt32 controllerDataOffset;
			public UInt32 frameCount;
			public OctoshockDll.ePeripheralType player1Type;
			public OctoshockDll.ePeripheralType player2Type;

			public bool parseSuccessful = false;
		}

	}
}
