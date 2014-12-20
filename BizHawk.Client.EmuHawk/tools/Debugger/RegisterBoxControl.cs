using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RegisterBoxControl : UserControl
	{
		public IDebuggable Core { get; set; }
		public GenericDebugger ParentDebugger { get; set; }

		private bool _supressChangeEvents = false;

		public RegisterBoxControl()
		{
			InitializeComponent();
			AutoScroll = true;
		}

		private void RegisterBoxControl_Load(object sender, EventArgs e)
		{

		}

		public void UpdateValues()
		{
			if (this.Enabled)
			{
				var registers = Core.GetCpuFlagsAndRegisters();
				

				_supressChangeEvents = true;

				foreach (var register in registers)
				{
					Controls
						.OfType<Panel>()
						.First(p => p.Name == "FlagPanel")
						.Controls
						.OfType<CheckBox>()
						.ToList()
						.ForEach(checkbox =>
						{
							if (checkbox.Name == register.Key)
							{
								checkbox.Checked = register.Value.Value == 1;
							}
						});

					Controls
						.OfType<TextBox>()
						.ToList()
						.ForEach(textbox =>
						{
							if (textbox.Name == register.Key)
							{
								textbox.Text = register.Value.Value.ToHexString(register.Value.BitSize / 16);
							}
						});
				}

				_supressChangeEvents = false;
			}
		}

		private bool CanGetCpuRegisters
		{
			get
			{
				try
				{
					var registers = Core.GetCpuFlagsAndRegisters();
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}
		}

		private bool CanSetCpuRegisters
		{
			get
			{
				try
				{
					Core.SetCpuRegister("", 0);
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}
				catch (Exception)
				{
					return true;
				}
			}
		}

		public void GenerateUI()
		{
			var canget = CanGetCpuRegisters;
			var canset = CanSetCpuRegisters;

			if (!canget && !canset)
			{
				ParentDebugger.DisableRegisterBox();
				this.Enabled = false;
			}

			var registers = Core.GetCpuFlagsAndRegisters();

			int y = 0;
			foreach (var register in registers.Where(r => r.Value.BitSize != 1))
			{
				this.Controls.Add(new Label
				{
					Text = register.Key.Replace("Flag ", "") + (canset ? ": " : ""),
					Location = new Point(5, y + 2),
					Width = 35
				});

				if (canset)
				{
					var t = new TextBox
					{
						Name = register.Key,
						Text = register.Value.Value.ToHexString(register.Value.BitSize / 16),
						Width = 6 + ((register.Value.BitSize / 4) * 9),
						Location = new Point(40, y),
						MaxLength = register.Value.BitSize / 4,
						CharacterCasing = CharacterCasing.Upper
					};

					t.TextChanged += (o, e) =>
					{
						if (!_supressChangeEvents)
						{
							try
							{
								Core.SetCpuRegister(t.Name, int.Parse(t.Text));
							}
							catch (InvalidOperationException)
							{
								t.Enabled = false;
							}
						}
					};

					this.Controls.Add(t);
				}
				else
				{
					this.Controls.Add(new Label
					{
						Name = register.Key,
						Text = register.Value.ToString(),
						Width = 45,
						Location = new Point(40, y)
					});
				}

				y += 25;
			}

			var flags = registers.Where(r => r.Value.BitSize == 1);

			if (flags.Any())
			{
				var p = new Panel
				{
					Name = "FlagPanel",
					Location = new Point(5, y),
					BorderStyle = BorderStyle.None,
					Size = new Size(240, 23),
					AutoScroll = true
				};

				foreach (var flag in registers.Where(r => r.Value.BitSize == 1).OrderByDescending(x => x.Key))
				{
					var c = new CheckBox
					{
						Appearance = System.Windows.Forms.Appearance.Button,
						Name = flag.Key,
						Text = flag.Key.Replace("Flag", "").Trim(), // Hack
						Checked = flag.Value.Value == 1 ? true : false,
						Location = new Point(40, y),
						Dock = DockStyle.Left,
						Size = new Size(23, 23)
					};

					c.CheckedChanged += (o, e) =>
					{
						if (!_supressChangeEvents)
						{
							try
							{
								Core.SetCpuRegister(c.Name, c.Checked ? 1 : 0);
							}
							catch (InvalidOperationException) // TODO: This is hacky stuff because NES doesn't support setting flags!  Need to know when a core supports this or not, and enable/disable the box accordingly
							{
								_supressChangeEvents = true;
								c.Checked = !c.Checked;
								_supressChangeEvents = false;
								c.Enabled = false;
							}
						}
					};

					p.Controls.Add(c);
				}

				this.Controls.Add(p);
			}
		}
	}
}
