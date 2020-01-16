using System;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.ApiHawk.Classes.Events;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Here your first form
	/// /!\ it MUST be called CustomMainForm and implements IExternalToolForm
	/// Take also care of the namespace
	/// </summary>
	public partial class CustomMainForm : Form, IExternalToolForm
	{
		#region Fields

		/*
		The following stuff will be automatically filled
		by BizHawk runtime
		*/
		[RequiredService]
		internal IMemoryDomains _memoryDomains { get; set; }
		[RequiredService]
		private IEmulator _emu { get; set; }

		/*private members for our needed*/
		private WatchList _watches;

		#endregion

		#region cTor(s)

		public CustomMainForm()
		{
			InitializeComponent();
			label_GameHash.Click += Label_GameHash_Click;

			ClientApi.BeforeQuickSave += ClientApi_BeforeQuickSave;
			ClientApi.BeforeQuickLoad += ClientApi_BeforeQuickLoad;
		}

		#endregion

		#region Methods

		private void button1_Click(object sender, EventArgs e)
		{
			ClientApi.DoFrameAdvance();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			ClientApi.GetInput(1);
		}

		private void button3_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < 600; i++)
			{
				if (i % 60 == 0)
				{
					Joypad j1 = ClientApi.GetInput(1);
					j1.AddInput(JoypadButton.A);
					ClientApi.SetInput(1, j1);

					ClientApi.DoFrameAdvance();

					j1.RemoveInput(JoypadButton.A);
					ClientApi.SetInput(1, j1);
					ClientApi.DoFrameAdvance();
				}
				ClientApi.DoFrameAdvance();
			}
			Joypad j = ClientApi.GetInput(1);
			j.ClearInputs();
			ClientApi.SetInput(1, j);
		}

		private void Label_GameHash_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(Global.Game.Hash);
		}

		private void loadstate_Click(object sender, EventArgs e)
		{
			if (savestateName.Text.Trim() != string.Empty)
			{
				ClientApi.LoadState(savestateName.Text);
				//BinaryStateLoader.LoadAndDetect(savestateName.Text + ".State").GetLump(BinaryStateLump.Framebuffer, false, Test);
			}
		}

		/*private void Test(BinaryReader r)
		{
			System.Drawing.Bitmap b = new System.Drawing.Bitmap(r.BaseStream);
		}*/

		private void saveState_Click(object sender, EventArgs e)
		{
			if (savestateName.Text.Trim() != string.Empty)
			{
				ClientApi.SaveState(savestateName.Text);
			}
		}

		//We will override F10 quicksave behavior
		private void ClientApi_BeforeQuickSave(object sender, BeforeQuickSaveEventArgs e)
		{
			if(e.Slot == 0)
			{
				string basePath = Path.Combine(PathManager.GetSaveStatePath(Global.Game), "Test");
				if (!Directory.Exists(basePath))
				{
					Directory.CreateDirectory(basePath);
				}
				ClientApi.SaveState(Path.Combine(basePath, e.Name));
				e.Handled = true;
			}
		}

		//We will override F10 quickload behavior
		private void ClientApi_BeforeQuickLoad(object sender, BeforeQuickLoadEventArgs e)
		{
			if (e.Slot == 0)
			{
				string basePath = Path.Combine(PathManager.GetSaveStatePath(Global.Game), "Test");
				ClientApi.LoadState(Path.Combine(basePath, e.Name));
				e.Handled = true;
			}
		}

		#endregion

		#region BizHawk Required methods

		/// <summary>
		/// Return true if you want the <see cref="UpdateValues"/> method
		/// to be called before rendering
		/// </summary>
		public bool UpdateBefore
		{
			get
			{
				return true;
			}
		}

		public bool AskSaveChanges()
		{
			return true;
		}

		/// <summary>
		/// This method is called instead of regular <see cref="UpdateValues"/>
		/// when emulator is runnig in turbo mode
		/// </summary>
		public void FastUpdate()
		{ }

		public void NewUpdate(ToolFormUpdateType type) {}

		/// <summary>
		/// Restart is called the first time you call the form
		/// but also when you start playing a movie
		/// </summary>
		public void Restart()
		{
			//set a client padding
//			ClientApi.SetExtraPadding(50, 50);

			if (Global.Game.Name != "Null")
			{
				//first initialization of WatchList
				if (_watches == null)
				{
					_watches = new WatchList(_memoryDomains, _emu.SystemId ?? string.Empty);

					//Create some watch
					Watch myFirstWatch = Watch.GenerateWatch(_memoryDomains.MainMemory, 0x40, WatchSize.Byte, BizHawk.Client.Common.DisplayType.Hex, true);
					Watch mySecondWatch = Watch.GenerateWatch(_memoryDomains.MainMemory, 0x50, WatchSize.Word, BizHawk.Client.Common.DisplayType.Unsigned, true);
					Watch myThirdWatch = Watch.GenerateWatch(_memoryDomains.MainMemory, 0x60, WatchSize.DWord, BizHawk.Client.Common.DisplayType.Hex, true);

					//add them into the list
					_watches.Add(myFirstWatch);
					_watches.Add(mySecondWatch);
					_watches.Add(myThirdWatch);

					label_Game.Text = string.Format("You're playing {0}", Global.Game.Name);
					label_GameHash.Text = string.Format("Hash: {0}", Global.Game.Hash);
				}
				//refresh it
				else
				{
					_watches.RefreshDomains(_memoryDomains);
					label_Game.Text = string.Format("You're playing {0}", Global.Game.Name);
					label_GameHash.Text = string.Format("Hash: {0}", Global.Game.Hash);
				}
			}
			else
			{
				label_Game.Text = string.Format("You aren't playing to anything");
				label_GameHash.Text = string.Empty;
			}
		}

		/// <summary>
		/// This method is called when a frame is rendered
		/// You can comapre it the lua equivalent emu.frameadvance()
		/// </summary>
		public void UpdateValues()
		{
			if (Global.Game.Name != "Null")
			{
				//we update our watches
				_watches.UpdateValues();
				label_Watch1.Text = string.Format("First watch ({0}) current value: {1}", _watches[0].AddressString, _watches[0].ValueString);
				label_Watch2.Text = string.Format("Second watch ({0}) current value: {1}", _watches[1].AddressString, _watches[1].ValueString);
				label_Watch3.Text = string.Format("Third watch ({0}) current value: {1}", _watches[2].AddressString, _watches[2].ValueString);
			}
		}

		#endregion BizHawk Required methods
	}
}
