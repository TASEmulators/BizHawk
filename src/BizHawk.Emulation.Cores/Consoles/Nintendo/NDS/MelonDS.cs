using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.MelonDS, "Arisotura", "1.0+", "https://melonds.kuribo64.net/")]
	[ServiceNotApplicable(typeof(IRegionable))]
	public sealed partial class NDS : WaterboxCore
	{
		private readonly LibMelonDS _core;
		private readonly IntPtr _console;
		private readonly NDSDisassembler _disassembler;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly LibMelonDS.LogCallback _logCallback;

		private bool loggingShouldInsertLevelNextCall = true;

		private void LogCallback(LibMelonDS.LogLevel level, string message)
		{
			if (loggingShouldInsertLevelNextCall)
			{
				Console.Write($"[{level}] ");
				loggingShouldInsertLevelNextCall = false;
			}
			Console.Write(message);
			if (message.EndsWith('\n')) loggingShouldInsertLevelNextCall = true;
		}

		private readonly MelonDSGLTextureProvider _glTextureProvider;
		private readonly IOpenGLProvider _openGLProvider;
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly LibMelonDS.GetGLProcAddressCallback _getGLProcAddressCallback;
		private object _glContext;

		private IntPtr GetGLProcAddressCallback(string proc)
			=> _openGLProvider.GetGLProcAddress(proc);

		// TODO: Probably can make these into an interface (ITouchScreen with UntransformPoint/TransformPoint methods?)
		// Which case the hackiness of the current screen controls wouldn't be as bad
		public Vector2 GetTouchCoords(int x, int y)
		{
			if (_glContext != null)
			{
				_core.GetTouchCoords(ref x, ref y);
			}
			else
			{
				// no GL context, so nothing fancy can be applied
				y -= 192;
			}

			return new(x, y);
		}

		public Vector2 GetScreenCoords(float x, float y)
		{
			if (_glContext != null)
			{
				_core.GetScreenCoords(ref x, ref y);
			}
			else
			{
				// no GL context, so nothing fancy can be applied
				y += 192;
			}

			return new(x, y);
		}

		private static readonly BigInteger GENERATOR_CONSTANT = BigInteger.Parse("0FFFEFB4E295902582A680F5F1A4F3E79", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		private static readonly BigInteger U128_MAX = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture);

		[CoreConstructor(VSystemID.Raw.NDS)]
		public NDS(CoreLoadParameters<NDSSettings, NDSSyncSettings> lp)
			: base(lp.Comm, new()
			{
				DefaultWidth = 256,
				DefaultHeight = 384,
				MaxWidth = 256 * 3 + (128 * 4 / 3) + 1,
				MaxHeight = (384 / 2) * 2 + 128,
				MaxSamples = 4096, // rather large max samples is intentional, see comment in ThreadStartCallback
				DefaultFpsNumerator = 33513982,
				DefaultFpsDenominator = 560190,
				SystemId = VSystemID.Raw.NDS,
			})
		{
			try
			{
				_syncSettings = lp.SyncSettings ?? new();
				_settings = lp.Settings ?? new();

				_activeSyncSettings = _syncSettings.Clone();

				IsDSi = _activeSyncSettings.UseDSi;

				var roms = lp.Roms.Select(r => r.FileData).ToList();

				// make sure we have a valid header before doing any parsing
				if (roms[0].Length < 0x1000)
				{
					throw new InvalidOperationException("ROM is too small to be a valid NDS ROM!");
				}

				ReadOnlySpan<byte> romHeader = roms[0].AsSpan(0, 0x1000);
				ReadOnlySpan<byte> dsiWare = [ ], bios7i = [ ];
				if (!IsRomValid(roms[0]))
				{
					// if the ROM isn't valid, this could potentially be a backup TAD the user is attempting to load
					// backup TADs are DSiWare exported to the SD Card
					// https://problemkaputt.de/gbatek.htm#dsisdmmcdsiwarefilesonexternalsdcardbinakatadfiles
					bios7i = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7i"));
					var fixTadKeyX = bios7i.Slice(0xB588, 0x10);
					var fixTadKeyY = bios7i.Slice(0x8328, 0x10);
					var varTadKeyY = bios7i.Slice(0x8318, 0x10);

					// CCM is used to encrypt backup TAD files
					// For purposes of decryption, this is just CTR really, as we can more or less ignore authentication
					// CTR isn't implemented by C# AES of course... so have to reimplement it
					// Note: Not a copy paste of N3DSHasher! DSi is weird and transforms each block in little endian rather than big endian
					static void AesCtrTransform(Aes aes, byte[] iv, Span<byte> inputOutput)
					{
						// ECB encryptor is used for both CTR encryption and decryption
						using var encryptor = aes.CreateEncryptor();
						var blockSize = aes.BlockSize / 8;
						var outputBlockBuffer = new byte[blockSize];

						// mostly copied from tiny-AES-c (public domain)
						for (int i = 0, bi = blockSize; i < inputOutput.Length; ++i, ++bi)
						{
							if (bi == blockSize)
							{
								encryptor.TransformBlock(iv, 0, iv.Length, outputBlockBuffer, 0);
								for (bi = blockSize - 1; bi >= 0; --bi)
								{
									if (iv[bi] == 0xFF)
									{
										iv[bi] = 0;
										continue;
									}

									++iv[bi];
									break;
								}

								bi = 0;
							}

							inputOutput[i] ^= outputBlockBuffer[blockSize - 1 - bi];
						}
					}

					static byte[] DeriveNormalKey(ReadOnlySpan<byte> keyX, ReadOnlySpan<byte> keyY)
					{
						static BigInteger LeftRot128(BigInteger v, int rot)
						{
							var l = (v << rot) & U128_MAX;
							var r = v >> (128 - rot);
							return l | r;
						}

						static BigInteger Add128(BigInteger v1, BigInteger v2)
							=> (v1 + v2) & U128_MAX;

						var keyBytes = new byte[17];
						keyX.CopyTo(keyBytes);
						var keyXBigInteger = new BigInteger(keyBytes);

						keyY.CopyTo(keyBytes);
						var keyYBigInteger = new BigInteger(keyBytes);

						var normalKey = LeftRot128(Add128(keyXBigInteger ^ keyYBigInteger, GENERATOR_CONSTANT), 42);
						var normalKeyBytes = normalKey.ToByteArray();
						if (normalKeyBytes.Length > 17)
						{
							// this shoudn't ever happen
							throw new InvalidOperationException();
						}

						Array.Resize(ref normalKeyBytes, 16);
						Array.Reverse(normalKeyBytes);
						return normalKeyBytes;
					}

					static void InitIv(Span<byte> iv, ReadOnlySpan<byte> nonce)
					{
						iv[0] = 0x02;
						// ES block nonce
						for (var i = 0; i < 12; i++)
						{
							iv[1 + i] = nonce[11 - i];
						}

						iv[13] = 0x00;
						iv[14] = 0x00;
						iv[15] = 0x01;
					}

					using var aes = Aes.Create();
					aes.Mode = CipherMode.ECB;
					aes.Padding = PaddingMode.None;
					aes.BlockSize = 128;
					aes.KeySize = 128;
					var iv = new byte[16];

					// first decrypt the header (mainly to verify this is in fact a backup TAD)
					aes.Key = DeriveNormalKey(fixTadKeyX, fixTadKeyY);
					InitIv(iv, roms[0].AsSpan(0x4020 + 0xB4 + 0x11, 12));
					AesCtrTransform(aes, iv, roms[0].AsSpan(0x4020, 0x30));

					if (!roms[0].AsSpan(0x4020, 4).SequenceEqual("4ANT"u8))
					{
						// not a backup TAD, this is a garbage NDS anyways
						throw new InvalidOperationException("Invalid ROM");
					}

					// these include the ES block footer
					var tmdSize = BinaryPrimitives.ReadUInt32LittleEndian(roms[0].AsSpan(0x4020 + 0x28, 4));
					var appSize = BinaryPrimitives.ReadUInt32LittleEndian(roms[0].AsSpan(0x4020 + 0x2C, 4));

					if (appSize % 0x20020 < 0x20)
					{
						throw new InvalidOperationException("Invalid ROM");
					}

					// decrypt cert area now (to fetch the console unique id, which Nintendo mistakenly includes in here)
					InitIv(iv, roms[0].AsSpan(0x40F4 + 0x440 + 0x11, 12));
					AesCtrTransform(aes, iv, roms[0].AsSpan(0x40F4, 0x440));

					var consoleIdStr = Encoding.ASCII.GetString(roms[0].AsSpan(0x40F4 + 0x38F, 16));
					if (!ulong.TryParse(consoleIdStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var consoleId))
					{
						throw new InvalidOperationException("Failed to parse console ID in TAD TWL cert!");
					}

					Span<byte> varTadKeyX = stackalloc byte[16];
					BinaryPrimitives.WriteUInt32LittleEndian(varTadKeyX, 0x4E00004A);
					BinaryPrimitives.WriteUInt32LittleEndian(varTadKeyX[4..], 0x4A00004E);
					BinaryPrimitives.WriteUInt32LittleEndian(varTadKeyX[8..], (uint)(consoleId >> 32) ^ 0xC80C4B72);
					BinaryPrimitives.WriteUInt32LittleEndian(varTadKeyX[12..], (uint)(consoleId & 0xFFFFFFFF));

					// decrypt app area now
					aes.Key = DeriveNormalKey(varTadKeyX, varTadKeyY);
					var appStart = (int)(0x4554 + tmdSize);
					var appEnd = appStart + (int)appSize;
					var appOffset = 0;
					var appDataOffset = 0;
					while (appStart + appOffset < appEnd)
					{
						var esBlockFooterOffset = (int)Math.Min(0x20020, appSize - appOffset) - 0x20;
						InitIv(iv, roms[0].AsSpan(appStart + appOffset + esBlockFooterOffset + 0x11, 12));
						AesCtrTransform(aes, iv, roms[0].AsSpan(appStart + appOffset, esBlockFooterOffset));
						appOffset += esBlockFooterOffset + 0x20;
						appDataOffset += esBlockFooterOffset;
					}

					var appData = new byte[appDataOffset];
					appOffset = 0;
					appDataOffset = 0;
					while (appStart + appOffset < appEnd)
					{
						var esBlockFooterOffset = (int)Math.Min(0x20020, appSize - appOffset) - 0x20;
						roms[0].AsSpan(appStart + appOffset, esBlockFooterOffset).CopyTo(appData.AsSpan(appDataOffset));
						appOffset += esBlockFooterOffset + 0x20;
						appDataOffset += esBlockFooterOffset;
					}

					dsiWare = appData;
					if (dsiWare.Length < 0x1000 || !IsRomValid(dsiWare))
					{
						throw new InvalidOperationException("Invalid ROM in TAD");
					}

					romHeader = dsiWare[..0x1000];
					DSiTitleId = GetDSiTitleId(romHeader);
					if (!IsDSiWare)
					{
						throw new InvalidOperationException("Backup TAD did not have DSiWare");
					}
				}

				DSiTitleId = GetDSiTitleId(romHeader);
				IsDSi |= IsDSiWare;

				if (roms.Count > (IsDSi ? 1 : 2))
				{
					throw new InvalidOperationException("Wrong number of ROMs!");
				}

				InitMemoryCallbacks();

				_traceCallback = MakeTrace;
				_threadStartCallback = ThreadStartCallback;

				_logCallback = LogCallback;

				_openGLProvider = CoreComm.OpenGLProvider;
				_getGLProcAddressCallback = GetGLProcAddressCallback;

				if (lp.DeterministicEmulationRequested)
				{
					_activeSyncSettings.ThreeDeeRenderer = NDSSyncSettings.ThreeDeeRendererType.Software;
				}

				if (_activeSyncSettings.ThreeDeeRenderer != NDSSyncSettings.ThreeDeeRendererType.Software)
				{
					// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
					var (majorGlVersion, minorGlVersion) = _activeSyncSettings.ThreeDeeRenderer switch
					{
						NDSSyncSettings.ThreeDeeRendererType.OpenGL_Classic => (3, 2),
						NDSSyncSettings.ThreeDeeRendererType.OpenGL_Compute => (4, 3),
						_ => throw new InvalidOperationException($"Invalid {nameof(NDSSyncSettings.ThreeDeeRenderer)}")
					};

					if (!_openGLProvider.SupportsGLVersion(majorGlVersion, minorGlVersion))
					{
						lp.Comm.Notify($"OpenGL {majorGlVersion}.{minorGlVersion} is not supported on this machine, falling back to software renderer");
						_activeSyncSettings.ThreeDeeRenderer = NDSSyncSettings.ThreeDeeRendererType.Software;
					}
					else
					{
						_glContext = _openGLProvider.RequestGLContext(majorGlVersion, minorGlVersion, true);
						// reallocate video buffer for scaling
						if (_activeSyncSettings.GLScaleFactor > 1)
						{
							var maxWidth = (256 * _activeSyncSettings.GLScaleFactor) * 3 + ((128 * _activeSyncSettings.GLScaleFactor) * 4 / 3) + 1;
							var maxHeight = (384 / 2 * _activeSyncSettings.GLScaleFactor) * 2 + (128 * _activeSyncSettings.GLScaleFactor);
							_videoBuffer = new int[maxWidth * maxHeight];
						}
					}
				}

				if (_activeSyncSettings.ThreeDeeRenderer == NDSSyncSettings.ThreeDeeRendererType.Software)
				{
					if (!_openGLProvider.SupportsGLVersion(3, 1))
					{
						lp.Comm.Notify("OpenGL 3.1 is not supported on this machine, screen control options will not work.");
					}
					else
					{
						_glContext = _openGLProvider.RequestGLContext(3, 1, true);
					}
				}

				// start off with baseline 128MiBs
				uint mmapMiBSize = 128;
				if (!IsDSiWare)
				{
					uint romSize = 1;
					while (romSize < roms[0].Length)
					{
						romSize <<= 1;
					}

					mmapMiBSize += romSize / (1024 * 1024);
					if (romSize != roms[0].Length)
					{
						mmapMiBSize += ((uint)roms[0].Length + 1024 * 1024 - 1) / (1024 * 1024);
					}

					if (_activeSyncSettings.EnableDLDI)
					{
						mmapMiBSize += 256;
					}
				}

				if (IsDSi)
				{
					// NAND is 240MiB, round off to 256MiBs here (extra 16MiB for other misc allocations)
					mmapMiBSize += 256;

					// clear NAND code allocates quite a bit of memory (enough for another copy of NAND itself)
					if (_activeSyncSettings.ClearNAND || lp.DeterministicEmulationRequested)
					{
						mmapMiBSize += 256;
					}

					if (_activeSyncSettings.EnableDSiSDCard)
					{
						mmapMiBSize += 256;
					}
				}

				if (roms.Count > 1)
				{
					uint romSize = 1;
					while (romSize < roms[1].Length)
					{
						romSize <<= 1;
					}

					mmapMiBSize += romSize / (1024 * 1024);
					if (romSize != roms[1].Length)
					{
						mmapMiBSize += ((uint)roms[1].Length + 1024 * 1024 - 1) / (1024 * 1024);
					}
				}

				_core = PreInit<LibMelonDS>(new()
				{
					Filename = "melonDS.wbx",
					SbrkHeapSizeKB = 32 * 1024,
					SealedHeapSizeKB = 4,
					InvisibleHeapSizeKB = 176 * 1024,
					PlainHeapSizeKB = 4,
					MmapHeapSizeKB = 1024 * mmapMiBSize,
					SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
					SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
				},
				[
					_readCallback, _writeCallback, _execCallback, _traceCallback,
					_threadStartCallback, _logCallback, _getGLProcAddressCallback
				]);

				_core.SetLogCallback(_logCallback);

				if (_glContext != null)
				{
					var error = _core.InitGL(_getGLProcAddressCallback, _activeSyncSettings.ThreeDeeRenderer, _activeSyncSettings.GLScaleFactor, !OSTailoredCode.IsUnixHost);
					if (error != IntPtr.Zero)
					{
						using (_exe.EnterExit())
						{
							throw new InvalidOperationException(Marshal.PtrToStringAnsi(error));
						}
					}
				}

				_activeSyncSettings.UseRealBIOS |= IsDSi;

				if (!_activeSyncSettings.UseRealBIOS)
				{
					var arm9RomOffset = BinaryPrimitives.ReadInt32LittleEndian(romHeader.Slice(0x20, 4));
					if (arm9RomOffset is >= 0x4000 and < 0x8000)
					{
						// check if the user is using an encrypted rom
						// if they are, they need to be using real bios files
						var secureAreaId = BinaryPrimitives.ReadUInt64LittleEndian(roms[0].AsSpan(arm9RomOffset, 8));
						_activeSyncSettings.UseRealBIOS = secureAreaId != 0xE7FFDEFF_E7FFDEFF;
					}
				}

				ReadOnlySpan<byte> bios9 = [ ], bios7 = [ ], firmware = [ ];
				if (_activeSyncSettings.UseRealBIOS)
				{
					bios9 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9"));
					bios7 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7"));
					firmware = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", IsDSi ? "firmwarei" : "firmware"));

					if (firmware.Length is not (0x20000 or 0x40000 or 0x80000))
					{
						throw new InvalidOperationException("Invalid firmware length");
					}

					NDSFirmware.MaybeWarnIfBadFw(firmware, CoreComm.ShowMessage);
				}

				ReadOnlySpan<byte> bios9i = [ ], nand = [ ];
				if (IsDSi)
				{
					bios9i = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9i"));
					bios7i = bios7i.IsEmpty ? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7i")) : bios7i;
					nand = DecideNAND(CoreComm.CoreFileProvider, (DSiTitleId.Upper & ~0xFF) == 0x00030000, romHeader[0x1B0]);
				}

				ReadOnlySpan<byte> ndsRom = [ ], gbaRom = [ ], tmd = [ ];
				if (IsDSiWare)
				{
					tmd = GetTMDData(DSiTitleId.Full);
					dsiWare = dsiWare.IsEmpty ? roms[0] : dsiWare;
				}
				else
				{
					ndsRom = roms[0];
					if (roms.Count == 2)
					{
						gbaRom = roms[1];
					}
				}

				_activeSyncSettings.FirmwareOverride |= !_activeSyncSettings.UseRealBIOS || lp.DeterministicEmulationRequested;

				// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
				if (!IsDSi && _activeSyncSettings.FirmwareStartUp == NDSSyncSettings.StartUp.AutoBoot)
				{
					_activeSyncSettings.FirmwareLanguage |= (NDSSyncSettings.Language)0x40;
				}

				_activeSyncSettings.UseRealTime &= !lp.DeterministicEmulationRequested;
				var startTime = _activeSyncSettings.UseRealTime ? DateTime.Now : _activeSyncSettings.InitialTime;

				LibMelonDS.ConsoleCreationArgs consoleCreationArgs;

				consoleCreationArgs.NdsRomLength = ndsRom.Length;
				consoleCreationArgs.GbaRomLength = gbaRom.Length;
				consoleCreationArgs.Arm9BiosLength = bios9.Length;
				consoleCreationArgs.Arm7BiosLength = bios7.Length;
				consoleCreationArgs.FirmwareLength = firmware.Length;
				consoleCreationArgs.Arm9iBiosLength = bios9i.Length;
				consoleCreationArgs.Arm7iBiosLength = bios7i.Length;
				consoleCreationArgs.NandLength = nand.Length;
				consoleCreationArgs.DsiWareLength = dsiWare.Length;
				consoleCreationArgs.TmdLength = tmd.Length;

				consoleCreationArgs.DSi = IsDSi;
				consoleCreationArgs.ClearNAND = _activeSyncSettings.ClearNAND || lp.DeterministicEmulationRequested;
				consoleCreationArgs.SkipFW = _activeSyncSettings.SkipFirmware;
				consoleCreationArgs.FullDSiBIOSBoot = _activeSyncSettings.FullDSiBIOSBoot;
				consoleCreationArgs.EnableDLDI = _activeSyncSettings.EnableDLDI;
				consoleCreationArgs.EnableDSiSDCard = _activeSyncSettings.EnableDSiSDCard;
				consoleCreationArgs.DSiDSPHLE = _activeSyncSettings.DSiDSPHLE;

				consoleCreationArgs.EnableJIT = _activeSyncSettings.EnableJIT;
				consoleCreationArgs.MaxBranchSize = _activeSyncSettings.JITMaxBranchSize;
				consoleCreationArgs.LiteralOptimizations = _activeSyncSettings.JITLiteralOptimizations;
				consoleCreationArgs.BranchOptimizations = _activeSyncSettings.JITBranchOptimizations;

				consoleCreationArgs.BitDepth = _settings.AudioBitDepth;
				consoleCreationArgs.Interpolation = _settings.AudioInterpolation;

				consoleCreationArgs.ThreeDeeRenderer = _activeSyncSettings.ThreeDeeRenderer;
				consoleCreationArgs.Threaded3D = _activeSyncSettings.ThreadedRendering;
				consoleCreationArgs.ScaleFactor = _activeSyncSettings.GLScaleFactor;
				consoleCreationArgs.BetterPolygons = _activeSyncSettings.GLBetterPolygons;
				consoleCreationArgs.HiResCoordinates = _activeSyncSettings.GLHiResCoordinates;

				consoleCreationArgs.StartYear = startTime.Year % 100;
				consoleCreationArgs.StartMonth = startTime.Month;
				consoleCreationArgs.StartDay = startTime.Day;
				consoleCreationArgs.StartHour = startTime.Hour;
				consoleCreationArgs.StartMinute = startTime.Minute;
				consoleCreationArgs.StartSecond = startTime.Second;

				_activeSyncSettings.GetFirmwareSettings(out consoleCreationArgs.FwSettings);

				var errorBuffer = new byte[1024];
				unsafe
				{
					fixed (byte*
						ndsRomPtr = ndsRom,
						gbaRomPtr = gbaRom,
						bios9Ptr = bios9,
						bios7Ptr = bios7,
						firmwarePtr = firmware,
						bios9iPtr = bios9i,
						bios7iPtr = bios7i,
						nandPtr = nand,
						dsiWarePtr = dsiWare,
						tmdPtr = tmd)
					{
						consoleCreationArgs.NdsRomData = (IntPtr)ndsRomPtr;
						consoleCreationArgs.GbaRomData = (IntPtr)gbaRomPtr;
						consoleCreationArgs.Arm9BiosData = (IntPtr)bios9Ptr;
						consoleCreationArgs.Arm7BiosData = (IntPtr)bios7Ptr;
						consoleCreationArgs.FirmwareData = (IntPtr)firmwarePtr;
						consoleCreationArgs.Arm9iBiosData = (IntPtr)bios9iPtr;
						consoleCreationArgs.Arm7iBiosData = (IntPtr)bios7iPtr;
						consoleCreationArgs.NandData = (IntPtr)nandPtr;
						consoleCreationArgs.DsiWareData = (IntPtr)dsiWarePtr;
						consoleCreationArgs.TmdData = (IntPtr)tmdPtr;
						_console = _core.CreateConsole(ref consoleCreationArgs, errorBuffer);
					}
				}

				if (_console == IntPtr.Zero)
				{
					var errorStr = Encoding.ASCII.GetString(errorBuffer).TrimEnd('\0');
					throw new InvalidOperationException(errorStr);
				}

				if (IsDSiWare)
				{
					_core.DSiWareSavsLength(_console, DSiTitleId.Full, out PublicSavSize, out PrivateSavSize, out BannerSavSize);
					DSiWareSaveLength = PublicSavSize + PrivateSavSize + BannerSavSize;
				}

				PostInit();

				((MemoryDomainList)this.AsMemoryDomains()).SystemBus = new NDSSystemBus(this.AsMemoryDomains()["ARM9 System Bus"], this.AsMemoryDomains()["ARM7 System Bus"]);

				DeterministicEmulation = lp.DeterministicEmulationRequested || !_activeSyncSettings.UseRealTime;

				_frameThreadPtr = _core.GetFrameThreadProc();
				if (_frameThreadPtr != IntPtr.Zero)
				{
					Console.WriteLine($"Setting up waterbox thread for 0x{(ulong)_frameThreadPtr:X16}");
					_frameThread = new(FrameThreadProc) { IsBackground = true };
					_frameThread.Start();
					_frameThreadAction = CallingConventionAdapters
						.GetWaterboxUnsafeUnwrapped()
						.GetDelegateForFunctionPointer<Action>(_frameThreadPtr);
					_core.SetThreadStartCallback(_threadStartCallback);
				}

				_disassembler = new(_core);
				_serviceProvider.Register<IDisassemblable>(_disassembler);

				// tracelogger cannot be used with the JIT
				if (!_activeSyncSettings.EnableJIT)
				{
					const string TRACE_HEADER = "ARM9+ARM7: Opcode address, opcode, registers (r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, SP, LR, PC, Cy, CpuMode)";
					Tracer = new TraceBuffer(TRACE_HEADER);
					_serviceProvider.Register(Tracer);
				}

				if (_glContext != null)
				{
					_glTextureProvider = new(this, _core, () => _openGLProvider.ActivateGLContext(_glContext));
					_serviceProvider.Register<IVideoProvider>(_glTextureProvider);
					RefreshScreenSettings(_settings);
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private static bool IsRomValid(ReadOnlySpan<byte> rom)
		{
			// check ARM7/ARM9 (and maybe ARM7i/ARM9i) binary offsets/sizes to see if they're sane
			// if these are wildly wrong, the ROM may crash the core
			var unitCode = rom[0x12];
			var isDsiExclusive = (unitCode & 0x03) == 3;
			var arm9RomOffset = BinaryPrimitives.ReadUInt32LittleEndian(rom.Slice(0x20, 4));
			var arm9Size = BinaryPrimitives.ReadUInt32LittleEndian(rom.Slice(0x2C, 4));
			var arm7RomOffset = BinaryPrimitives.ReadUInt32LittleEndian(rom.Slice(0x30, 4));
			var arm7Size = BinaryPrimitives.ReadUInt32LittleEndian(rom.Slice(0x3C, 4));
			if (arm9RomOffset > rom.Length
				|| (arm9Size > 0x3BFE00 && !isDsiExclusive)
				|| arm9RomOffset + arm9Size > rom.Length
				|| arm7RomOffset > rom.Length
				|| (arm7Size > 0x3BFE00 && !isDsiExclusive)
				|| arm7RomOffset + arm7Size > rom.Length)
			{
				return false;
			}

			if ((unitCode & 0x02) != 0)
			{
				var arm9iRomOffset = BinaryPrimitives.ReadUInt32LittleEndian(rom.Slice(0x1C0, 4));
				var arm9iSize = BinaryPrimitives.ReadUInt32LittleEndian(rom.Slice(0x1CC, 4));
				var arm7iRomOffset = BinaryPrimitives.ReadUInt32LittleEndian(rom.Slice(0x1D0, 4));
				var arm7iSize = BinaryPrimitives.ReadUInt32LittleEndian(rom.Slice(0x1DC, 4));
				if (arm9iRomOffset > rom.Length
					|| arm9iRomOffset + arm9iSize > rom.Length
					|| arm7iRomOffset > rom.Length
					|| arm7iRomOffset + arm7iSize > rom.Length)
				{
					return false;
				}
			}

			return true;
		}

		private static (ulong Full, uint Upper, uint Lower) GetDSiTitleId(ReadOnlySpan<byte> romHeader)
		{
			var titleId = BinaryPrimitives.ReadUInt64LittleEndian(romHeader.Slice(0x230, 8));
			return (titleId, (uint)(titleId >> 32), (uint)(titleId & 0xFFFFFFFFU));
		}

		private static byte[] DecideNAND(ICoreFileProvider cfp, bool isDSiEnhanced, byte regionFlags)
		{
			// TODO: priority settings?
			var nandOptions = new List<string> { "JPN", "USA", "EUR", "AUS", "CHN", "KOR" };
			if (isDSiEnhanced) // NB: Core makes cartridges region free regardless, DSiWare must follow DSi region locking however (we'll enforce it regardless)
			{
				nandOptions.Clear();
				if (regionFlags.Bit(0)) nandOptions.Add("JPN");
				if (regionFlags.Bit(1)) nandOptions.Add("USA");
				if (regionFlags.Bit(2)) nandOptions.Add("EUR");
				if (regionFlags.Bit(3)) nandOptions.Add("AUS");
				if (regionFlags.Bit(4)) nandOptions.Add("CHN");
				if (regionFlags.Bit(5)) nandOptions.Add("KOR");
			}

			foreach (var option in nandOptions)
			{
				var ret = cfp.GetFirmware(new("NDS", $"NAND_{option}"));
				if (ret is not null) return ret;
			}

			throw new MissingFirmwareException("Suitable NAND file not found!");
		}

		private static byte[] GetTMDData(ulong titleId)
		{
			using var zip = new ZipArchive(Zstd.DecompressZstdStream(new MemoryStream(Resources.TMDS.Value)), ZipArchiveMode.Read, false);
			using var tmd = zip.GetEntry($"{titleId:x16}.tmd")?.Open() ?? throw new Exception($"Cannot find TMD for title ID {titleId:x16}, please report");
			return tmd.ReadAllBytes();
		}

		// todo: wire this up w/ frontend
		public byte[] GetNAND()
		{
			var length = _core.GetNANDSize(_console);

			if (length > 0)
			{
				var ret = new byte[length];
				_core.GetNANDData(_console, ret);
				return ret;
			}

			return null;
		}

		public bool IsDSi { get; }

		// This check is a bit inaccurate, "true" DSiWare have an upper title ID of 0x00030004
		// However, non-DSiWare NAND apps will have a different upper title ID (usually 0x00030005)
		// Cartridge titles always have 0x00030000 as their upper title ID
		public bool IsDSiWare => (DSiTitleId.Upper & ~0xFF) == 0x00030000 && DSiTitleId.Upper != 0x00030000;

		private (ulong Full, uint Upper, uint Lower) DSiTitleId { get; }

		public override ControllerDefinition ControllerDefinition => NDSController;

		public static readonly ControllerDefinition NDSController = new ControllerDefinition("NDS Controller")
		{
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Y", "X", "L", "R", "LidOpen", "LidClose", "Touch", "Microphone", "Power"
			}
		}.AddXYPair("Touch {0}", AxisPairOrientation.RightAndDown, 0.RangeTo(255), 128, 0.RangeTo(191), 96)
			.AddAxis("Mic Volume", 0.RangeTo(100), 100)
			.AddAxis("GBA Light Sensor", 0.RangeTo(10), 0)
			.MakeImmutable();

		private static LibMelonDS.Buttons GetButtons(IController c)
		{
			LibMelonDS.Buttons b = 0;
			if (c.IsPressed("Up"))
				b |= LibMelonDS.Buttons.UP;
			if (c.IsPressed("Down"))
				b |= LibMelonDS.Buttons.DOWN;
			if (c.IsPressed("Left"))
				b |= LibMelonDS.Buttons.LEFT;
			if (c.IsPressed("Right"))
				b |= LibMelonDS.Buttons.RIGHT;
			if (c.IsPressed("Start"))
				b |= LibMelonDS.Buttons.START;
			if (c.IsPressed("Select"))
				b |= LibMelonDS.Buttons.SELECT;
			if (c.IsPressed("B"))
				b |= LibMelonDS.Buttons.B;
			if (c.IsPressed("A"))
				b |= LibMelonDS.Buttons.A;
			if (c.IsPressed("Y"))
				b |= LibMelonDS.Buttons.Y;
			if (c.IsPressed("X"))
				b |= LibMelonDS.Buttons.X;
			if (c.IsPressed("L"))
				b |= LibMelonDS.Buttons.L;
			if (c.IsPressed("R"))
				b |= LibMelonDS.Buttons.R;
			if (c.IsPressed("LidOpen"))
				b |= LibMelonDS.Buttons.LIDOPEN;
			if (c.IsPressed("LidClose"))
				b |= LibMelonDS.Buttons.LIDCLOSE;
			if (c.IsPressed("Touch"))
				b |= LibMelonDS.Buttons.TOUCH;

			return b;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			if (_glContext != null)
			{
				_openGLProvider.ActivateGLContext(_glContext);
			}

			var isTracing = Tracer?.IsEnabled() ?? false;
			_core.SetTraceCallback(isTracing ? _traceCallback : null, _settings.GetTraceMask());

			if (controller.IsPressed("Power"))
			{
				_core.ResetConsole(_console, _activeSyncSettings.SkipFirmware, DSiTitleId.Full);
			}

			return new LibMelonDS.FrameInfo
			{
				Console = _console,
				Keys = GetButtons(controller),
				TouchX = (byte)controller.AxisValue("Touch X"),
				TouchY = (byte)controller.AxisValue("Touch Y"),
				MicVolume = (byte)(controller.IsPressed("Microphone") ? controller.AxisValue("Mic Volume") : 0),
				GBALightSensor = (byte)controller.AxisValue("GBA Light Sensor"),
				ConsiderAltLag = (byte)(_settings.ConsiderAltLag ? 1 : 0),
				UseTouchInterpolation = (byte)(_activeSyncSettings.UseTouchInterpolation ? 1 : 0),
			};
		}

		private readonly IntPtr _frameThreadPtr;
		private readonly Action _frameThreadAction;
		private readonly LibMelonDS.ThreadStartCallback _threadStartCallback;

		private readonly Thread _frameThread;
		private readonly SemaphoreSlim _frameThreadStartEvent = new(0, 1);
		private readonly SemaphoreSlim _frameThreadEndEvent = new(0, 1);
		private bool _isDisposing;
		private bool _renderThreadRanThisFrame;

		public override void Dispose()
		{
			_isDisposing = true;
			_frameThreadStartEvent.Release();

			if (_frameThread != null)
			{
				while (_frameThread.IsAlive)
				{
					Thread.Sleep(1);
				}
			}

			_frameThreadStartEvent.Dispose();
			_frameThreadEndEvent.Dispose();

			if (_glContext != null)
			{
				_openGLProvider.ReleaseContext(_glContext);
				_glContext = null;
			}

			_memoryCallbacks.ActiveChanged -= SetMemoryCallbacks;

			base.Dispose();
		}

		private void FrameThreadProc()
		{
			while (true)
			{
				_frameThreadStartEvent.Wait();
				if (_isDisposing) break;
				_frameThreadAction();
				_frameThreadEndEvent.Release();
			}
		}

		private void ThreadStartCallback()
		{
			if (_renderThreadRanThisFrame)
			{
				// This is technically possible due to the game able to force another frame to be rendered by touching vcount
				// (ALSO MEANS VSYNC NUMBERS ARE KIND OF A LIE)
				_frameThreadEndEvent.Wait();
			}

			_renderThreadRanThisFrame = true;
			_frameThreadStartEvent.Release();
		}

		protected override void FrameAdvancePost()
		{
			if (_renderThreadRanThisFrame)
			{
				_frameThreadEndEvent.Wait();
				_renderThreadRanThisFrame = false;
			}

			if (_glTextureProvider != null)
			{
				_glTextureProvider.VideoDirty = true;
			}
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			SetMemoryCallbacks();
			_core.SetThreadStartCallback(_threadStartCallback);
			if (_frameThreadPtr != _core.GetFrameThreadProc())
			{
				throw new InvalidOperationException("_frameThreadPtr mismatch");
			}

			_core.SetSoundConfig(_console, _settings.AudioBitDepth, _settings.AudioInterpolation);

			// Present GL again post load state
			// This is mainly important to mimic PopulateFromBuffer's functionality
			// Note this doesn't actually do anything for OpenGL Classic and Compute (yet)
			// But at least width and height will be corrected (since base LoadStateBinary overrides them)
			if (_glTextureProvider != null)
			{
				_openGLProvider.ActivateGLContext(_glContext);
				_core.PresentGL(_console, out var w , out var h);
				BufferWidth = w;
				BufferHeight = h;
				_glTextureProvider.VideoDirty = true;
			}
		}

		// omega hack
		public class NDSSystemBus : MemoryDomain
		{
			private readonly MemoryDomain Arm9Bus;
			private readonly MemoryDomain Arm7Bus;
			private bool _useArm9;

			public NDSSystemBus(MemoryDomain arm9, MemoryDomain arm7)
			{
				Size = 1L << 32;
				WordSize = 4;
				EndianType = Endian.Little;
				Writable = true;

				Arm9Bus = arm9;
				Arm7Bus = arm7;

				UseArm9 = true; // important to set the initial name correctly
			}

			public bool UseArm9
			{
				get => _useArm9;
				set
				{
					_useArm9 = value;
					Name = _useArm9 ? "ARM9 System Bus" : "ARM7 System Bus";
				}
			}

			public override byte PeekByte(long addr) => UseArm9 ? Arm9Bus.PeekByte(addr) : Arm7Bus.PeekByte(addr);

			public override void PokeByte(long addr, byte val)
			{
				if (UseArm9)
				{
					Arm9Bus.PokeByte(addr, val);
				}
				else
				{
					Arm7Bus.PokeByte(addr, val);
				}
			}
		}
	}
}
