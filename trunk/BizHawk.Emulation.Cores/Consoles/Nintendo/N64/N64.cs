using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	[CoreAttributes(
		"Mupen64Plus",
		"",
		isPorted: true,
		isReleased: true,
		portedVersion: "2.0",
		portedUrl: "https://code.google.com/p/mupen64plus/"
		)]
	public partial class N64 : IEmulator, IMemoryDomains, ISaveRam, IDebuggable, IStatable,
		ISettable<N64Settings, N64SyncSettings>
	{
		private readonly N64Input _inputProvider;
		private readonly N64VideoProvider _videoProvider;
		private readonly N64Audio _audioProvider;

		private readonly EventWaitHandle _pendingThreadEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
		private readonly EventWaitHandle _completeThreadEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

		private mupen64plusApi api; // mupen64plus DLL Api
		
		private N64SyncSettings _syncSettings;
		private N64Settings _settings;

		private bool _pendingThreadTerminate;

		private DisplayType _display_type = DisplayType.NTSC;

		private Action _pendingThreadAction;

		private bool _disableExpansionSlot = true;

		/// <summary>
		/// Create mupen64plus Emulator
		/// </summary>
		/// <param name="comm">Core communication object</param>
		/// <param name="game">Game information of game to load</param>
		/// <param name="rom">Rom that should be loaded</param>
		/// <param name="syncSettings">N64SyncSettings object</param>
		[CoreConstructor("N64")]
		public N64(CoreComm comm, GameInfo game, byte[] file, object settings, object syncSettings)
		{
			int SaveType = 0;
			if (game.OptionValue("SaveType") == "EEPROM_16K")
			{
				SaveType = 1;
			}

			CoreComm = comm;

			_syncSettings = (N64SyncSettings)syncSettings ?? new N64SyncSettings();
			_settings = (N64Settings)settings ?? new N64Settings();

			_disableExpansionSlot = _syncSettings.DisableExpansionSlot;

			// Override the user's expansion slot setting if it is mentioned in the gamedb (it is mentioned but the game MUST have this setting or else not work
			if (game.OptionValue("expansionpak") != null && game.OptionValue("expansionpak") == "1")
			{
				_disableExpansionSlot = false;
				IsOverridingUserExpansionSlotSetting = true;
			}

			byte country_code = file[0x3E];
			switch (country_code)
			{
				// PAL codes
				case 0x44:
				case 0x46:
				case 0x49:
				case 0x50:
				case 0x53:
				case 0x55:
				case 0x58:
				case 0x59:
					_display_type = DisplayType.PAL;
					break;

				// NTSC codes
				case 0x37:
				case 0x41:
				case 0x45:
				case 0x4a:
				default: // Fallback for unknown codes
					_display_type = DisplayType.NTSC;
					break;
			}
			switch (DisplayType)
			{
				case DisplayType.NTSC:
					comm.VsyncNum = 60000;
					comm.VsyncDen = 1001;
					break;
				default:
					comm.VsyncNum = 50;
					comm.VsyncDen = 1;
					break;
			}

			StartThreadLoop();

			var videosettings = _syncSettings.GetVPS(game, _settings.VideoSizeX, _settings.VideoSizeY);
			var coreType = _syncSettings.Core;

			//zero 19-apr-2014 - added this to solve problem with SDL initialization corrupting the main thread (I think) and breaking subsequent emulators (for example, NES)
			//not sure why this works... if we put the plugin initializations in here, we get deadlocks in some SDL initialization. doesnt make sense to me...
			RunThreadAction(() =>
			{
				api = new mupen64plusApi(this, file, videosettings, SaveType, (int)coreType, _disableExpansionSlot);
			});

			// Order is important because the register with the mupen core
			_videoProvider = new N64VideoProvider(api, videosettings);
			_audioProvider = new N64Audio(api);
			_inputProvider = new N64Input(api, comm, this._syncSettings.Controllers);


			string rsp = _syncSettings.Rsp == N64SyncSettings.RspType.Rsp_Hle ?
				"mupen64plus-rsp-hle.dll" :
				"mupen64plus-rsp-z64-hlevideo.dll";

			api.AttachPlugin(mupen64plusApi.m64p_plugin_type.M64PLUGIN_RSP, rsp);

			InitMemoryDomains();
			RefreshMemoryCallbacks();

			api.AsyncExecuteEmulator();
			SetControllerButtons();
		}

		public bool UsingExpansionSlot
		{
			get { return !_disableExpansionSlot; }
		}

		public bool IsOverridingUserExpansionSlotSetting { get; set; }

		public void Dispose()
		{
			RunThreadAction(() =>
			{
				_videoProvider.Dispose();
				_audioProvider.Dispose();
				api.Dispose();
			});

			EndThreadLoop();
		}

		private void ThreadLoop()
		{
			for (; ; )
			{
				_pendingThreadEvent.WaitOne();
				_pendingThreadAction();
				if (_pendingThreadTerminate)
				{
					break;
				}

				_completeThreadEvent.Set();
			}

			_pendingThreadTerminate = false;
			_completeThreadEvent.Set();
		}

		private void RunThreadAction(Action action)
		{
			_pendingThreadAction = action;
			_pendingThreadEvent.Set();
			_completeThreadEvent.WaitOne();
		}

		private void StartThreadLoop()
		{
			var thread = new Thread(ThreadLoop) { IsBackground = true };
			thread.Start(); // will this solve the hanging process problem?
		}

		private void EndThreadLoop()
		{
			RunThreadAction(() => { _pendingThreadTerminate = true; });
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			IsVIFrame = false;

			_audioProvider.RenderSound = rendersound;

			if (Controller["Reset"])
			{
				api.soft_reset();
			}

			if (Controller["Power"])
			{
				api.hard_reset();
			}

			api.frame_advance();


			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;
		}

		public string SystemId { get { return "N64"; } }

		public string BoardName { get { return null; } }

		public CoreComm CoreComm { get; private set; }

		public IVideoProvider VideoProvider { get { return _videoProvider; } }

		public DisplayType DisplayType { get { return _display_type; } }

		public ISoundProvider SoundProvider { get { return null; } }

		public ISyncSoundProvider SyncSoundProvider { get { return _audioProvider.Resampler; } }

		public bool StartAsyncSound() { return false; }

		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition
		{
			get { return _inputProvider.ControllerDefinition; }
		}

		public IController Controller
		{
			get { return _inputProvider.Controller; }
			set { _inputProvider.Controller = value; }
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }

		public bool IsLagFrame
		{
			get
			{
				if (_settings.UseMupenStyleLag)
				{
					return !IsVIFrame;
				}

				return !_inputProvider.LastFrameInputPolled;
			}

			internal set
			{
				if (_settings.UseMupenStyleLag)
				{
					IsVIFrame = !value;
				}
				else
				{
					_inputProvider.LastFrameInputPolled = !value;
				}
			}
		}

		public bool IsVIFrame
		{
			get { return _videoProvider.IsVIFrame; }
			internal set { _videoProvider.IsVIFrame = value; }
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public bool DeterministicEmulation { get { return false; } }

		#region Savestates

		// these next 5 functions are all exact copy paste from gambatte.
		// if something's wrong here, it's probably wrong there too
		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);

			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			var hex = reader.ReadLine();
			var state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		private byte[] SaveStatePrivateBuff = new byte[16788288 + 1024];
		public void SaveStateBinary(BinaryWriter writer)
		{
			byte[] data = SaveStatePrivateBuff;
			int bytes_used = api.SaveState(data);

			writer.Write(bytes_used);
			writer.Write(data, 0, bytes_used);

			byte[] saveram = api.SaveSaveram();
			writer.Write(saveram);
			if (saveram.Length != mupen64plusApi.kSaveramSize)
			{
				throw new InvalidOperationException("Unexpected N64 SaveRam size");
			}

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if ((_disableExpansionSlot && length >= 16788288) || (!_disableExpansionSlot && length < 16788288))
			{
				throw new SavestateSizeMismatchException("Wrong N64 savestate size");
			}

			reader.Read(SaveStatePrivateBuff, 0, length);
			byte[] data = SaveStatePrivateBuff;

			api.LoadState(data);

			reader.Read(SaveStatePrivateBuff, 0, mupen64plusApi.kSaveramSize);
			api.LoadSaveram(SaveStatePrivateBuff);

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		private byte[] SaveStateBinaryPrivateBuff = new byte[0];

		public byte[] SaveStateBinary()
		{
			// WELCOME TO THE HACK ZONE
			byte[] saveram = api.SaveSaveram();

			int lenwant = 4 + SaveStatePrivateBuff.Length + saveram.Length + 1 + 4 + 4;
			if (SaveStateBinaryPrivateBuff.Length != lenwant)
			{
				Console.WriteLine("Allocating new N64 private buffer size {0}", lenwant);
				SaveStateBinaryPrivateBuff = new byte[lenwant];
			}

			var ms = new MemoryStream(SaveStateBinaryPrivateBuff);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();

			if (ms.Length != SaveStateBinaryPrivateBuff.Length)
			{
				throw new Exception("Unexpected Length");
			}

			return SaveStateBinaryPrivateBuff;
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		#endregion

		#region Debugging Hooks

		private mupen64plusApi.MemoryCallback readcb;
		private mupen64plusApi.MemoryCallback writecb;

		private void RefreshMemoryCallbacks()
		{
			var mcs = CoreComm.MemoryCallbackSystem;

			// we RefreshMemoryCallbacks() after the triggers in case the trigger turns itself off at that point
			if (mcs.HasReads)
				readcb = delegate(uint addr) { mcs.CallRead(addr); };
			else
				readcb = null;
			if (mcs.HasWrites)
				writecb = delegate(uint addr) { mcs.CallWrite(addr); };
			else
				writecb = null;

			api.setReadCallback(readcb);
			api.setWriteCallback(writecb);
		}

		#endregion
	}
}
