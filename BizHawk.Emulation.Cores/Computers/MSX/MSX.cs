using System;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	[Core(
		"MSXHawk",
		"",
		isPorted: false,
		isReleased: false)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class MSX : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IStatable, IInputPollable, IRegionable, ISettable<MSX.MSXSettings, MSX.MSXSyncSettings>
	{
		[CoreConstructor("MSX")]
		public MSX(CoreComm comm, GameInfo game, byte[] rom, object settings, object syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			Settings = (MSXSettings)settings ?? new MSXSettings();
			SyncSettings = (MSXSyncSettings)syncSettings ?? new MSXSyncSettings();
			CoreComm = comm;

			RomData = rom;
			int size = RomData.Length;

			if (RomData.Length % BankSize != 0)
			{
				Array.Resize(ref RomData, ((RomData.Length / BankSize) + 1) * BankSize);
			}

			// we want all ROMS to be multiples of 64K for easy memory mapping later
			if (RomData.Length != 0x10000)
			{
				Array.Resize(ref RomData, 0x10000);
			}

			// if the original was not 64 or 48 k, move it (may need to do this case by case)

			if (size == 0x8000)
			{
				for (int i = 0x7FFF; i >= 0; i--)
				{
					RomData[i + 0x4000] = RomData[i];
				}
				for (int i = 0; i < 0x4000; i++)
				{
					RomData[i] = 0; 
				}
			}

			if (size == 0x4000)
			{
				for (int i = 0; i < 0x4000; i++)
				{
					RomData[i + 0x4000] = RomData[i];
					RomData[i + 0x8000] = RomData[i];
					RomData[i + 0xC000] = RomData[i];
				}
			}

			Bios = comm.CoreFileProvider.GetFirmware("MSX", "bios", true, "BIOS Not Found, Cannot Load");
			Basic = comm.CoreFileProvider.GetFirmware("MSX", "basic", true, "BIOS Not Found, Cannot Load");

			Basic = new byte[0x4000];

			MSX_Pntr = LibMSX.MSX_create();

			LibMSX.MSX_load_bios(MSX_Pntr, Bios, Basic);
			LibMSX.MSX_load(MSX_Pntr, RomData, (uint)RomData.Length, 0, RomData, (uint)RomData.Length, 0);

			blip.SetRates(3579545, 44100);

			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(this);

			SetupMemoryDomains();

			InputCallbacks = new InputCallbackSystem();

			Header_Length = LibMSX.MSX_getheaderlength(MSX_Pntr);
			Disasm_Length = LibMSX.MSX_getdisasmlength(MSX_Pntr);
			Reg_String_Length = LibMSX.MSX_getregstringlength(MSX_Pntr);

			StringBuilder new_header = new StringBuilder(Header_Length);
			LibMSX.MSX_getheader(MSX_Pntr, new_header, Header_Length);

			Console.WriteLine(Header_Length + " " + Disasm_Length + " " + Reg_String_Length);

			Tracer = new TraceBuffer { Header = new_header.ToString() };

			var serviceProvider = ServiceProvider as BasicServiceProvider;
			serviceProvider.Register<ITraceable>(Tracer);
		}

		public void HardReset()
		{

		}

		IntPtr MSX_Pntr { get; set; } = IntPtr.Zero;
		byte[] MSX_core = new byte[0x20000];
		public static byte[] Bios;
		public static byte[] Basic;

		// Constants
		private const int BankSize = 16384;

		// ROM
		public byte[] RomData;

		// Machine resources
		private IController _controller = NullController.Instance;

		private int _frame = 0;

		public DisplayType Region => DisplayType.NTSC;

		#region Trace Logger
		private ITraceable Tracer;

		private LibMSX.TraceCallback tracecb;

		// these will be constant values assigned during core construction
		private int Header_Length;
		private int Disasm_Length;
		private int Reg_String_Length;

		private void MakeTrace(int t)
		{
			StringBuilder new_d = new StringBuilder(Disasm_Length);
			StringBuilder new_r = new StringBuilder(Reg_String_Length);

			LibMSX.MSX_getdisassembly(MSX_Pntr, new_d, t, Disasm_Length);
			LibMSX.MSX_getregisterstate(MSX_Pntr, new_r, t, Reg_String_Length);

			Tracer.Put(new TraceInfo
			{
				Disassembly = new_d.ToString().PadRight(36),
				RegisterInfo = new_r.ToString()
			});
		}

		#endregion

		private MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem(new[] { "System Bus" });
		public IMemoryCallbackSystem MemoryCallbacks => _memorycallbacks;
	}
}
