using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.DiscSystem;

namespace BizHawk
{

	public class PsxCore : ExternalCore, IEmulator, IVideoProvider, ISoundProvider
	{
		public PsxCore(IExternalCoreAccessor accessor)
			: base(accessor)
		{
			var domains = new List<MemoryDomain>(1);
			memoryDomains = domains.AsReadOnly();
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();

			mDiscInterface = new DiscInterface(mAccessor);

			UnmanagedOpaque = QueryCoreCall<Func<IntPtr,IntPtr>>("PsxCore.Construct")(ManagedOpaque);

			QueryCoreCall(out cGetResolution, "PsxCore.GetResolution");
			QueryCoreCall(out cUpdateVideoBuffer, "PsxCore.UpdateVideoBuffer");
			QueryCoreCall(out cFrameAdvance, "PsxCore.FrameAdvance");
		}

		DiscInterface mDiscInterface;
		public void SetDiscHopper(DiscHopper hopper)
		{
			mDiscInterface.DiscHopper = hopper;
		}

		public override IntPtr ClientSignal(string type, IntPtr obj, string param, IntPtr value)
		{
			if (param == "GetDiscInterface")
			{
				return mDiscInterface.UnmanagedOpaque;
			}
			return base.ClientSignal(type, obj, param, value);
		}

		Func<System.Drawing.Size> cGetResolution;
		Action cFrameAdvance;
		Action<IntPtr> cUpdateVideoBuffer;

		//video provider
		int[] videoBuffer = new int[256 * 256];
		public int[] GetVideoBuffer() { return videoBuffer; }
		public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 192; } }
		public int BackgroundColor { get { return 0; } }

		public void ResetFrameCounter()
		{
			Frame = 0;
		}

		public string SystemId { get { return "PSX"; } }
		public static readonly ControllerDefinition NullController = new ControllerDefinition { Name = "Null Controller" };

		private Random rand = new Random();
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return this; } }
		public unsafe void FrameAdvance(bool render)
		{
			//if (render == false) return;
			cFrameAdvance();
			fixed (int* vidbuf = &videoBuffer[0])
				cUpdateVideoBuffer(new IntPtr(vidbuf));
		}
		public ControllerDefinition ControllerDefinition { get { return NullController; } }
		public IController Controller { get; set; }

		public int Frame { get; set; }
		public int LagCount { get { return 0; } set { return; } }
		public bool IsLagFrame { get { return false; } }

		public byte[] SaveRam { get { return new byte[0]; } }
		public bool DeterministicEmulation { get; set; }
		public bool SaveRamModified { get; set; }
		public void SaveStateText(TextWriter writer) { }
		public void LoadStateText(TextReader reader) { }
		public void SaveStateBinary(BinaryWriter writer) { }
		public void LoadStateBinary(BinaryReader reader) { }
		public byte[] SaveStateBinary() { return new byte[1]; }
		public void GetSamples(short[] samples) { }
		public void DiscardSamples() { }
        public int MaxVolume { get; set; }
		private IList<MemoryDomain> memoryDomains;
		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }
		public void Dispose() { }
	}
}