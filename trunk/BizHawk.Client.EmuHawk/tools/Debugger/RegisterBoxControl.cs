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
			foreach (var register in registers)
			{
				this.Controls.Add(new Label
				{
					Text = register.Key + (canset ? ": " : ""),
					Location = new Point(5, y + 2),
					Width = 50
				});

				if (canset)
				{
					if (register.Key.Contains("Flag")) // TODO: this depends on naming conventions!
					{
						var c = new CheckBox
						{
							Name = register.Key,
							Text = "",
							Checked = register.Value == 1 ? true : false,
							Location = new Point(55, y)
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

						this.Controls.Add(c);
					}
					else
					{
						var t = new TextBox
						{
							Name = register.Key,
							Text = register.Value.ToString(),
							Width = 75,
							Location = new Point(55, y),
						};

						t.TextChanged += (o, e) =>
						{
							Core.SetCpuRegister(t.Name, int.Parse(t.Text));
						};

						this.Controls.Add(t);
					}
				}
				else
				{
					this.Controls.Add(new Label
					{
						Name = register.Key,
						Text = register.Value.ToString(),
						Width = 75,
						Location = new Point(55, y)
					});
				}

				y += 25;
			}
		}
	}
}
