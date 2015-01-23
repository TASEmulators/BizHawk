using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
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
					if (Controls.OfType<Panel>().Any(p => p.Name == "FlagPanel"))
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
					}

					if (CanSetCpuRegisters)
					{

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
					else
					{
						Controls
							.OfType<Label>()
							.ToList()
							.ForEach(label =>
							{
								if (label.Name == register.Key)
								{
									label.Text = register.Value.Value.ToHexString(register.Value.BitSize / 16);
								}
							});
					}
				}

				_supressChangeEvents = false;
			}
		}

		public bool CanGetCpuRegisters
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

		public bool CanSetCpuRegisters
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
			this.Controls.Clear();
			var canget = CanGetCpuRegisters;
			var canset = CanSetCpuRegisters;

			if (!canget && !canset)
			{
				ParentDebugger.DisableRegisterBox();
				this.Enabled = false;
			}

			var registers = Core.GetCpuFlagsAndRegisters();

			int y = UIHelper.ScaleY(0);

			var maxCharSize = registers.Where(r => r.Value.BitSize != 1).Max(r => r.Key.Length);
			var width = maxCharSize * 5;

			foreach (var register in registers.Where(r => r.Value.BitSize != 1))
			{
				this.Controls.Add(new Label
				{
					Text = register.Key,
					Location = new Point(UIHelper.ScaleX(5), y + UIHelper.ScaleY(2)),
					Size = new Size(UIHelper.ScaleX(width), UIHelper.ScaleY(15))
				});

				if (canset)
				{
					var t = new TextBox
					{
						Name = register.Key,
						Text = register.Value.Value.ToHexString(register.Value.BitSize / 16),
						Width = UIHelper.ScaleX(6 + ((register.Value.BitSize / 4) * 9)),
						Location = new Point(UIHelper.ScaleX(width + 10), y),
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
						Text = register.Value.Value.ToString(),
						Size = new Size(UIHelper.ScaleX(6 + ((register.Value.BitSize / 4) * 9)), UIHelper.ScaleY(15)),
						Location = new Point(UIHelper.ScaleX(width + 10), y)
					});
				}

				y += UIHelper.ScaleY(25);
			}

			var flags = registers.Where(r => r.Value.BitSize == 1);

			if (flags.Any())
			{
				var p = new Panel
				{
					Name = "FlagPanel",
					Location = new Point(UIHelper.ScaleX(5), y),
					BorderStyle = BorderStyle.None,
					Size = new Size(UIHelper.ScaleX(240), UIHelper.ScaleY(23)),
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
						Location = new Point(UIHelper.ScaleX(40), y),
						Dock = DockStyle.Left,
						Size = new Size(UIHelper.ScaleX(23), UIHelper.ScaleY(23)),
						Enabled = canset
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
