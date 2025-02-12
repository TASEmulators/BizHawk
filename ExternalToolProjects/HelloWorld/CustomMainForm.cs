using System;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Emulation.Common;

namespace HelloWorld
{
	/// <remarks>All of this is example code, but it's at least a little more substantiative than a simple "hello world".</remarks>
	[ExternalTool("HelloWorld", Description = "An example of how to interact with EmuHawk")]
//	[ExternalToolApplicability.SingleRom(VSystemID.Raw.NES, "EA343F4E445A9050D4B4FBAC2C77D0693B1D0922")] // example of limiting tool usage (this is SMB1)
	[ExternalToolEmbeddedIcon("HelloWorld.icon_Hello.ico")]
	public partial class CustomMainForm : ToolFormBase, IExternalToolForm
	{
		/// <remarks>
		/// <see cref="ApiContainer"/> can be used as a shorthand for accessing the various APIs, more like the Lua syntax.
		/// </remarks>
		public ApiContainer? _apiContainer { get; set; }

		private ApiContainer APIs
			=> _apiContainer!;

		/// <remarks>
		/// <see cref="RequiredServiceAttribute">RequiredServices</see> are populated by EmuHawk at runtime.
		/// These remain supported, but you should only use them when there is no API that does what you want.
		/// </remarks>
		[RequiredService]
		public IMemoryDomains? _memoryDomains { get; set; }

		private IMemoryDomains MemoryDomains
			=> _memoryDomains!;

		/// <remarks>
		/// An example of a hack. Hacks should be your last resort because they're prone to break with new releases.
		/// </remarks>
		private Config GlobalConfig
			=> ((EmulationApi) APIs.Emulation).ForbiddenConfigReference;

		private string SavestatePath
			=> Path.Combine(GlobalConfig.PathEntries.SaveStateAbsolutePath(APIs.Emulation.GetSystemId()), "Test");

		/// <remarks>
		/// Another hack because there's no API for controlling the RAM Watch tool.
		/// </remarks>
		private WatchList? _watches;

		private WatchList Watches
			=> _watches ??= new(MemoryDomains, APIs.Emulation.GetSystemId()) // technically this should be `GlobalEmulator.SystemId` instead of `GlobalGame.SystemId` (which this is) but it's close enough
			{
				Watch.GenerateWatch(MemoryDomains.MainMemory, 0x40, WatchSize.Byte, WatchDisplayType.Hex, bigEndian: true),
				Watch.GenerateWatch(MemoryDomains.MainMemory, 0x50, WatchSize.Word, WatchDisplayType.Unsigned, bigEndian: true),
				Watch.GenerateWatch(MemoryDomains.MainMemory, 0x60, WatchSize.DWord, WatchDisplayType.Hex, bigEndian: true),
			};

		private readonly Label[] WatchReadouts;

		protected override string WindowTitleStatic
			=> "HelloWorld";

		public CustomMainForm()
		{
			InitializeComponent();
			WatchReadouts = [ label_Watch1, label_Watch2, label_Watch3 ];
			label_GameHash.Click += (_, _) => Clipboard.SetText(APIs.Emulation.GetGameInfo()!.Hash);
			Closing += (_, _) =>
			{
				APIs.EmuClient.SetClientExtraPadding(0, 0, 0, 0);
				APIs.EmuClient.BeforeQuickSave -= HandleQuickSave;
				APIs.EmuClient.BeforeQuickLoad -= HandleQuickLoad;
			};
			Load += (_, _) =>
			{
				APIs.EmuClient.BeforeQuickSave += HandleQuickSave;
				APIs.EmuClient.BeforeQuickLoad += HandleQuickLoad;
			};
		}

		public void HandleQuickSave(object sender, BeforeQuickSaveEventArgs e)
		{
			if (e.Slot is not 0) return; // only take effect on slot 0
			var basePath = SavestatePath;
			if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
			APIs.EmuClient.SaveState(Path.Combine(basePath, e.Name));
			e.Handled = true;
		}

		public void HandleQuickLoad(object sender, BeforeQuickLoadEventArgs e)
		{
			if (e.Slot is not 0) return; // only take effect on slot 0
			APIs.EmuClient.LoadState(Path.Combine(SavestatePath, e.Name));
			e.Handled = true;
		}

		/// <remarks>This is called once when the form is opened, and every time a new movie session starts.</remarks>
		public override void Restart()
		{
			APIs.EmuClient.SetClientExtraPadding(left: 50, top: 50);
			var gi = APIs.Emulation.GetGameInfo();
			if (gi?.Name is { Length: not 0 } gameName)
			{
				Watches.RefreshDomains(MemoryDomains, GlobalConfig.RamWatchDefinePrevious);
				label_Game.Text = $"You're playing {gameName}";
				label_GameHash.Text = $"Hash: {gi.Hash}";
			}
			else
			{
				label_Game.Text = "You're playing... nothing";
				label_GameHash.Text = string.Empty;
			}
		}

		public override void UpdateValues(ToolFormUpdateType type)
		{
			if (type is not (ToolFormUpdateType.PreFrame or ToolFormUpdateType.FastPreFrame)
				|| string.IsNullOrEmpty(APIs.Emulation.GetGameInfo()?.Name)
				|| Watches.Count < 3)
			{
				return;
			}
			Watches.UpdateValues(GlobalConfig.RamWatchDefinePrevious);
			for (var i = 0; i < 3; i++)
			{
				WatchReadouts[i].Text = $"Watch 0x{Watches[i].AddressString} current value: {Watches[i].ValueString}";
			}
		}

		private void button1_Click(object sender, EventArgs e)
			=> APIs.EmuClient.DoFrameAdvance();

		private void loadstate_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(savestateName.Text)) APIs.EmuClient.LoadState(savestateName.Text);
		}

		private void saveState_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(savestateName.Text)) APIs.EmuClient.SaveState(savestateName.Text);
		}
	}
}
