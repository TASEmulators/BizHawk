using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	[PortedCore(CoreNames.VirtualJaguar, "Niels Wagenaar, Carwin Jones, Adam Green, James L. Hammons", "2.1.3", "https://icculus.org/virtualjaguar/", isReleased: false)]
	public partial class VirtualJaguar : WaterboxCore, IRegionable, IDriveLight
	{
		private readonly LibVirtualJaguar _core;
		private readonly JaguarDisassembler _disassembler;

		[CoreConstructor(VSystemID.Raw.JAG)]
		public VirtualJaguar(CoreLoadParameters<VirtualJaguarSettings, VirtualJaguarSyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth = 326,
				DefaultHeight = 240,
				MaxWidth = 1304,
				MaxHeight = 256,
				MaxSamples = 1024,
				DefaultFpsNumerator = 60,
				DefaultFpsDenominator = 1,
				SystemId = VSystemID.Raw.JAG,
			})
		{
			_settings = lp.Settings ?? new();
			_syncSettings = lp.SyncSettings ?? new();

			ControllerDefinition = CreateControllerDefinition(_syncSettings.P1Active, _syncSettings.P2Active);
			Region = _syncSettings.NTSC ? DisplayType.NTSC : DisplayType.PAL;
			VsyncNumerator = _syncSettings.NTSC ? 60 : 50;

			InitMemoryCallbacks();
			_cpuTraceCallback = MakeCPUTrace;
			_gpuTraceCallback = MakeGPUTrace;
			_dspTraceCallback = MakeDSPTrace;
			_cdTocCallback = CDTOCCallback;
			_cdReadCallback = CDReadCallback;

			_core = PreInit<LibVirtualJaguar>(new WaterboxOptions
			{
				Filename = "virtualjaguar.wbx",
				SbrkHeapSizeKB = 64 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 64 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { _readCallback, _writeCallback, _execCallback, _cpuTraceCallback, _gpuTraceCallback, _dspTraceCallback, _cdTocCallback, _cdReadCallback, });

			var bios = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("Jaguar", "Bios"));
			if (bios.Length != 0x20000)
			{
				throw new MissingFirmwareException("Jaguar Bios must be 131072 bytes!");
			}

			var settings = new LibVirtualJaguar.Settings()
			{
				NTSC = _syncSettings.NTSC,
				UseBIOS = !_syncSettings.SkipBIOS,
				UseFastBlitter = _syncSettings.UseFastBlitter,
			};

			if (lp.Discs.Count > 0)
			{
#if false
				_cd = lp.Discs[0].DiscData;
				_cdReader = new(_cd);
#else
				_cd = new Disc[lp.Discs.Count];
				_cdReader = new DiscSectorReader[lp.Discs.Count];
				for (int i = 0; i < lp.Discs.Count; i++)
				{
					_cd[i] = lp.Discs[i].DiscData;
					_cdReader[i] = new(lp.Discs[i].DiscData);
				}
#endif
				_core.SetCdCallbacks(_cdTocCallback, _cdReadCallback);

				unsafe
				{
					fixed (byte* bp = bios)
					{
						_core.InitWithCd(ref settings, (IntPtr)bp);
					}
				}
			}
			else
			{
				_cdTocCallback = null;
				_cdReadCallback = null;
				var rom = lp.Roms[0].FileData;
				unsafe
				{
					fixed (byte* rp = rom, bp = bios)
					{
						if (!_core.Init(ref settings, (IntPtr)bp, (IntPtr)rp, rom.Length))
						{
							throw new Exception("Core rejected the rom!");
						}
					}
				}
			}

			_core.SetCdCallbacks(null, null);

			PostInit();

			_core.SetCdCallbacks(_cdTocCallback, _cdReadCallback);

			_disassembler = new();
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			// bleh
			const string TRACE_HEADER = "M68K: PC, machine code, mnemonic, operands, registers (D0-D7, A0-A7, SR), flags (XNZVC)\r\n"
				+ "GPU/DSP: PC, machine code, mnemonic, operands, registers (r0-r32)";
			Tracer = new TraceBuffer(TRACE_HEADER);
			_serviceProvider.Register(Tracer);
		}

		private static readonly IReadOnlyList<string> JaguarButtonsOrdered = new[]
		{
			"Up", "Down", "Left", "Right", "A", "B", "C", "Option", "Pause",
			"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "Asterisk", "Pound",
		};

		private static ControllerDefinition CreateControllerDefinition(bool p1, bool p2)
		{
			var ret = new ControllerDefinition("Jaguar Controller");

			if (p1)
			{
				ret.BoolButtons.AddRange(JaguarButtonsOrdered.Select(s => $"P1 {s}"));
			}

			if (p2)
			{
				ret.BoolButtons.AddRange(JaguarButtonsOrdered.Select(s => $"P2 {s}"));
			}

			ret.BoolButtons.Add("Power");
			return ret.MakeImmutable();
		}

		private LibVirtualJaguar.Buttons GetButtons(IController controller, int n)
		{
			LibVirtualJaguar.Buttons ret = 0;

			if (controller.IsPressed($"P{n} Up"))
				ret |= LibVirtualJaguar.Buttons.Up;
			if (controller.IsPressed($"P{n} Down"))
				ret |= LibVirtualJaguar.Buttons.Down;
			if (controller.IsPressed($"P{n} Left"))
				ret |= LibVirtualJaguar.Buttons.Left;
			if (controller.IsPressed($"P{n} Right"))
				ret |= LibVirtualJaguar.Buttons.Right;
			if (controller.IsPressed($"P{n} 0"))
				ret |= LibVirtualJaguar.Buttons._0;
			if (controller.IsPressed($"P{n} 1"))
				ret |= LibVirtualJaguar.Buttons._1;
			if (controller.IsPressed($"P{n} 2"))
				ret |= LibVirtualJaguar.Buttons._2;
			if (controller.IsPressed($"P{n} 3"))
				ret |= LibVirtualJaguar.Buttons._3;
			if (controller.IsPressed($"P{n} 4"))
				ret |= LibVirtualJaguar.Buttons._4;
			if (controller.IsPressed($"P{n} 5"))
				ret |= LibVirtualJaguar.Buttons._5;
			if (controller.IsPressed($"P{n} 6"))
				ret |= LibVirtualJaguar.Buttons._6;
			if (controller.IsPressed($"P{n} 7"))
				ret |= LibVirtualJaguar.Buttons._7;
			if (controller.IsPressed($"P{n} 8"))
				ret |= LibVirtualJaguar.Buttons._8;
			if (controller.IsPressed($"P{n} 9"))
				ret |= LibVirtualJaguar.Buttons._9;
			if (controller.IsPressed($"P{n} Asterisk"))
				ret |= LibVirtualJaguar.Buttons.Asterisk;
			if (controller.IsPressed($"P{n} Pound"))
				ret |= LibVirtualJaguar.Buttons.Pound;
			if (controller.IsPressed($"P{n} A"))
				ret |= LibVirtualJaguar.Buttons.A;
			if (controller.IsPressed($"P{n} B"))
				ret |= LibVirtualJaguar.Buttons.B;
			if (controller.IsPressed($"P{n} C"))
				ret |= LibVirtualJaguar.Buttons.C;
			if (controller.IsPressed($"P{n} Option"))
				ret |= LibVirtualJaguar.Buttons.Option;
			if (controller.IsPressed($"P{n} Pause"))
				ret |= LibVirtualJaguar.Buttons.Pause;

			return ret;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			if (Tracer.IsEnabled())
			{
				_core.SetTraceCallbacks(
					_settings.TraceCPU ? _cpuTraceCallback : null,
					_settings.TraceGPU ? _gpuTraceCallback : null,
					_settings.TraceDSP ? _dspTraceCallback : null);
			}
			else
			{
				_core.SetTraceCallbacks(null, null, null);
			}

			DriveLightOn = false;

			return new LibVirtualJaguar.FrameInfo()
			{
				Player1 = GetButtons(controller, 1),
				Player2 = GetButtons(controller, 2),
				Reset = controller.IsPressed("Power"),
			};
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			SetMemoryCallbacks();
			_core.SetCdCallbacks(_cdTocCallback, _cdReadCallback);
		}

		public DisplayType Region { get; }

		public bool IsJaguarCD => _cd != null;
		public bool DriveLightEnabled => IsJaguarCD;
		public bool DriveLightOn { get; private set; }

		private readonly LibVirtualJaguar.CDTOCCallback _cdTocCallback;
		private readonly LibVirtualJaguar.CDReadCallback _cdReadCallback;

#if false // uh oh, we don't actually have multisession disc support, so...
		private readonly Disc _cd;
		private readonly DiscSectorReader _cdReader;

		private void CDTOCCallback(IntPtr dst)
		{
			var lastLeadOutTs = new Timestamp(_cd.TOC.LeadoutLBA + 150);

			var toc = new LibVirtualJaguar.TOC
			{
				Padding0 = 0,
				Padding1 = 0,
				NumSessions = (byte)(_cd.Structure.Sessions.Count - 1),
				MinTrack = (byte)_cd.TOC.FirstRecordedTrackNumber,
				MaxTrack = (byte)_cd.TOC.LastRecordedTrackNumber,
				LastLeadOutMins = lastLeadOutTs.MIN,
				LastLeadOutSecs = lastLeadOutTs.SEC,
				LastLeadOutFrames = lastLeadOutTs.FRAC,
				Tracks = new LibVirtualJaguar.TOC.Track[127],
			};

			var trackNum = 0;
			for (int i = 1; i < _cd.Structure.Sessions.Count; i++)
			{
				var session = _cd.Structure.Sessions[i];
				for (int j = 1; j < session.InformationTrackCount; j++)
				{
					var track = session.Tracks[trackNum];
					toc.Tracks[i].TrackNum = (byte)track.Number;
					var ts = new Timestamp(track.LBA + 150);
					toc.Tracks[i].StartMins = ts.MIN;
					toc.Tracks[i].StartSecs = ts.SEC;
					toc.Tracks[i].StartFrames = ts.FRAC;
					toc.Tracks[i].SessionNum = (byte)(i - 1);
					var durTs = new Timestamp(track.NextTrack.LBA - track.LBA);
					toc.Tracks[i].DurMins = durTs.MIN;
					toc.Tracks[i].DurSecs = durTs.SEC;
					toc.Tracks[i].DurFrames = durTs.FRAC;
					trackNum++;
				}
			}

			Marshal.StructureToPtr(toc, dst, false);
		}

		private void CDReadCallback(int lba, IntPtr dst)
		{
			var buf = new byte[2352];
			_cdReader.ReadLBA_2352(lba, buf, 0);
			Marshal.Copy(buf, 0, dst, 2352);
			DriveLightOn = true;
		}

#else

		private readonly Disc[] _cd;
		private readonly DiscSectorReader[] _cdReader;
		private int[] _cdLbaOffsets;

		private void CDTOCCallback(IntPtr dst)
		{
			var lastLeadOutTs = new Timestamp(_cd.Sum(c => c.TOC.LeadoutLBA) + _cd.Length * 150);

			var toc = new LibVirtualJaguar.TOC
			{
				Padding0 = 0,
				Padding1 = 0,
				NumSessions = (byte)_cd.Length,
				MinTrack = (byte)_cd[0].TOC.FirstRecordedTrackNumber,
				MaxTrack = (byte)(_cd[0].TOC.FirstRecordedTrackNumber + _cd.Sum(c => c.Session1.InformationTrackCount - c.TOC.FirstRecordedTrackNumber)),
				LastLeadOutMins = lastLeadOutTs.MIN,
				LastLeadOutSecs = lastLeadOutTs.SEC,
				LastLeadOutFrames = lastLeadOutTs.FRAC,
				Tracks = new LibVirtualJaguar.TOC.Track[127],
			};

			var trackNum = 0;
			var lbaOffset = 0;
			var trackOffset = 0;
			_cdLbaOffsets = new int[_cd.Length];
			for (int i = 0; i < _cd.Length; i++)
			{
				var session = _cd[i].Session1;
				for (int j = 0; j < session.InformationTrackCount; j++)
				{
					var track = session.Tracks[j + 1];
					toc.Tracks[trackNum].TrackNum = (byte)(trackOffset + track.Number);
					var ts = new Timestamp(lbaOffset + track.LBA + 150);
					toc.Tracks[trackNum].StartMins = ts.MIN;
					toc.Tracks[trackNum].StartSecs = ts.SEC;
					toc.Tracks[trackNum].StartFrames = ts.FRAC;
					toc.Tracks[trackNum].SessionNum = (byte)i;
					var durTs = new Timestamp(track.NextTrack.LBA - track.LBA);
					toc.Tracks[trackNum].DurMins = durTs.MIN;
					toc.Tracks[trackNum].DurSecs = durTs.SEC;
					toc.Tracks[trackNum].DurFrames = durTs.FRAC;
					trackNum++;
				}

				trackOffset += session.InformationTrackCount;
				lbaOffset += session.LeadoutTrack.LBA - session.FirstInformationTrack.LBA + 150;
				_cdLbaOffsets[i] = lbaOffset;
			}

			Marshal.StructureToPtr(toc, dst, false);
		}

		private void CDReadCallback(int lba, IntPtr dst)
		{
			var buf = new byte[2352];
			for (int i = 0; i < _cdReader.Length; i++)
			{
				if (lba < _cdLbaOffsets[i])
				{
					_cdReader[i].ReadLBA_2352(lba - (i == 0 ? 0 : _cdLbaOffsets[i - 1]), buf, 0);
					break;
				}
			}

			Marshal.Copy(buf, 0, dst, 2352);
			DriveLightOn = true;
		}
#endif
	}
}
