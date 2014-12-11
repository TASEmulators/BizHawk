//TODO hook up newer file ID stuff, think about how to combine it with the disc ID
//TODO change display manager to not require 0xFF alpha channel set on videoproviders. check gdi+ and opengl! this will get us a speedup in some places
//TODO Disc.Structure.Sessions[0].length_aba was 0

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

		public static readonly ControllerDefinition DualShockController = new ControllerDefinition
		{
			Name = "DualShock Controller",
			BoolButtons =
			{					
				"Up", "Down", "Left", "Right", 
				"Select", "Start",
				"Square", "Triangle", "Circle", "Cross", 
				"L1", "R1",  "L2", "R2", "L3", "R3", 
				"MODE",
			},
			FloatControls =
			{
				"LStick X", "LStick Y",
				"RStick X", "RStick Y",
			},
			FloatRanges = 
			{
				new[] {0.0f, 128.0f, 255.0f},
				new[] {255.0f, 128.0f, 0.0f},
				new[] {0.0f, 128.0f, 255.0f},
				new[] {255.0f, 128.0f, 0.0f},
			}
		};

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
			public DiscInterface(DiscSystem.Disc disc, Action cbActivity)
			{
				this.Disc = disc;
				cbReadTOC = ShockDisc_ReadTOC;
				cbReadLBA = ShockDisc_ReadLBA2448;
				this.cbActivity = cbActivity;
				OctoshockDll.shock_CreateDisc(out OctoshockHandle, IntPtr.Zero, disc.LBACount, cbReadTOC, cbReadLBA, true);
			}

			OctoshockDll.ShockDisc_ReadTOC cbReadTOC;
			OctoshockDll.ShockDisc_ReadLBA cbReadLBA;
			Action cbActivity;

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
				cbActivity();

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


		//note: its annoying that we have to have a disc before constructing this.
		//might want to change that later. HOWEVER - we need to definitely have a region, at least
		public Octoshock(CoreComm comm, DiscSystem.Disc disc)
		{
			ServiceProvider = new BasicServiceProvider(this);
			var domains = new List<MemoryDomain>();
			CoreComm = comm;

			CoreComm.UsesDriveLed = true;

			Attach();

			this.disc = disc;
			discInterface = new DiscInterface(disc, 
				() =>
				{
					//if current disc this delegate disc, activity is happening
					if (disc == this.disc)
						CoreComm.DriveLED = true;
				});

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


			//these should track values in octoshock gpu.cpp FillVideoParams
			//if (discInfo.region == OctoshockDll.eRegion.EU)
			//{
			//  VirtualWidth = 377; // " Dunno :( "
			//  VirtualHeight = 288;
			//}
			//else 
			//{
			//  VirtualWidth = 320; // Dunno :(
			//  VirtualHeight = 240;
			//}
			//BUT-for now theyre normalized (NOTE: THIS MESSES UP THE ASPECT RATIOS)
			VirtualWidth = 800;
			VirtualHeight = 480;


			OctoshockDll.shock_OpenTray(psx);
			OctoshockDll.shock_SetDisc(psx, discInterface.OctoshockHandle);
			OctoshockDll.shock_CloseTray(psx);
			OctoshockDll.shock_Peripheral_Connect(psx, 0x01, OctoshockDll.ePeripheralType.DualShock);
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

		static Octoshock()
		{
		}


		[FeatureNotImplemented]
		public void ResetCounters()
		{
			// FIXME when all this stuff is implemented
			Frame = 0;
		}

		void SetInput()
		{
			uint buttons = 0;
			
			//dualshock style
			if(Controller["Select"]) buttons |= 1;
			if (Controller["L3"]) buttons |= 2;
			if (Controller["R3"]) buttons |= 4;
			if (Controller["Start"]) buttons |= 8;
			if (Controller["Up"]) buttons |= 16;
			if (Controller["Right"]) buttons |= 32;
			if (Controller["Down"]) buttons |= 64;
			if (Controller["Left"]) buttons |= 128;
			if (Controller["L2"]) buttons |= 256;
			if (Controller["R2"]) buttons |= 512;
			if (Controller["L1"]) buttons |= 1024;
			if (Controller["R1"]) buttons |= 2048;
			if (Controller["Triangle"]) buttons |= 4096;
			if (Controller["Circle"]) buttons |= 8192;
			if (Controller["Cross"]) buttons |= 16384;
			if (Controller["Square"]) buttons |= 32768;
			if (Controller["MODE"]) buttons |= 65536;

			byte left_x = (byte)Controller.GetFloat("LStick X");
			byte left_y = (byte)Controller.GetFloat("LStick Y");
			byte right_x = (byte)Controller.GetFloat("RStick X");
			byte right_y = (byte)Controller.GetFloat("RStick Y");

			OctoshockDll.shock_Peripheral_SetPadInput(psx, 0x01, buttons, left_x, left_y, right_x, right_y);
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			Frame++;
			CoreComm.DriveLED = false;

			SetInput();

			OctoshockDll.shock_Step(psx, OctoshockDll.eShockStep.Frame);

			OctoshockDll.ShockFramebufferInfo fb = new OctoshockDll.ShockFramebufferInfo();
			fb.flags = OctoshockDll.eShockFramebufferFlags.Normalize;
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

		public ControllerDefinition ControllerDefinition { get { return DualShockController; } }
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
		public int VirtualHeight { get; private set; }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return 0; } }
		public void GetSamples(short[] samples) { }
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
	}
}
