using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	[ImportExtension(".pjm")]
	public class PJMImport : MovieImporter
	{
        protected override void RunImport()
		{
            Bk2Movie movie = Result.Movie;
            MiscHeaderInfo info;

            movie.HeaderEntries.Add(HeaderKeys.PLATFORM, "PSX");

            using (var fs = SourceFile.OpenRead()) {
                using (var br = new BinaryReader(fs)) {
                    info = parseHeader(movie, br);

                    fs.Seek(info.controllerDataOffset, SeekOrigin.Begin);

                    if(info.binaryFormat) {
                        parseBinaryInputLog(br, movie, info);
                        return;
                    }
                }

                if (!info.parseSuccessful) {
                    return;
                }

                using (var sr = new StreamReader(fs)) {
                    parseTextInputLog(sr, movie, info);
                }
            }
		}

        private MiscHeaderInfo parseHeader(Bk2Movie movie, BinaryReader br) {
            var info = new MiscHeaderInfo();

            string magic = new string(br.ReadChars(4));
            if (magic != "PJM ") {
                Result.Errors.Add("Not a PJM file: invalid magic number in file header.");
                return info;
            }

            UInt32 movieVersionNumber = br.ReadUInt32();
            if (movieVersionNumber != 2) {
                Result.Warnings.Add(String.Format("Unexpected PJM format version: got {0}, expecting 2", movieVersionNumber));
            }

            // 008: UInt32 emulator version.
            br.ReadUInt32();

            byte flags = br.ReadByte();
            byte flags2 = br.ReadByte();
            if ((flags & 0x02) != 0) {
                Result.Errors.Add("PJM file starts from savestate; this is currently unsupported.");
            }
            if ((flags & 0x04) != 0) {
                movie.HeaderEntries.Add(HeaderKeys.PAL, "1");
            }
            if ((flags & 0x08) != 0) {
                Result.Errors.Add("PJM file contains embedded memory cards; this is currently unsupported.");
            }
            if ((flags & 0x10) != 0) {
                Result.Errors.Add("PJM file contains embedded cheat list; this is currently unsupported.");
            }
            if ((flags & 0x20) != 0 || (flags2 & 0x06) != 0) {
                Result.Errors.Add("PJM file relies on emulator hacks; this is currently unsupported.");
            }
            if ((flags & 0x40) != 0) {
                info.binaryFormat = false;
            }
            if ((flags & 0x80) != 0 || (flags2 & 0x01) != 0) {
                Result.Errors.Add("PJM file uses multitap; this is currently unsupported.");
                return info;
            }

            switch (br.ReadByte()) {
                case 0:
                    info.player1Type.IsConnected = false;
                    break;
                case 4:
                    info.player1Type.Type = Octoshock.ControllerSetting.ControllerType.Gamepad;
                    break;
                case 7:
                    info.player1Type.Type = Octoshock.ControllerSetting.ControllerType.DualShock;
                    break;
                default:
                    Result.Errors.Add("PJM file has unrecognised controller type for Player 1.");
                    return info;
            }

            switch (br.ReadByte()) {
                case 0:
                    info.player2Type.IsConnected = false;
                    break;
                case 4:
                    info.player2Type.Type = Octoshock.ControllerSetting.ControllerType.Gamepad;
                    break;
                case 7:
                    info.player2Type.Type = Octoshock.ControllerSetting.ControllerType.DualShock;
                    break;
                default:
                    Result.Errors.Add("PJM file has unrecognised controller type for Player 2.");
                    return info;
            }

            info.frameCount = br.ReadUInt32();
            UInt32 rerecordCount = br.ReadUInt32();
            movie.HeaderEntries.Add(HeaderKeys.RERECORDS, rerecordCount.ToString());

            // 018: UInt32 savestateOffset
            br.ReadUInt32();

            // 01C: UInt32 memoryCard1Offset
            br.ReadUInt32();

            // 020: UInt32 memoryCard2Offset
            br.ReadUInt32();

            // 024: UInt32 cheatListOffset
            br.ReadUInt32();

            // 028: UInt32 cdRomIdOffset
            // Source format is just the first up-to-8 alphanumeric characters of the CD label, 
            // so not so useful.
            br.ReadUInt32();

            info.controllerDataOffset = br.ReadUInt32();

            UInt32 authorNameLength = br.ReadUInt32();
            char[] authorName = br.ReadChars((int)authorNameLength);

            movie.HeaderEntries.Add(HeaderKeys.AUTHOR, new string(authorName));

            info.parseSuccessful = true;
            return info;
        }

        private void parseBinaryInputLog(BinaryReader br, Bk2Movie movie, MiscHeaderInfo info) {
            Octoshock.SyncSettings settings = new Octoshock.SyncSettings();
            SimpleController controllers = new SimpleController();
            settings.Controllers = new[] { info.player1Type, info.player2Type };
            controllers.Type = Octoshock.CreateControllerDefinition(settings);

            string[] buttons = { "Select", "L3", "R3", "Start", "Up", "Right", "Down", "Left",
                                    "L2", "R2", "L1", "R1", "Triangle", "Circle", "Cross", "Square"};

            bool isCdTrayOpen = false;

            for (int frame = 0; frame < movie.FrameCount; ++frame) {
                if (info.player1Type.IsConnected) {
                    UInt16 controllerState = br.ReadUInt16();
                    for (int button = 0; button < buttons.Length; button++) {
                        controllers["P1 " + buttons[button]] = (((controllerState >> button) & 0x1) != 0);
                        if (((controllerState >> button) & 0x1) != 0 && button > 15) {
                            continue;
                        }
                    }

                    if(info.player1Type.Type != Octoshock.ControllerSetting.ControllerType.Gamepad) {
                        Tuple<string, float> leftX = new Tuple<string, float>("P1 LStick X", (float)br.ReadByte());
                        Tuple<string, float> leftY = new Tuple<string, float>("P1 LStick Y", (float)br.ReadByte());
                        Tuple<string, float> rightX = new Tuple<string, float>("P1 RStick X", (float)br.ReadByte());
                        Tuple<string, float> rightY = new Tuple<string, float>("P1 RStick Y", (float)br.ReadByte());

                        controllers.AcceptNewFloats(new[] { leftX, leftY, rightX, rightY });
                    }
                }

                if (info.player2Type.IsConnected) {
                    UInt16 controllerState = br.ReadUInt16();
                    for (int button = 0; button < buttons.Length; button++) {
                        controllers["P2 " + buttons[button]] = (((controllerState >> button) & 0x1) != 0);
                        if (((controllerState >> button) & 0x1) != 0 && button > 15) {
                            continue;
                        }
                    }

                    if (info.player2Type.Type != Octoshock.ControllerSetting.ControllerType.Gamepad) {
                        Tuple<string, float> leftX = new Tuple<string, float>("P2 LStick X", (float)br.ReadByte());
                        Tuple<string, float> leftY = new Tuple<string, float>("P2 LStick Y", (float)br.ReadByte());
                        Tuple<string, float> rightX = new Tuple<string, float>("P2 RStick X", (float)br.ReadByte());
                        Tuple<string, float> rightY = new Tuple<string, float>("P2 RStick Y", (float)br.ReadByte());

                        controllers.AcceptNewFloats(new[] { leftX, leftY, rightX, rightY });
                    }
                }

                byte controlState = br.ReadByte();
                controllers["Reset"] = (controlState & 0x02) != 0;
                if((controlState & 0x04) != 0) {
                    if(isCdTrayOpen) {
                        controllers["Close"] = true;
                    } else {
                        controllers["Open"] = true;
                    }
                    isCdTrayOpen = !isCdTrayOpen;
                } else {
                    controllers["Close"] = false;
                    controllers["Open"] = false;
                }

                if((controlState & 0xFC) != 0) {
                    Result.Warnings.Add("Ignored toggle hack flag on frame " + frame.ToString());
                }

                movie.AppendFrame(controllers);
            }
        }

        private void parseTextInputLog(TextReader tr, Bk2Movie movie, MiscHeaderInfo info) {
            throw new NotImplementedException();
        }

        private class MiscHeaderInfo {
            public bool binaryFormat;
            public UInt32 controllerDataOffset;
            public UInt32 frameCount;
            public Octoshock.ControllerSetting player1Type = new Octoshock.ControllerSetting() { IsConnected = true };
            public Octoshock.ControllerSetting player2Type = new Octoshock.ControllerSetting() { IsConnected = true };

            public bool parseSuccessful = false;
        }

    }
}
