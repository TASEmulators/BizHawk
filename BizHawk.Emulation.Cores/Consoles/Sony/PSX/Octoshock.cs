//TODO hook up newer file ID stuff, think about how to combine it with the disc ID
//TODO change display manager to not require 0xFF alpha channel set on videoproviders. check gdi+ and opengl! this will get us a speedup in some places
//TODO Disc.Structure.Sessions[0].length_aba was 0
//TODO add sram dump option (bold it if dirty) to file menu

using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

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
	public unsafe class Octoshock : IEmulator, IVideoProvider, ISyncSoundProvider, IMemoryDomains, ISaveRam, IStatable, IDriveLight
	{
		public string SystemId { get { return "PSX"; } }

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

		/// <summary>
		/// Wraps the ShockDiscRef returned from the DLL and acts as a bridge between it and a DiscSystem disc
		/// </summary>
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
		public Octoshock(CoreComm comm, DiscSystem.Disc disc, byte[] exe)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			DriveLightEnabled = true;

			Attach();

			this.disc = disc;

			string firmwareRegion = "U";
			OctoshockDll.eRegion region = OctoshockDll.eRegion.NA;

			if (disc != null)
			{
				discInterface = new DiscInterface(disc,
					() =>
					{
						//if current disc this delegate disc, activity is happening
						if (disc == this.disc)
							DriveLightOn = true;
					});

				//determine region of the provided disc
				OctoshockDll.ShockDiscInfo discInfo;
				OctoshockDll.shock_AnalyzeDisc(discInterface.OctoshockHandle, out discInfo);

				//try to acquire the appropriate firmware
				if (discInfo.region == OctoshockDll.eRegion.EU) firmwareRegion = "E";
				if (discInfo.region == OctoshockDll.eRegion.JP) firmwareRegion = "J";
			}
			else
			{
				//assume its NA region for test programs, for now. could it be read out of the ps-exe header?
			}

			byte[] firmware = comm.CoreFileProvider.GetFirmware("PSX", "U", true, "A PSX `" + firmwareRegion + "` region bios file is required");

			//create the instance
			fixed (byte* pFirmware = firmware)
				OctoshockDll.shock_Create(out psx, region, pFirmware);

			SetMemoryDomains();

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

			//set a default framebuffer
			BufferWidth = VirtualWidth;
			BufferHeight = VirtualHeight;
			frameBuffer = new int[BufferWidth*BufferHeight];

			if (disc != null)
			{
				OctoshockDll.shock_OpenTray(psx);
				OctoshockDll.shock_SetDisc(psx, discInterface.OctoshockHandle);
				OctoshockDll.shock_CloseTray(psx);
			}
			else
			{
				//must be an exe
				fixed (byte* pExeBuffer = exe)
					OctoshockDll.shock_MountEXE(psx, pExeBuffer, exe.Length);
			}
			OctoshockDll.shock_Peripheral_Connect(psx, 0x01, OctoshockDll.ePeripheralType.DualShock);

			//do this after framebuffers and peripherals and whatever crap are setup. kind of lame, but thats how it is for now
			StudySaveBufferSize();
		
			OctoshockDll.shock_PowerOn(psx);
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
			DriveLightOn = false;

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

			fixed (short* samples = sbuff)
			{
				sbuffcontains = OctoshockDll.shock_GetSamples(psx, null);
				if (sbuffcontains * 2 > sbuff.Length) throw new InvalidOperationException("shock_GetSamples returned too many samples: " + sbuffcontains);
				OctoshockDll.shock_GetSamples(psx, samples);
			}
		}

		public ControllerDefinition ControllerDefinition { get { return DualShockController; } }
		public IController Controller { get; set; }

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		[FeatureNotImplemented]
		public bool DeterministicEmulation { get { return true; } }

		public int[] GetVideoBuffer() { return frameBuffer; }
		public int VirtualWidth { get; private set; }
		public int VirtualHeight { get; private set; }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return 0; } }

		#region Debugging

		unsafe void SetMemoryDomains()
		{
			var mmd = new List<MemoryDomain>();
			IntPtr ptr;
			int size;

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.MainRAM);
			mmd.Add(MemoryDomain.FromIntPtr("MainRAM", size, MemoryDomain.Endian.Little, ptr, true));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.GPURAM);
			mmd.Add(MemoryDomain.FromIntPtr("GPURAM", size, MemoryDomain.Endian.Little, ptr, true));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.SPURAM);
			mmd.Add(MemoryDomain.FromIntPtr("SPURAM", size, MemoryDomain.Endian.Little, ptr, true));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.BiosROM);
			mmd.Add(MemoryDomain.FromIntPtr("BiosROM", size, MemoryDomain.Endian.Little, ptr, true));

			OctoshockDll.shock_GetMemData(psx, out ptr, out size, OctoshockDll.eMemType.PIOMem);
			mmd.Add(MemoryDomain.FromIntPtr("PIOMem", size, MemoryDomain.Endian.Little, ptr, true));

			MemoryDomains = new MemoryDomainList(mmd, 0);
		}

		public MemoryDomainList MemoryDomains { get; private set; }

		#endregion

		#region ISoundProvider

		private short[] sbuff = new short[1454*2]; //this is the most ive ever seen.. dont know why
		private int sbuffcontains = 0;

		public ISoundProvider SoundProvider { get { throw new InvalidOperationException(); } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = sbuff;
			nsamp = sbuffcontains;
		}

		public void DiscardSamples()
		{
			sbuffcontains = 0;
		}

		#endregion

		#region ISaveRam

		public byte[] CloneSaveRam()
		{
			var buf = new byte[128 * 1024];
			fixed (byte* pbuf = buf)
			{
				var transaction = new OctoshockDll.ShockMemcardTransaction();
				transaction.buffer128k = pbuf;
				transaction.transaction = OctoshockDll.eShockMemcardTransaction.Read;
				OctoshockDll.shock_Peripheral_MemcardTransact(psx, 0x01, ref transaction);
			}
			return buf;
		}

		public void StoreSaveRam(byte[] data)
		{
			fixed (byte* pbuf = data)
			{
				var transaction = new OctoshockDll.ShockMemcardTransaction();
				transaction.buffer128k = pbuf;
				transaction.transaction = OctoshockDll.eShockMemcardTransaction.Write;
				OctoshockDll.shock_Peripheral_MemcardTransact(psx, 0x01, ref transaction);
			}
		}

		public bool SaveRamModified
		{
			get
			{
				var transaction = new OctoshockDll.ShockMemcardTransaction();
				transaction.transaction = OctoshockDll.eShockMemcardTransaction.CheckDirty;
				return OctoshockDll.shock_Peripheral_MemcardTransact(psx, 0x01, ref transaction) == OctoshockDll.SHOCK_TRUE;
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
		}

		byte[] savebuff;
		byte[] savebuff2;

		void StudySaveBufferSize()
		{
			var transaction = new OctoshockDll.ShockStateTransaction();
			transaction.transaction = OctoshockDll.eShockStateTransaction.BinarySize;
			int size = OctoshockDll.shock_StateTransaction(psx, ref transaction);
			savebuff = new byte[size];
			savebuff2 = new byte[savebuff.Length + 13];
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
	}
}
