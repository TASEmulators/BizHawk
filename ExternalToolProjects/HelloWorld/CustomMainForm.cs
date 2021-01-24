using System;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Emulation.Common;

using DisplayType = BizHawk.Client.Common.DisplayType;

namespace HelloWorld
{
	/// <remarks>All of this is example code, but it's at least a little more substantiative than a simple "hello world".</remarks>
	[ExternalTool("HelloWorld", Description = "An example of how to interact with EmuHawk")]
//	[ExternalToolApplicability.SingleRom(CoreSystem.NES, "EA343F4E445A9050D4B4FBAC2C77D0693B1D0922")] // example of limiting tool usage (this is SMB1)
	[ExternalToolEmbeddedIcon("HelloWorld.icon_Hello.ico")]
	public partial class CustomMainForm : Form, IExternalToolForm
	{
		/// <remarks>
		/// <see cref="RequiredServiceAttribute">RequiredServices</see> are populated by EmuHawk at runtime.
		/// These remain supported, but you should only use them when there is no API that does what you want.
		/// </remarks>
		[RequiredService]
		private IEmulator? _emu { get; set; }

		[RequiredService]
		private IMemoryDomains? _memoryDomains { get; set; }

		/// <remarks>
		/// <see cref="RequiredApiAttribute">RequiredApis</see> are populated by EmuHawk at runtime.
		/// You can have props for any subset of the available APIs, or use an <see cref="ApiContainer"/> to get them all at once.
		/// </remarks>
		[RequiredApi]
		private IEmulationApi? _emuApi { get; set; }

		/// <remarks>
		/// <see cref="ApiContainer"/> can be used as a shorthand for accessing the various APIs, more like the Lua syntax.
		/// </remarks>
		public ApiContainer? _apiContainer { get; set; }

		private ApiContainer APIs => _apiContainer ?? throw new NullReferenceException();

		/// <remarks>
		/// An example of a hack. Hacks should be your last resort because they're prone to break with new releases.
		/// </remarks>
		private Config GlobalConfig => (_emuApi as EmulationApi ?? throw new Exception("required API wasn't fulfilled")).ForbiddenConfigReference;

		private WatchList? _watches;

		private WatchList Watches
		{
			get
			{
				WatchList CreateWatches()
				{
					var w = new WatchList(_memoryDomains, _emu?.SystemId ?? string.Empty);
					w.AddRange(new[] {
						Watch.GenerateWatch(_memoryDomains?.MainMemory, 0x40, WatchSize.Byte, DisplayType.Hex, true),
						Watch.GenerateWatch(_memoryDomains?.MainMemory, 0x50, WatchSize.Word, DisplayType.Unsigned, true),
						Watch.GenerateWatch(_memoryDomains?.MainMemory, 0x60, WatchSize.DWord, DisplayType.Hex, true)
					});
					return w;
				}
				_watches ??= CreateWatches();
				return _watches;
			}
		}

		public CustomMainForm()
		{
			InitializeComponent();
			label_GameHash.Click += label_GameHash_Click;
			Closing += (sender, args) => APIs.EmuClient.SetClientExtraPadding(0, 0, 0, 0);

			APIs.EmuClient.BeforeQuickSave += (sender, e) =>
			{
				if (e.Slot != 0) return; // only take effect on slot 0
				var basePath = Path.Combine(GlobalConfig.PathEntries.SaveStateAbsolutePath(APIs.Emulation.GetSystemId()), "Test");
				if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
				APIs.EmuClient.SaveState(Path.Combine(basePath, e.Name));
				e.Handled = true;
			};
			APIs.EmuClient.BeforeQuickLoad += (sender, e) =>
			{
				if (e.Slot != 0) return; // only take effect on slot 0
				var basePath = Path.Combine(GlobalConfig.PathEntries.SaveStateAbsolutePath(APIs.Emulation.GetSystemId()), "Test");
				APIs.EmuClient.LoadState(Path.Combine(basePath, e.Name));
				e.Handled = true;
			};
		}

		public bool AskSaveChanges() => true;

		/// <remarks>This is called once when the form is opened, and every time a new movie session starts.</remarks>
		public void Restart()
		{
			APIs.EmuClient.SetClientExtraPadding(50, 50);

			if (APIs.GameInfo.GetRomName() != "Null")
			{
				Watches.RefreshDomains(_memoryDomains, GlobalConfig.RamWatchDefinePrevious);
				label_Game.Text = $"You're playing {APIs.GameInfo.GetRomName()}";
				label_GameHash.Text = $"Hash: {APIs.GameInfo.GetRomHash()}";
			}
			else
			{
				label_Game.Text = "You're playing... nothing";
				label_GameHash.Text = string.Empty;
			}
		}

		public void UpdateValues(ToolFormUpdateType type)
		{
			if (!(type == ToolFormUpdateType.PreFrame || type == ToolFormUpdateType.FastPreFrame)
			    || APIs.GameInfo.GetRomName() == "Null"
			    || Watches.Count < 3)
			{
				return;
			}
			Watches.UpdateValues(GlobalConfig.RamWatchDefinePrevious);
			label_Watch1.Text = $"First watch ({Watches[0].AddressString}) current value: {Watches[0].ValueString}";
			label_Watch2.Text = $"Second watch ({Watches[1].AddressString}) current value: {Watches[1].ValueString}";
			label_Watch3.Text = $"Third watch ({Watches[2].AddressString}) current value: {Watches[2].ValueString}";
		}

		private void button1_Click(object sender, EventArgs e) => APIs.EmuClient.DoFrameAdvance();

		private void button2_Click(object sender, EventArgs e) => APIs.EmuClient.GetInput(1);

		private void button3_Click(object sender, EventArgs e)
		{
			for (var i = 0; i < 600; i++)
			{
				if (i % 60 == 0)
				{
					var j1 = APIs.EmuClient.GetInput(1);
					j1.AddInput(JoypadButton.A);
					APIs.EmuClient.SetInput(1, j1);

					APIs.EmuClient.DoFrameAdvance();

					j1.RemoveInput(JoypadButton.A);
					APIs.EmuClient.SetInput(1, j1);
					APIs.EmuClient.DoFrameAdvance();
				}
				APIs.EmuClient.DoFrameAdvance();
			}
			var j = APIs.EmuClient.GetInput(1);
			j.ClearInputs();
			APIs.EmuClient.SetInput(1, j);
		}

		private void label_GameHash_Click(object sender, EventArgs e) => Clipboard.SetText(APIs.GameInfo.GetRomHash());

		private void loadstate_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(savestateName.Text)) return;
			APIs.EmuClient.LoadState(savestateName.Text);
#if false
			static void Test(BinaryReader r)
			{
				var b = new System.Drawing.Bitmap(r.BaseStream);
			}
			BinaryStateLoader.LoadAndDetect($"{savestateName.Text}.State").GetLump(BinaryStateLump.Framebuffer, false, Test);
#endif
		}

		private void saveState_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(savestateName.Text)) APIs.EmuClient.SaveState(savestateName.Text);
		}
	}
}
