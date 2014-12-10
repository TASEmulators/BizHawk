//TODO hook up newer file ID stuff, think about how to combine it with the disc ID
//TODO not liking the name ShockFramebufferJob
//TODO change display manager to not require 0xFF alpha channel set on videoproviders. check gdi+ and opengl! this will get us a speedup in some places
//TODO Disc.Structure.Sessions[0].length_aba was 0

//looks like we can have (in NTSC) framebuffer dimensions like this:
//width: 280, 350, 700
//height: 240, 480
//mednafen's strategy is to put everything in a 320x240 and scale it up 3x to 960x720 by default (which is adequate to contain the largest PSX framebuffer)
//heres my strategy.
//1. we should have a native output mode, for debugging. but most users wont want it (massively distorted resolutions are common in games)
//2. do the right thing: 
//always double a height of 240, and double a width of 280 or 350. For 280, float content in center screen.
//but lets not do this til we're on an upgraded mednafen

using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

#pragma warning disable 649 //adelikat: Disable dumb warnings until this file is complete

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	[CoreAttributes(
		"Octoshock",
		"Ryphecha",
		isPorted: true,
		isReleased: false
		)]
	public unsafe class Octoshock : IEmulator, IVideoProvider, ISoundProvider
	{
		public string SystemId { get { return "NULL"; } }
		public static readonly ControllerDefinition NullController = new ControllerDefinition { Name = "Null Controller" };

		public string BoardName { get { return null; } }

		private int[] frameBuffer = new int[0];
		private Random rand = new Random();
		public CoreComm CoreComm { get; private set; }
		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return this; } }
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(this, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		//we can only have one active core at a time, due to the lib being so static.
		//so we'll track the current one here and detach the previous one whenever a new one is booted up.
		static Octoshock CurrOctoshockCore;
		
		IntPtr psx;
		DiscSystem.Disc disc;
		DiscInterface discInterface;

		bool disposed = false;
		public void Dispose()
		{
			if (disposed) return;

			OctoshockDll.shock_Destroy(psx);
			psx = IntPtr.Zero;

			disposed = true;
		}

		class DiscInterface : IDisposable
		{
			public DiscInterface(DiscSystem.Disc disc)
			{
				this.Disc = disc;
				cbReadTOC = ShockDisc_ReadTOC;
				cbReadLBA = ShockDisc_ReadLBA2448;
				OctoshockDll.shock_CreateDisc(out OctoshockHandle, IntPtr.Zero, disc.LBACount, cbReadTOC, cbReadLBA, true);
			}

			OctoshockDll.ShockDisc_ReadTOC cbReadTOC;
			OctoshockDll.ShockDisc_ReadLBA cbReadLBA;

			public DiscSystem.Disc Disc;
			public IntPtr OctoshockHandle;

			public void Dispose()
			{
				OctoshockDll.shock_DestroyDisc(OctoshockHandle);
				OctoshockHandle = IntPtr.Zero;
			}

			int ShockDisc_ReadTOC(IntPtr opaque, OctoshockDll.ShockTOC* read_target, OctoshockDll.ShockTOCTrack* tracks101)
			{
				read_target->disc_type = 1; //hardcoded in octoshock
				read_target->first_track = (byte)Disc.TOCRaw.FirstRecordedTrackNumber; //i _think_ thats what is meant here
				read_target->last_track = (byte)Disc.TOCRaw.LastRecordedTrackNumber; //i _think_ thats what is meant here

				tracks101[0].lba = tracks101[0].adr = tracks101[0].control = 0;

				for (int i = 1; i < 100; i++)
				{
					var item = Disc.TOCRaw.TOCItems[i];
					tracks101[i].adr = 1; //not sure what this is
					tracks101[i].lba = (uint)item.LBATimestamp.Sector;
					tracks101[i].control = (byte)item.Control;
				}

				////the lead-out track is to be synthesized
				tracks101[read_target->last_track + 1].adr = 1;
				tracks101[read_target->last_track + 1].control = 0;
				tracks101[read_target->last_track + 1].lba = (uint)Disc.TOCRaw.LeadoutTimestamp.Sector;
				////laaaame
				//tracks101[read_target->last_track + 1].lba =
				//  (uint)(
				//  Disc.Structure.Sessions[0].Tracks[read_target->last_track - 1].Start_ABA //AUGH. see comment in Start_ABA
				//  + Disc.Structure.Sessions[0].Tracks[read_target->last_track - 1].LengthInSectors
				//  - 150
				//  );

				//element 100 is to be copied as the lead-out track
				tracks101[100] = tracks101[read_target->last_track + 1];

				return OctoshockDll.SHOCK_OK;
			}

			byte[] SectorBuffer = new byte[2352];

			int ShockDisc_ReadLBA2448(IntPtr opaque, int lba, void* dst)
			{
				//lets you check subcode generation by logging it and checking against the CCD subcode
				bool subcodeLog = false;
				bool readLog = false;

				if (subcodeLog) Console.Write("{0}|", lba);
				else if (readLog) Console.WriteLine("Read Sector: " + lba);

				Disc.ReadLBA_2352(lba, SectorBuffer, 0);
				Marshal.Copy(SectorBuffer, 0, new IntPtr(dst), 2352);
				Disc.ReadLBA_SectorEntry(lba).SubcodeSector.ReadSubcodeDeinterleaved(SectorBuffer, 0);
				Marshal.Copy(SectorBuffer, 0, new IntPtr((byte*)dst + 2352), 96);

				if (subcodeLog)
				{
					for (int i = 0; i < 24; i++)
						Console.Write("{0:X2}", *((byte*)dst + 2352 + i));
					Console.WriteLine();
				}
				

				return OctoshockDll.SHOCK_OK;
			}
		}


		public Octoshock(CoreComm comm, DiscSystem.Disc disc)
		{
			ServiceProvider = new BasicServiceProvider(this);
			var domains = new List<MemoryDomain>();
			CoreComm = comm;
			VirtualWidth = BufferWidth = 256;
			BufferHeight = 192;

			Attach();

			this.disc = disc;
			discInterface = new DiscInterface(disc);

			//determine region of the provided disc
			OctoshockDll.ShockDiscInfo discInfo;
			OctoshockDll.shock_AnalyzeDisc(discInterface.OctoshockHandle, out discInfo);

			//try to acquire the appropriate firmware
			string firmwareRegion = "U";
			if(discInfo.region == OctoshockDll.eRegion.EU) firmwareRegion = "E";
			if (discInfo.region == OctoshockDll.eRegion.JP) firmwareRegion = "J";
			byte[] firmware = comm.CoreFileProvider.GetFirmware("PSX", "U", true, "A PSX `" + firmwareRegion + "` region bios file is required");

			//create the instance
			fixed (byte* pFirmware = firmware)
				OctoshockDll.shock_Create(out psx, discInfo.region, pFirmware);

			OctoshockDll.shock_OpenTray(psx);
			OctoshockDll.shock_SetDisc(psx, discInterface.OctoshockHandle);
			OctoshockDll.shock_CloseTray(psx);
			OctoshockDll.shock_PowerOn(psx);
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		void Attach()
		{
			//attach this core as the current
			if (CurrOctoshockCore != null)
				CurrOctoshockCore.Dispose();
			CurrOctoshockCore = this;

			//the psx instance cant be created until the desired region is known, which needs a disc, so we need the dll static attached first
		}

		//public void LoadCuePath(string path)
		//{
		//  Attach();
		//  DiscSystem.Disc.FromCCDPath
		//}


		static Octoshock()
		{
		}


		[FeatureNotImplemented]
		public void ResetCounters()
		{
			// FIXME when all this stuff is implemented
			Frame = 0;
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			OctoshockDll.shock_Step(psx, OctoshockDll.eShockStep.Frame);

			OctoshockDll.ShockFramebufferJob fb = new OctoshockDll.ShockFramebufferJob();
			OctoshockDll.shock_GetFramebuffer(psx, ref fb);

			//Console.WriteLine(fb.height);

			if (render == false) return;

			int w = fb.width;
			int h = fb.height;
			BufferWidth = w;
			BufferHeight = h;

			int len = w*h;
			if (frameBuffer.Length != len)
			{
				Console.WriteLine("PSX FB size: {0},{1}", fb.width, fb.height);
				frameBuffer = new int[len];
			}

			fixed (int* ptr = frameBuffer)
			{
				fb.ptr = ptr;
				OctoshockDll.shock_GetFramebuffer(psx, ref fb);
				//alpha channel is added in c++, right now. wish we didnt have to do it at all
			}
		}

		[FeatureNotImplemented]
		public ControllerDefinition ControllerDefinition { get { return NullController; } }

		[FeatureNotImplemented]
		public IController Controller { get; set; }

		public int Frame
		{
			[FeatureNotImplemented]
			get;

			[FeatureNotImplemented]
			set;
		}

		[FeatureNotImplemented]
		public bool DeterministicEmulation { get { return true; } }

		public int[] GetVideoBuffer() { return frameBuffer; }
		public int VirtualWidth { get; private set; }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return 0; } }
		public void GetSamples(short[] samples) { }
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
	}
}
