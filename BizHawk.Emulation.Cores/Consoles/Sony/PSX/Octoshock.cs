//TODO hook up newer file ID stuff, think about how to combine it with the disc ID
//TODO change display manager to not require 0xFF alpha channel set on videoproviders. check gdi+ and opengl! this will get us a speedup in some places
//TODO Disc.Structure.Sessions[0].length_aba was 0
//TODO mednafen 0.9.37 changed some disc region detection heuristics. analyze and apply in c# side. also the SCEX id handling changed, maybe simplified

//TODO - ok, think about this. we MUST load a state with the CDC completely intact. no quickly changing discs. thats madness.
//well, I could savestate the disc index and validate the disc collection when loading a state.
//the big problem is, it's completely at odds with the slider-based disc changing model. 
//but, maybe it can be reconciled with that model by using the disc ejection to our advantage. 
//perhaps moving the slider is meaningless if the disc is ejected--it only affects what disc is inserted when the disc gets inserted!! yeah! this might could save us!
//not exactly user friendly but maybe we can build it from there with a custom UI.. a disk-changer? dunno if that would help

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;
using BizHawk.Common;

#pragma warning disable 649 //adelikat: Disable dumb warnings until this file is complete

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	[CoreAttributes(
		"Octoshock",
		"Ryphecha",
		isPorted: true,
		isReleased: true
		)]
	public unsafe partial class Octoshock : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IStatable, IDriveLight, ISettable<Octoshock.Settings, Octoshock.SyncSettings>, IRegionable, IInputPollable
	{
		public string SystemId { get { return "PSX"; } }

		public static ControllerDefinition CreateControllerDefinition(SyncSettings syncSettings)
		{
			ControllerDefinition definition = new ControllerDefinition();
			definition.Name = "PSX DualShock Controller"; // <-- for compatibility
														  //ControllerDefinition.Name = "PSX FrontIO"; // TODO - later rename to this, I guess, so it's less misleading. don't want to wreck keybindings yet.

			var cfg = syncSettings.FIOConfig.ToLogical();

			for (int i = 0; i < cfg.NumPlayers; i++)
			{
				int pnum = i + 1;
				definition.BoolButtons.AddRange(new[]
				{
						"P" + pnum + " Up",
						"P" + pnum + " Down",
						"P" + pnum + " Left",
						"P" + pnum + " Right",
						"P" + pnum + " Select",
						"P" + pnum + " Start",
						"P" + pnum + " Square",
						"P" + pnum + " Triangle",
						"P" + pnum + " Circle",
						"P" + pnum + " Cross",
						"P" + pnum + " L1",
						"P" + pnum + " R1",
						"P" + pnum + " L2",
						"P" + pnum + " R2",
					});

				var type = cfg.DevicesPlayer[i];

				if (type == OctoshockDll.ePeripheralType.DualShock || type == OctoshockDll.ePeripheralType.DualAnalog)
				{
					definition.BoolButtons.Add("P" + pnum + " L3");
					definition.BoolButtons.Add("P" + pnum + " R3");
					definition.BoolButtons.Add("P" + pnum + " MODE");

					definition.FloatControls.AddRange(new[]
					{
							"P" + pnum + " LStick X",
							"P" + pnum + " LStick Y",
							"P" + pnum + " RStick X",
							"P" + pnum + " RStick Y"
						});

					definition.FloatRanges.Add(new[] { 0.0f, 128.0f, 255.0f });
					definition.FloatRanges.Add(new[] { 255.0f, 128.0f, 0.0f });
					definition.FloatRanges.Add(new[] { 0.0f, 128.0f, 255.0f });
					definition.FloatRanges.Add(new[] { 255.0f, 128.0f, 0.0f });
				}
			}

			definition.BoolButtons.AddRange(new[]
			{
				"Open",
				"Close",
				"Reset"
			});

			definition.FloatControls.Add("Disc Select");

			definition.FloatRanges.Add(
				//new[] {-1f,-1f,-1f} //this is carefully chosen so that we end up with a -1 disc by default (indicating that it's never been set)
				//hmm.. I don't see why this wouldn't work
				new[] { 0f, 1f, 1f }
			);

			return definition;
		}

		private void SetControllerButtons()
		{
			ControllerDefinition = CreateControllerDefinition(_SyncSettings);
		}

		public string BoardName { get { return null; } }

		private int[] frameBuffer = new int[0];
		private Random rand = new Random();
		public CoreComm CoreComm { get; private set; }

		//we can only have one active core at a time, due to the lib being so static.
		//so we'll track the current one here and detach the previous one whenever a new one is booted up.
		static Octoshock CurrOctoshockCore;

		IntPtr psx;

		bool disposed = false;
		public void Dispose()
		{
			if (disposed) return;

			disposed = true;

			//discs arent bound to shock core instances, but they may be mounted. kill the core instance first to effectively dereference the disc
			OctoshockDll.shock_Destroy(psx);
			psx = IntPtr.Zero;

			//destroy all discs we're managing (and the unmanaged octoshock resources)
			foreach (var di in discInterfaces)
			{
				di.Disc.Dispose();
				di.Dispose();
			}
			discInterfaces.Clear();
		}

		/// <summary>
		/// Wraps the ShockDiscRef returned from the DLL and acts as a bridge between it and a DiscSystem disc
		/// </summary>
		class DiscInterface : IDisposable
		{
			public DiscInterface(DiscSystem.Disc disc, Action<DiscInterface> cbActivity)
			{
				this.Disc = disc;
				cbReadTOC = ShockDisc_ReadTOC;
				cbReadLBA = ShockDisc_ReadLBA2448;
				this.cbActivity = cbActivity;
				OctoshockDll.shock_CreateDisc(out OctoshockHandle, IntPtr.Zero, disc.Session1.LeadoutLBA, cbReadTOC, cbReadLBA, true);
			}

			OctoshockDll.ShockDisc_ReadTOC cbReadTOC;
			OctoshockDll.ShockDisc_ReadLBA cbReadLBA;
			Action<DiscInterface> cbActivity;

			public DiscSystem.Disc Disc;
			public IntPtr OctoshockHandle;

			public void Dispose()
			{
				OctoshockDll.shock_DestroyDisc(OctoshockHandle);
				OctoshockHandle = IntPtr.Zero;
			}

			int ShockDisc_ReadTOC(IntPtr opaque, OctoshockDll.ShockTOC* read_target, OctoshockDll.ShockTOCTrack* tracks101)
			{
				read_target->disc_type = (byte)Disc.TOC.Session1Format;
				read_target->first_track = (byte)Disc.TOC.FirstRecordedTrackNumber; //i _think_ thats what is meant here
				read_target->last_track = (byte)Disc.TOC.LastRecordedTrackNumber; //i _think_ thats what is meant here

				tracks101[0].lba = tracks101[0].adr = tracks101[0].control = 0;

				for (int i = 1; i < 100; i++)
				{
					var item = Disc.TOC.TOCItems[i];
					tracks101[i].adr = (byte)(item.Exists ? 1 : 0);
					tracks101[i].lba = (uint)item.LBA;
					tracks101[i].control = (byte)item.Control;
				}

				////the lead-out track is to be synthesized
				tracks101[read_target->last_track + 1].adr = 1;
				tracks101[read_target->last_track + 1].control = 0;
				tracks101[read_target->last_track + 1].lba = (uint)Disc.TOC.LeadoutLBA;

				//element 100 is to be copied as the lead-out track
				tracks101[100] = tracks101[read_target->last_track + 1];

				return OctoshockDll.SHOCK_OK;
			}

			byte[] SectorBuffer = new byte[2448];

			int ShockDisc_ReadLBA2448(IntPtr opaque, int lba, void* dst)
			{
				cbActivity(this);

				//todo - cache reader
				DiscSystem.DiscSectorReader dsr = new DiscSystem.DiscSectorReader(Disc);
				int readed = dsr.ReadLBA_2448(lba, SectorBuffer, 0);
				if (readed == 2448)
				{
					Marshal.Copy(SectorBuffer, 0, new IntPtr(dst), 2448);
					return OctoshockDll.SHOCK_OK;
				}
				else
					return OctoshockDll.SHOCK_ERROR;
			}
		}

		public List<DiscSystem.Disc> Discs;
		List<DiscInterface> discInterfaces = new List<DiscInterface>();
		DiscInterface currentDiscInterface;

		public DisplayType Region { get { return SystemVidStandard == OctoshockDll.eVidStandard.PAL ? DisplayType.PAL : DisplayType.NTSC; } }

		public OctoshockDll.eRegion SystemRegion { get; private set; }
		public OctoshockDll.eVidStandard SystemVidStandard { get; private set; }
		public System.Drawing.Size CurrentVideoSize { get; private set; }

		public bool CurrentTrayOpen { get; private set; }
		public int CurrentDiscIndexMounted { get; private set; }

		public List<string> HackyDiscButtons = new List<string>();

		public Octoshock(CoreComm comm, PSF psf, object settings, object syncSettings)
		{
			Load(comm, null, null, null, settings, syncSettings, psf);
			OctoshockDll.shock_PowerOn(psx);
		}

		//note: its annoying that we have to have a disc before constructing this.
		//might want to change that later. HOWEVER - we need to definitely have a region, at least
		public Octoshock(CoreComm comm, List<DiscSystem.Disc> discs, List<string> discNames, byte[] exe, object settings, object syncSettings)
		{
			Load(comm, discs, discNames, exe, settings, syncSettings, null);
			OctoshockDll.shock_PowerOn(psx);
		}

		void Load(CoreComm comm, List<DiscSystem.Disc> discs, List<string> discNames, byte[] exe, object settings, object syncSettings, PSF psf)
		{
			ConnectTracer();
			CoreComm = comm;
			DriveLightEnabled = true;

			_Settings = (Settings)settings ?? new Settings();
			_SyncSettings = (SyncSettings)syncSettings ?? new SyncSettings();

			Discs = discs;

			Attach();

			//assume this region for EXE and PSF, maybe not correct though
			string firmwareRegion = "U";
			SystemRegion = OctoshockDll.eRegion.NA;

			if (discs != null)
			{
				HackyDiscButtons.AddRange(discNames);

				foreach (var disc in discs)
				{
					var discInterface = new DiscInterface(disc,
						(di) =>
						{
							//if current disc this delegate disc, activity is happening
							if (di == currentDiscInterface)
								DriveLightOn = true;
						});

					discInterfaces.Add(discInterface);
				}
			}
			else
			{
				//assume its NA region for test programs, for now. could it be read out of the ps-exe header?
			}

			if (discInterfaces.Count != 0)
			{
				//determine region of one of the discs
				OctoshockDll.ShockDiscInfo discInfo;
				OctoshockDll.shock_AnalyzeDisc(discInterfaces[0].OctoshockHandle, out discInfo);

				//try to acquire the appropriate firmware
				if (discInfo.region == OctoshockDll.eRegion.EU) firmwareRegion = "E";
				if (discInfo.region == OctoshockDll.eRegion.JP) firmwareRegion = "J";
				SystemRegion = discInfo.region;
			}

			//see http://problemkaputt.de/psx-spx.htm
			int CpuClock_n = 44100 * 768;
			int CpuClock_d = 1;
			int VidClock_n = CpuClock_n * 11;
			int VidClock_d = CpuClock_d * 7;
			if (SystemRegion == OctoshockDll.eRegion.EU)
			{
				CoreComm.VsyncNum = VidClock_n;
				CoreComm.VsyncDen = VidClock_d * 314 * 3406;
				SystemVidStandard = OctoshockDll.eVidStandard.PAL;
			}
			else
			{
				CoreComm.VsyncNum = VidClock_n;
				CoreComm.VsyncDen = VidClock_d * 263 * 3413;
				SystemVidStandard = OctoshockDll.eVidStandard.NTSC;
			}

			//TODO - known bad firmwares are a no-go. we should refuse to boot them. (thats the mednafen policy)
			byte[] firmware = comm.CoreFileProvider.GetFirmware("PSX", firmwareRegion, true, "A PSX `" + firmwareRegion + "` region bios file is required");

			//create the instance
			fixed (byte* pFirmware = firmware)
				OctoshockDll.shock_Create(out psx, SystemRegion, pFirmware);

			SetMemoryDomains();
			InitMemCallbacks();

			//set a default framebuffer based on the first frame of emulation, to cut down on flickering or whatever
			//this is probably quixotic, but we have to pick something
			{
				BufferWidth = 280;
				BufferHeight = 240;
				if (SystemVidStandard == OctoshockDll.eVidStandard.PAL)
				{
					BufferWidth = 280;
					BufferHeight = 288;
				}
				CurrentVideoSize = new System.Drawing.Size(BufferWidth, BufferHeight);
				var ri = Octoshock.CalculateResolution(SystemVidStandard, _Settings, BufferWidth, BufferHeight);
				BufferWidth = VirtualWidth = ri.Resolution.Width;
				BufferHeight = VirtualHeight = ri.Resolution.Height;
				//VideoProvider_Padding = new System.Drawing.Size(50,50);
				frameBuffer = new int[BufferWidth * BufferHeight];
			}

			if (discInterfaces.Count != 0)
			{
				//start with first disc inserted and tray closed. it's a sensible default.
				//it will be possible for the user to specify a different initial configuration, but this will inform the UI
				CurrentTrayOpen = false;
				CurrentDiscIndexMounted = 1;
			}
			else if (psf == null)
			{
				//must be an exe
				fixed (byte* pExeBuffer = exe)
					OctoshockDll.shock_MountEXE(psx, pExeBuffer, exe.Length, false);

				//start with no disc inserted and tray closed
				CurrentTrayOpen = false;
				CurrentDiscIndexMounted = 0;
				OctoshockDll.shock_CloseTray(psx);
			}
			else
			{
				//must be a psf
				if (psf.LibData != null)
					fixed (byte* pBuf = psf.LibData)
						OctoshockDll.shock_MountEXE(psx, pBuf, psf.LibData.Length, true);
				fixed (byte* pBuf = psf.Data)
					OctoshockDll.shock_MountEXE(psx, pBuf, psf.Data.Length, false);

				//start with no disc inserted and tray closed
				CurrentTrayOpen = false;
				CurrentDiscIndexMounted = 0;
				OctoshockDll.shock_CloseTray(psx);
			}

			//setup the controller based on sync settings
			SetControllerButtons();

			var fioCfg = _SyncSettings.FIOConfig;
			if(fioCfg.Devices8[0] != OctoshockDll.ePeripheralType.None)
				OctoshockDll.shock_Peripheral_Connect(psx, 0x01, fioCfg.Devices8[0]);
			if (fioCfg.Devices8[4] != OctoshockDll.ePeripheralType.None)
				OctoshockDll.shock_Peripheral_Connect(psx, 0x02, fioCfg.Devices8[4]);

			var memcardTransaction = new OctoshockDll.ShockMemcardTransaction()
			{
				transaction = OctoshockDll.eShockMemcardTransaction.Connect
			};
			if (fioCfg.Memcards[0]) OctoshockDll.shock_Peripheral_MemcardTransact(psx, 0x01, ref memcardTransaction);
			if (fioCfg.Memcards[1]) OctoshockDll.shock_Peripheral_MemcardTransact(psx, 0x02, ref memcardTransaction);

			//do this after framebuffers and peripherals and whatever crap are setup. kind of lame, but thats how it is for now
			StudySaveBufferSize();
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public bool DriveLightEnabled { get; private set; }
		public bool DriveLightOn { get; private set; }

		void Attach()
		{
			//attach this core as the current
			if (CurrOctoshockCore != null)
				CurrOctoshockCore.Dispose();
			CurrOctoshockCore = this;

			//the psx instance cant be created until the desired region is known, which needs a disc, so we need the dll static attached first
		}

		static Octoshock()
		{
		}


		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		void SetInput()
		{
			var fioCfg = _SyncSettings.FIOConfig.ToLogical();

			int portNum = 0x01;
			foreach (int slot in new[] { 0, 4 })
			{
				//no input to set
				if (fioCfg.Devices8[slot] == OctoshockDll.ePeripheralType.None)
					continue;

				uint buttons = 0;
				string pstring = "P" + fioCfg.PlayerAssignments[slot] + " ";

				if (Controller.IsPressed(pstring + "Select")) buttons |= 1;
				if (Controller.IsPressed(pstring + "Start")) buttons |= 8;
				if (Controller.IsPressed(pstring + "Up")) buttons |= 16;
				if (Controller.IsPressed(pstring + "Right")) buttons |= 32;
				if (Controller.IsPressed(pstring + "Down")) buttons |= 64;
				if (Controller.IsPressed(pstring + "Left")) buttons |= 128;
				if (Controller.IsPressed(pstring + "L2")) buttons |= 256;
				if (Controller.IsPressed(pstring + "R2")) buttons |= 512;
				if (Controller.IsPressed(pstring + "L1")) buttons |= 1024;
				if (Controller.IsPressed(pstring + "R1")) buttons |= 2048;
				if (Controller.IsPressed(pstring + "Triangle")) buttons |= 4096;
				if (Controller.IsPressed(pstring + "Circle")) buttons |= 8192;
				if (Controller.IsPressed(pstring + "Cross")) buttons |= 16384;
				if (Controller.IsPressed(pstring + "Square")) buttons |= 32768;

				byte left_x = 0, left_y = 0, right_x = 0, right_y = 0;
				if (fioCfg.Devices8[slot] == OctoshockDll.ePeripheralType.DualShock || fioCfg.Devices8[slot] == OctoshockDll.ePeripheralType.DualAnalog)
				{
					if (Controller.IsPressed(pstring + "L3")) buttons |= 2;
					if (Controller.IsPressed(pstring + "R3")) buttons |= 4;
					if (Controller.IsPressed(pstring + "MODE")) buttons |= 65536;

					left_x = (byte)Controller.GetFloat(pstring + "LStick X");
					left_y = (byte)Controller.GetFloat(pstring + "LStick Y");
					right_x = (byte)Controller.GetFloat(pstring + "RStick X");
					right_y = (byte)Controller.GetFloat(pstring + "RStick Y");
				}

				OctoshockDll.shock_Peripheral_SetPadInput(psx, portNum, buttons, left_x, left_y, right_x, right_y);
				portNum <<= 1;
			}
		}

		public class ResolutionInfo
		{
			public System.Drawing.Size Resolution, Padding;
			public System.Drawing.Size Total { get { return System.Drawing.Size.Add(Resolution, Padding); } }
		}

		/// <summary>
		/// Calculates what the output resolution would be for the given input resolution and settings
		/// </summary>
		public static ResolutionInfo CalculateResolution(OctoshockDll.eVidStandard standard, Settings settings, int w, int h)
		{
			ResolutionInfo ret = new ResolutionInfo();

			//some of this logic is duplicated in the c++ side, be sure to check there
			//TODO - scanline control + framebuffer mode is majorly broken

			int virtual_width = 800;
			if (settings.HorizontalClipping == eHorizontalClipping.Basic) virtual_width = 768;
			if (settings.HorizontalClipping == eHorizontalClipping.Framebuffer) virtual_width = 736;

			int scanline_start = standard == OctoshockDll.eVidStandard.NTSC ? settings.ScanlineStart_NTSC : settings.ScanlineStart_PAL;
			int scanline_end = standard == OctoshockDll.eVidStandard.NTSC ? settings.ScanlineEnd_NTSC : settings.ScanlineEnd_PAL;
			int scanline_num = scanline_end - scanline_start + 1;
			//int scanline_num = h; // I wanted to do this, but our logic for mednafen modes here is based on un-doubled resolution. i could do a hack to divide it by 2 though
			int real_scanline_num = standard == OctoshockDll.eVidStandard.NTSC ? 240 : 288;

			int VirtualWidth=-1, VirtualHeight=-1;
			switch (settings.ResolutionMode)
			{
				case eResolutionMode.Mednafen:

					//mednafen uses 320xScanlines as the 1x size
					//it does change the 1x width when doing basic clipping.
					//and it does easily change the height when doing scanline removal.
					//now, our framebuffer cropping mode is more complex...
					VirtualWidth = (standard == OctoshockDll.eVidStandard.NTSC) ? 320 : 363;
					VirtualHeight = scanline_num;

					if (settings.HorizontalClipping == eHorizontalClipping.Basic)
						VirtualWidth = (standard == OctoshockDll.eVidStandard.NTSC) ? 302 : 384;

					if (settings.HorizontalClipping == eHorizontalClipping.Framebuffer)
					{
						//mednafen typically sends us a framebuffer with overscan. 350x240 is a nominal example here. it's squished inward to 320x240 for correct PAR.
						//ok: here we have a framebuffer without overscan. 320x240 nominal. So the VirtualWidth of what we got is off by a factor of 109.375%
						//so a beginning approach would be this:
						//VirtualWidth = (int)(VirtualWidth * 320.0f / 350);
						//but that will shrink things which are already annoyingly shrunken. 
						//therefore, lets do that, but then scale the whole window by the same factor so the width becomes unscaled and now the height is scaled up!
						//weird, huh?
						VirtualHeight = (int)(VirtualHeight * 350.0f / 320);

						//now unfortunately we may have lost vertical pixels. common in the case of PAL (rendering 256 on a field of 288)
						//therefore we'll be stretching way too much vertically here. 
						//lets add those pixels back with a new hack
						if (standard == OctoshockDll.eVidStandard.PAL)
						{
							if (h > 288) ret.Padding = new System.Drawing.Size(0, 576 - h);
							else ret.Padding = new System.Drawing.Size(0, 288 - h);
						}
						else
						{
							if (h > 288) ret.Padding = new System.Drawing.Size(0, 480 - h);
							else ret.Padding = new System.Drawing.Size(0, 240 - h);
						}
					}
					break;

				//384 / 288 = 1.3333333333333333333333333333333

				case eResolutionMode.TweakedMednafen:

					if (standard == OctoshockDll.eVidStandard.NTSC)
					{
						//dont make this 430, it's already been turned into 400 from 368+30 and then some fudge factor
						VirtualWidth = 400;
						VirtualHeight = (int)(scanline_num * 300.0f / 240);
						if (settings.HorizontalClipping == eHorizontalClipping.Basic)
							VirtualWidth = 378;
					}
					else
					{
						//this is a bit tricky. we know we want 400 for the virtualwidth. 
						VirtualWidth = 400;
						if (settings.HorizontalClipping == eHorizontalClipping.Basic)
							VirtualWidth = 378;
						//I'll be honest, I was just guessing here mostly
						//I need the AR to basically work out to be 363/288 (thats what it was in mednafen mode) so...
						VirtualHeight = (int)(scanline_num * (400.0f/363*288) / 288);
					}

					if (settings.HorizontalClipping == eHorizontalClipping.Framebuffer)
					{
						//see discussion above
						VirtualHeight = (int)(VirtualHeight * 350.0f / 320);

						if (standard == OctoshockDll.eVidStandard.PAL)
						{
							if (h > 288) ret.Padding = new System.Drawing.Size(0, 576 - h);
							else ret.Padding = new System.Drawing.Size(0, 288 - h);
						}
						else
						{
							if (h > 288) ret.Padding = new System.Drawing.Size(0, 480 - h);
							else ret.Padding = new System.Drawing.Size(0, 240 - h);
						}
					}
					break;

				case eResolutionMode.PixelPro:
					VirtualWidth = virtual_width;
					VirtualHeight = scanline_num * 2;
					break;

				case eResolutionMode.Debug:
					VirtualWidth = w;
					VirtualHeight = h;
					break;
			}

			ret.Resolution = new System.Drawing.Size(VirtualWidth, VirtualHeight);
			return ret;
		}

		void PokeDisc()
		{
			if (CurrentDiscIndexMounted == 0)
			{
				currentDiscInterface = null;
				OctoshockDll.shock_PokeDisc(psx, IntPtr.Zero);
			}
			else
			{
				currentDiscInterface = discInterfaces[CurrentDiscIndexMounted - 1];
				OctoshockDll.shock_PokeDisc(psx, currentDiscInterface.OctoshockHandle);
			}
		}

		void FrameAdvance_PrepDiscState()
		{
			//reminder: if this is the beginning of time, we can begin with the disc ejected or inserted.

			//if tray open is requested, and valid, apply it
			//in the first frame, go ahead and open it up so we have a chance to put a disc in it
			if (Controller.IsPressed("Open") && !CurrentTrayOpen || Frame == 0)
			{
				OctoshockDll.shock_OpenTray(psx);
				CurrentTrayOpen = true;
			}

			//change the disc if needed, and valid
			//also if frame is 0, we need to set a disc no matter what
			int requestedDisc = (int)Controller.GetFloat("Disc Select");
			if (requestedDisc != CurrentDiscIndexMounted && CurrentTrayOpen
				|| Frame == 0
				)
			{
				//dont replace default disc with the leave-default placeholder!
				if (requestedDisc == -1)
				{

				}
				else
				{
					CurrentDiscIndexMounted = requestedDisc;
				}

				if (CurrentDiscIndexMounted == 0 || discInterfaces.Count == 0)
				{
					currentDiscInterface = null;
					OctoshockDll.shock_SetDisc(psx, IntPtr.Zero);
				}
				else
				{
					currentDiscInterface = discInterfaces[CurrentDiscIndexMounted - 1];
					OctoshockDll.shock_SetDisc(psx, currentDiscInterface.OctoshockHandle);
				}
			}

			//if tray close is requested, and valid, apply it.
			if (Controller.IsPressed("Close") && CurrentTrayOpen)
			{
				OctoshockDll.shock_CloseTray(psx);
				CurrentTrayOpen = false;
			}

			//if frame is 0 and user has made no preference, close the tray
			if (!Controller.IsPressed("Close") && !Controller.IsPressed("Open") && Frame == 0 && CurrentTrayOpen)
			{
				OctoshockDll.shock_CloseTray(psx);
				CurrentTrayOpen = false;
			}
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			FrameAdvance_PrepDiscState();

			//clear drive light. itll get set to light up by sector-reading callbacks
			//TODO - debounce this by a frame or so perhaps?
			//TODO - actually, make this feedback from the core. there should be a register or status which effectively corresponds to whether it's reading.
			DriveLightOn = false;

			Frame++;

			SetInput();

			OctoshockDll.shock_SetLEC(psx, _SyncSettings.EnableLEC);

			var ropts = new OctoshockDll.ShockRenderOptions()
			{
				scanline_start = SystemVidStandard == OctoshockDll.eVidStandard.NTSC ? _Settings.ScanlineStart_NTSC : _Settings.ScanlineStart_PAL,
				scanline_end = SystemVidStandard == OctoshockDll.eVidStandard.NTSC ? _Settings.ScanlineEnd_NTSC : _Settings.ScanlineEnd_PAL,
			};
			if (_Settings.HorizontalClipping == eHorizontalClipping.Basic)
				ropts.renderType = OctoshockDll.eShockRenderType.ClipOverscan;
			if (_Settings.HorizontalClipping == eHorizontalClipping.Framebuffer)
				ropts.renderType = OctoshockDll.eShockRenderType.Framebuffer;

			if (_Settings.DeinterlaceMode == eDeinterlaceMode.Weave) ropts.deinterlaceMode = OctoshockDll.eShockDeinterlaceMode.Weave;
			if (_Settings.DeinterlaceMode == eDeinterlaceMode.Bob) ropts.deinterlaceMode = OctoshockDll.eShockDeinterlaceMode.Bob;
			if (_Settings.DeinterlaceMode == eDeinterlaceMode.BobOffset) ropts.deinterlaceMode = OctoshockDll.eShockDeinterlaceMode.BobOffset;

			OctoshockDll.shock_SetRenderOptions(psx, ref ropts);

			//prep tracer
			if (Tracer.Enabled)
				OctoshockDll.shock_SetTraceCallback(psx, IntPtr.Zero, trace_cb);
			else
				OctoshockDll.shock_SetTraceCallback(psx, IntPtr.Zero, null);

			//apply soft reset if needed
			if (Controller.IsPressed("Reset"))
				OctoshockDll.shock_SoftReset(psx);

			//------------------------
			OctoshockDll.shock_Step(psx, OctoshockDll.eShockStep.Frame);
			//------------------------

			//lag maintenance:
			int pad1 = OctoshockDll.shock_Peripheral_PollActive(psx, 0x01, true);
			int pad2 = OctoshockDll.shock_Peripheral_PollActive(psx, 0x02, true);
			IsLagFrame = true;
			if (pad1 == OctoshockDll.SHOCK_TRUE) IsLagFrame = false;
			if (pad2 == OctoshockDll.SHOCK_TRUE) IsLagFrame = false;
			if (_Settings.GPULag)
				IsLagFrame = OctoshockDll.shock_GetGPUUnlagged(psx) != OctoshockDll.SHOCK_TRUE;
			if (IsLagFrame)
				LagCount++;

			//what happens to sound in this case?
			if (render == false) return;

			OctoshockDll.ShockFramebufferInfo fb = new OctoshockDll.ShockFramebufferInfo();

			//run this once to get current logical size
			OctoshockDll.shock_GetFramebuffer(psx, ref fb);
			CurrentVideoSize = new System.Drawing.Size(fb.width, fb.height);

			if (_Settings.ResolutionMode == eResolutionMode.PixelPro)
				fb.flags = OctoshockDll.eShockFramebufferFlags.Normalize;

			OctoshockDll.shock_GetFramebuffer(psx, ref fb);

			int w = fb.width;
			int h = fb.height;
			BufferWidth = w;
			BufferHeight = h;

			var ri = CalculateResolution(this.SystemVidStandard, _Settings, w, h);
			VirtualWidth = ri.Resolution.Width;
			VirtualHeight = ri.Resolution.Height;
			VideoProvider_Padding = ri.Padding;

			int len = w * h;
			if (frameBuffer.Length != len)
			{
				Console.WriteLine("PSX FB size: {0},{1}", fb.width, fb.height);
				frameBuffer = new int[len];
			}

			fixed (int* ptr = frameBuffer)
			{
				fb.ptr = ptr;
				OctoshockDll.shock_GetFramebuffer(psx, ref fb);
			}

			fixed (short* samples = sbuff)
			{
				sbuffcontains = OctoshockDll.shock_GetSamples(psx, null);
				if (sbuffcontains * 2 > sbuff.Length) throw new InvalidOperationException("shock_GetSamples returned too many samples: " + sbuffcontains);
				OctoshockDll.shock_GetSamples(psx, samples);
			}
		}

		public ControllerDefinition ControllerDefinition { get; private set; }
		public IController Controller { get; set; }

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		public IInputCallbackSystem InputCallbacks
		{
			[FeatureNotImplemented]
			get
			{ throw new NotImplementedException(); }
		}

		[FeatureNotImplemented]
		public bool DeterministicEmulation { get { return true; } }

		public int[] GetVideoBuffer() { return frameBuffer; }
		public int VirtualWidth { get; private set; }
		public int VirtualHeight { get; private set; }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return 0; } }
		public System.Drawing.Size VideoProvider_Padding { get; private set; }

		#region Debugging

		OctoshockDll.ShockCallback_Mem mem_cb;

		void ShockMemCallback(uint address, OctoshockDll.eShockMemCb type, uint size, uint value)
		{
			switch (type)
			{
				case OctoshockDll.eShockMemCb.Read: 
					MemoryCallbacks.CallReads(address);
					break;
				case OctoshockDll.eShockMemCb.Write:
					MemoryCallbacks.CallWrites(address);
					break;
				case OctoshockDll.eShockMemCb.Execute:
					MemoryCallbacks.CallExecutes(address);
					break;
			}
		}

		void InitMemCallbacks()
		{
			mem_cb = new OctoshockDll.ShockCallback_Mem(ShockMemCallback);
			_memoryCallbacks.ActiveChanged += RefreshMemCallbacks;
		}

		void RefreshMemCallbacks()
		{
			OctoshockDll.eShockMemCb mask = OctoshockDll.eShockMemCb.None;
			if (MemoryCallbacks.HasReads) mask |= OctoshockDll.eShockMemCb.Read;
			if (MemoryCallbacks.HasWrites) mask |= OctoshockDll.eShockMemCb.Write;
			if (MemoryCallbacks.HasExecutes) mask |= OctoshockDll.eShockMemCb.Execute;
			OctoshockDll.shock_SetMemCb(psx, mem_cb, mask);
		}

		unsafe void SetMemoryDomains()
		{
			var mmd = new List<MemoryDomain>();
			IntPtr ptr;
			int size;

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.MainRAM);
			mmd.Add(MemoryDomain.FromIntPtr("MainRAM", size, MemoryDomain.Endian.Little, ptr, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.GPURAM);
			mmd.Add(MemoryDomain.FromIntPtr("GPURAM", size, MemoryDomain.Endian.Little, ptr, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.SPURAM);
			mmd.Add(MemoryDomain.FromIntPtr("SPURAM", size, MemoryDomain.Endian.Little, ptr, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.BiosROM);
			mmd.Add(MemoryDomain.FromIntPtr("BiosROM", size, MemoryDomain.Endian.Little, ptr, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.PIOMem);
			mmd.Add(MemoryDomain.FromIntPtr("PIOMem", size, MemoryDomain.Endian.Little, ptr, true, 4));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.DCache);
			mmd.Add(MemoryDomain.FromIntPtr("DCache", size, MemoryDomain.Endian.Little, ptr, true, 4));

			MemoryDomains = new MemoryDomainList(mmd);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}

		private IMemoryDomains MemoryDomains;

		#endregion

		#region ISoundProvider

		//private short[] sbuff = new short[1454 * 2]; //this is the most ive ever seen.. dont know why. two frames worth i guess
		private short[] sbuff = new short[1611 * 2]; //need this for pal
		private int sbuffcontains = 0;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = sbuff;
			nsamp = sbuffcontains;
		}

		public void DiscardSamples()
		{
			sbuffcontains = 0;
		}

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		#endregion

		#region ISaveRam

		public byte[] CloneSaveRam()
		{
			var cfg = _SyncSettings.FIOConfig.ToLogical();
			int nMemcards = cfg.NumMemcards;
			var buf = new byte[128 * 1024 * nMemcards];
			for (int i = 0, idx = 0, addr=0x01; i < 2; i++, addr<<=1)
			{
				if (cfg.Memcards[i])
				{
					fixed (byte* pbuf = buf)
					{
						var transaction = new OctoshockDll.ShockMemcardTransaction();
						transaction.buffer128k = pbuf + idx * 128 * 1024;
						transaction.transaction = OctoshockDll.eShockMemcardTransaction.Read;
						OctoshockDll.shock_Peripheral_MemcardTransact(psx, addr, ref transaction);
						idx++;
					}
				}
			}
			return buf;
		}

		public void StoreSaveRam(byte[] data)
		{
			var cfg = _SyncSettings.FIOConfig.ToLogical();
			for (int i = 0, idx = 0, addr = 0x01; i < 2; i++, addr <<= 1)
			{
				if (cfg.Memcards[i])
				{
					fixed (byte* pbuf = data)
					{
						var transaction = new OctoshockDll.ShockMemcardTransaction();
						transaction.buffer128k = pbuf + idx * 128 * 1024;
						transaction.transaction = OctoshockDll.eShockMemcardTransaction.Write;
						OctoshockDll.shock_Peripheral_MemcardTransact(psx, addr, ref transaction);
						idx++;
					}
				}
			}
		}

		public bool SaveRamModified
		{
			get
			{
				var cfg = _SyncSettings.FIOConfig.ToLogical();
				for (int i = 0, addr = 0x01; i < 2; i++, addr <<= 1)
				{
					if (cfg.Memcards[i])
					{
						var transaction = new OctoshockDll.ShockMemcardTransaction();
						transaction.transaction = OctoshockDll.eShockMemcardTransaction.CheckDirty;
						OctoshockDll.shock_Peripheral_MemcardTransact(psx, addr, ref transaction);
						if (OctoshockDll.shock_Peripheral_MemcardTransact(psx, addr, ref transaction) == OctoshockDll.SHOCK_TRUE)
							return true;
					}
				}

				return false;
			}
		}

		#endregion //ISaveRam


		#region Savestates
		//THIS IS STILL AWFUL

		JsonSerializer ser = new JsonSerializer() { Formatting = Formatting.Indented };

		class TextStateData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
			public bool CurrentDiscEjected;
			public int CurrentDiscIndexMounted;
		}

		public void SaveStateText(TextWriter writer)
		{
			var s = new TextState<TextStateData>();
			s.Prepare();

			var transaction = new OctoshockDll.ShockStateTransaction()
			{
				transaction = OctoshockDll.eShockStateTransaction.TextSave,
				ff = s.GetFunctionPointersSave()
			};
			int result = OctoshockDll.shock_StateTransaction(psx, ref transaction);
			if (result != OctoshockDll.SHOCK_OK)
				throw new InvalidOperationException("eShockStateTransaction.TextSave returned error!");

			s.ExtraData.IsLagFrame = IsLagFrame;
			s.ExtraData.LagCount = LagCount;
			s.ExtraData.Frame = Frame;
			s.ExtraData.CurrentDiscEjected = CurrentTrayOpen;
			s.ExtraData.CurrentDiscIndexMounted = CurrentDiscIndexMounted;

			ser.Serialize(writer, s);
			// TODO write extra copy of stuff we don't use (WHY?)
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (TextState<TextStateData>)ser.Deserialize(reader, typeof(TextState<TextStateData>));
			s.Prepare();
			var transaction = new OctoshockDll.ShockStateTransaction()
			{
				transaction = OctoshockDll.eShockStateTransaction.TextLoad,
				ff = s.GetFunctionPointersLoad()
			};

			int result = OctoshockDll.shock_StateTransaction(psx, ref transaction);
			if (result != OctoshockDll.SHOCK_OK)
				throw new InvalidOperationException("eShockStateTransaction.TextLoad returned error!");

			IsLagFrame = s.ExtraData.IsLagFrame;
			LagCount = s.ExtraData.LagCount;
			Frame = s.ExtraData.Frame;
			CurrentTrayOpen = s.ExtraData.CurrentDiscEjected;
			CurrentDiscIndexMounted = s.ExtraData.CurrentDiscIndexMounted;
			PokeDisc();
		}

		byte[] savebuff;
		byte[] savebuff2;

		void StudySaveBufferSize()
		{
			var transaction = new OctoshockDll.ShockStateTransaction();
			transaction.transaction = OctoshockDll.eShockStateTransaction.BinarySize;
			int size = OctoshockDll.shock_StateTransaction(psx, ref transaction);
			savebuff = new byte[size];
			savebuff2 = new byte[savebuff.Length + 4 + 4 + 4 + 1 + 1 + 4];
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			fixed (byte* psavebuff = savebuff)
			{
				var transaction = new OctoshockDll.ShockStateTransaction()
				{
					transaction = OctoshockDll.eShockStateTransaction.BinarySave,
					buffer = psavebuff,
					bufferLength = savebuff.Length
				};

				int result = OctoshockDll.shock_StateTransaction(psx, ref transaction);
				if (result != OctoshockDll.SHOCK_OK)
					throw new InvalidOperationException("eShockStateTransaction.BinarySave returned error!");
				writer.Write(savebuff.Length);
				writer.Write(savebuff);

				// other variables
				writer.Write(IsLagFrame);
				writer.Write(LagCount);
				writer.Write(Frame);
				writer.Write(CurrentTrayOpen);
				writer.Write(CurrentDiscIndexMounted);
			}
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			fixed (byte* psavebuff = savebuff)
			{
				var transaction = new OctoshockDll.ShockStateTransaction()
				{
					transaction = OctoshockDll.eShockStateTransaction.BinaryLoad,
					buffer = psavebuff,
					bufferLength = savebuff.Length
				};

				int length = reader.ReadInt32();
				if (length != savebuff.Length)
					throw new InvalidOperationException("Save buffer size mismatch!");
				reader.Read(savebuff, 0, length);
				int ret = OctoshockDll.shock_StateTransaction(psx, ref transaction);
				if (ret != OctoshockDll.SHOCK_OK)
					throw new InvalidOperationException("eShockStateTransaction.BinaryLoad returned error!");

				// other variables
				IsLagFrame = reader.ReadBoolean();
				LagCount = reader.ReadInt32();
				Frame = reader.ReadInt32();
				CurrentTrayOpen = reader.ReadBoolean();
				CurrentDiscIndexMounted = reader.ReadInt32();
				PokeDisc();
			}
		}

		public byte[] SaveStateBinary()
		{
			//this are objectionable shenanigans, but theyre required to get the extra info in the stream. we need a better approach.
			var ms = new MemoryStream(savebuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != savebuff2.Length)
				throw new InvalidOperationException();
			ms.Close();
			return savebuff2;
		}

		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		#endregion

		#region Settings

		Settings _Settings = new Settings();
		SyncSettings _SyncSettings;

		public enum eResolutionMode
		{
			PixelPro, Debug,
			Mednafen, TweakedMednafen
		}

		public class SyncSettings
		{
			public SyncSettings Clone()
			{
				return JsonConvert.DeserializeObject<SyncSettings>(JsonConvert.SerializeObject(this));
			}

			public bool EnableLEC;

			public SyncSettings()
			{
				//initialize with historical default settings
				var user = new OctoshockFIOConfigUser();
				user.Memcards[0] = user.Memcards[1] = true;
				user.Multitaps[0] = user.Multitaps[0] = false;
				user.Devices8[0] = OctoshockDll.ePeripheralType.DualShock;
				user.Devices8[4] = OctoshockDll.ePeripheralType.DualShock;
				FIOConfig = user;
			}

			public OctoshockFIOConfigUser FIOConfig;
		}

		public enum eHorizontalClipping
		{
			None,
			Basic,
			Framebuffer
		}

		public enum eDeinterlaceMode
		{
			Weave,
			Bob,
			BobOffset
		}

		public class Settings
		{
			[DisplayName("Determine Lag from GPU Frames")]
			[DefaultValue(false)]
			public bool GPULag { get; set; }

			[DisplayName("Resolution Mode")]
			[DefaultValue(eResolutionMode.PixelPro)]
			public eResolutionMode ResolutionMode { get; set; }

			[DisplayName("Horizontal Clipping")]
			[DefaultValue(eHorizontalClipping.None)]
			public eHorizontalClipping HorizontalClipping { get; set; }

			[DisplayName("ScanlineStart_NTSC")]
			[DefaultValue(0)]
			public int ScanlineStart_NTSC { get; set; }

			[DisplayName("ScanlineEnd_NTSC")]
			[DefaultValue(239)]
			public int ScanlineEnd_NTSC { get; set; }

			[DisplayName("ScanlineStart_PAL")]
			[DefaultValue(0)]
			public int ScanlineStart_PAL { get; set; }

			[DisplayName("ScanlineEnd_PAL")]
			[DefaultValue(287)]
			public int ScanlineEnd_PAL { get; set; }

			[DisplayName("DeinterlaceMode")]
			[DefaultValue(eDeinterlaceMode.Weave)]
			public eDeinterlaceMode DeinterlaceMode { get; set; }

			public void Validate()
			{
				if (ScanlineStart_NTSC < 0) ScanlineStart_NTSC = 0;
				if (ScanlineStart_PAL < 0) ScanlineStart_PAL = 0;
				if (ScanlineEnd_NTSC > 239) ScanlineEnd_NTSC = 239;
				if (ScanlineEnd_PAL > 287) ScanlineEnd_PAL = 287;

				//make sure theyre not in the wrong order
				if (ScanlineEnd_NTSC < ScanlineStart_NTSC)
				{
					int temp = ScanlineEnd_NTSC;
					ScanlineEnd_NTSC = ScanlineStart_NTSC;
					ScanlineStart_NTSC = temp;
				}
				if (ScanlineEnd_PAL < ScanlineStart_PAL)
				{
					int temp = ScanlineEnd_PAL;
					ScanlineEnd_PAL = ScanlineStart_PAL;
					ScanlineStart_PAL = temp;
				}
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}
		}

		public Settings GetSettings()
		{
			return _Settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _SyncSettings.Clone();
		}

		public bool PutSettings(Settings o)
		{
			_Settings.Validate();
			_Settings = o;

			//TODO - store settings into core? or we can just keep doing it before frameadvance

			return false;
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			//currently LEC and pad settings changes both require reboot
			bool reboot = true;

			//we could do it this way roughly if we need to
			//if(JsonConvert.SerializeObject(o.FIOConfig) != JsonConvert.SerializeObject(_SyncSettings.FIOConfig)


			_SyncSettings = o;


			return reboot;
		}

		#endregion
	}
}
