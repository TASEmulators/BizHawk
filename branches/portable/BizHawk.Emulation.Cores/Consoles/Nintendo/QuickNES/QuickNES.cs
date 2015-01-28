using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Newtonsoft.Json;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	[CoreAttributes(
		"QuickNes",
		"",
		isPorted: true,
		isReleased: true,
		portedVersion: "0.7.0",
		portedUrl: "https://github.com/kode54/QuickNES"
		)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class QuickNES : IEmulator, IVideoProvider, ISyncSoundProvider, ISaveRam, IInputPollable,
		IStatable, IDebuggable, ISettable<QuickNES.QuickNESSettings, QuickNES.QuickNESSyncSettings>, Cores.Nintendo.NES.INESPPUViewable
	{
		static QuickNES()
		{
			LibQuickNES.qn_setup_mappers();
		}

		[CoreConstructor("NES")]
		public QuickNES(CoreComm comm, byte[] file, object Settings, object SyncSettings)
		{
			using (FP.Save())
			{
				ServiceProvider = new BasicServiceProvider(this);
				CoreComm = comm;

				Context = LibQuickNES.qn_new();
				if (Context == IntPtr.Zero)
					throw new InvalidOperationException("qn_new() returned NULL");
				try
				{
					LibQuickNES.ThrowStringError(LibQuickNES.qn_loadines(Context, file, file.Length));

					InitSaveRamBuff();
					InitSaveStateBuff();
					InitAudio();
					InitMemoryDomains();

					int mapper = 0;
					string mappername = Marshal.PtrToStringAnsi(LibQuickNES.qn_get_mapper(Context, ref mapper));
					Console.WriteLine("QuickNES: Booted with Mapper #{0} \"{1}\"", mapper, mappername);
					BoardName = mappername;
					CoreComm.VsyncNum = 39375000;
					CoreComm.VsyncDen = 655171;
					PutSettings((QuickNESSettings)Settings ?? new QuickNESSettings());

					_syncSettings = (QuickNESSyncSettings)SyncSettings ?? new QuickNESSyncSettings();
					_syncSettingsNext = _syncSettings.Clone();

					SetControllerDefinition();
					ComputeBootGod();
				}
				catch
				{
					Dispose();
					throw;
				}
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		#region FPU precision

		private class FPCtrl : IDisposable
		{
			[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern uint _control87(uint @new, uint mask);

			public static void PrintCurrentFP()
			{
				uint curr = _control87(0, 0);
				Console.WriteLine("Current FP word: 0x{0:x8}", curr);
			}

			uint cw;

			public IDisposable Save()
			{
				cw = _control87(0, 0);
				_control87(0x00000, 0x30000);
				return this;
			}
			public void Dispose()
			{
				_control87(cw, 0x30000);
			}
		}

		FPCtrl FP = new FPCtrl();

		#endregion

		#region Controller

		public ControllerDefinition ControllerDefinition { get; private set; }
		public IController Controller { get; set; }

		void SetControllerDefinition()
		{
			var def = new ControllerDefinition();
			def.Name = "NES Controller";
			def.BoolButtons.AddRange(new[] { "Reset", "Power" }); // console buttons
			if (_syncSettings.LeftPortConnected || _syncSettings.RightPortConnected)
				def.BoolButtons.AddRange(PadP1.Select(p => p.Name));
			if (_syncSettings.LeftPortConnected && _syncSettings.RightPortConnected)
				def.BoolButtons.AddRange(PadP2.Select(p => p.Name));
			ControllerDefinition = def;
		}

		private struct PadEnt
		{
			public readonly string Name;
			public readonly int Mask;
			public PadEnt(string Name, int Mask)
			{
				this.Name = Name;
				this.Mask = Mask;
			}
		}

		private static PadEnt[] GetPadList(int player)
		{
			string prefix = string.Format("P{0} ", player);
			return PadNames.Zip(PadMasks, (s, i) => new PadEnt(prefix + s, i)).ToArray();
		}

		private static string[] PadNames = new[]
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A"
			};
		private static int[] PadMasks = new[]
			{
				16, 32, 64, 128, 8, 4, 2, 1
			};

		private static PadEnt[] PadP1 = GetPadList(1);
		private static PadEnt[] PadP2 = GetPadList(2);

		private int GetPad(IEnumerable<PadEnt> buttons)
		{
			int ret = 0;
			foreach (var b in buttons)
			{
				if (Controller[b.Name])
					ret |= b.Mask;
			}
			return ret;
		}

		void SetPads(out int j1, out int j2)
		{
			if (_syncSettings.LeftPortConnected)
				j1 = GetPad(PadP1) | unchecked((int)0xffffff00);
			else
				j1 = 0;
			if (_syncSettings.RightPortConnected)
				j2 = GetPad(_syncSettings.LeftPortConnected ? PadP2 : PadP1) | unchecked((int)0xffffff00);
			else
				j2 = 0;
		}

		#endregion

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			CheckDisposed();
			using (FP.Save())
			{
				if (Controller["Power"])
					LibQuickNES.qn_reset(Context, true);
				if (Controller["Reset"])
					LibQuickNES.qn_reset(Context, false);

				int j1, j2;
				SetPads(out j1, out j2);

				Frame++;
				LibQuickNES.ThrowStringError(LibQuickNES.qn_emulate_frame(Context, j1, j2));
				IsLagFrame = LibQuickNES.qn_get_joypad_read_count(Context) == 0;
				if (IsLagFrame)
					LagCount++;

				if (render)
					Blit();
				if (rendersound)
					DrainAudio();

				if (CB1 != null) CB1();
				if (CB2 != null) CB2();
			}
		}

		IntPtr Context;

		public int Frame { get; private set; }

		public string SystemId { get { return "NES"; } }
		public bool DeterministicEmulation { get { return true; } }
		public string BoardName { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
			IsLagFrame = false;
			LagCount = 0;
		}

		public CoreComm CoreComm
		{
			get;
			private set;
		}

		#region bootgod

		public RomStatus? BootGodStatus { get; private set; }
		public string BootGodName { get; private set; }

		void ComputeBootGod()
		{
			// inefficient, sloppy, etc etc
			Emulation.Cores.Nintendo.NES.NES.BootGodDB.Initialize();
			var chrrom = _memoryDomains["CHR VROM"];
			var prgrom = _memoryDomains["PRG ROM"];

			var ms = new System.IO.MemoryStream();
			for (int i = 0; i < prgrom.Size; i++)
				ms.WriteByte(prgrom.PeekByte(i));
			if (chrrom != null)
				for (int i = 0; i < chrrom.Size; i++)
					ms.WriteByte(chrrom.PeekByte(i));

			string sha1 = BizHawk.Common.BufferExtensions.BufferExtensions.HashSHA1(ms.ToArray());
			Console.WriteLine("Hash for BootGod: {0}", sha1);
			sha1 = "sha1:" + sha1; // huh?
			var carts = Emulation.Cores.Nintendo.NES.NES.BootGodDB.Instance.Identify(sha1);
			if (carts.Count > 0)
			{
				Console.WriteLine("BootGod entry found: {0}", carts[0].name);
				switch (carts[0].system)
				{
					case "NES-PAL":
					case "NES-PAL-A":
					case "NES-PAL-B":
					case "Dendy":
						Console.WriteLine("Bad region {0}! Failing over...", carts[0].system);
						throw new UnsupportedGameException("Unsupported region!");
					default:
						break;
				}
				BootGodStatus = RomStatus.GoodDump;
				BootGodName = carts[0].name;
			}
			else
			{
				Console.WriteLine("No BootGod entry found.");
				BootGodStatus = null;
				BootGodName = null;
			}
		}

		#endregion

		public void Dispose()
		{
			if (Context != IntPtr.Zero)
			{
				LibQuickNES.qn_delete(Context);
				Context = IntPtr.Zero;
			}
		}

		void CheckDisposed()
		{
			if (Context == IntPtr.Zero)
				throw new ObjectDisposedException(GetType().Name);
		}

		#region SoundProvider

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		void InitAudio()
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_set_sample_rate(Context, 44100));
		}

		void DrainAudio()
		{
			NumSamples = LibQuickNES.qn_read_audio(Context, MonoBuff, MonoBuff.Length);
			unsafe
			{
				fixed (short* _src = &MonoBuff[0], _dst = &StereoBuff[0])
				{
					short* src = _src;
					short* dst = _dst;
					for (int i = 0; i < NumSamples; i++)
					{
						*dst++ = *src;
						*dst++ = *src++;
					}
				}
			}
		}

		short[] MonoBuff = new short[1024];
		short[] StereoBuff = new short[2048];
		int NumSamples = 0;

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = StereoBuff;
			nsamp = NumSamples;
		}

		public void DiscardSamples()
		{
		}

		#endregion
		}
}
