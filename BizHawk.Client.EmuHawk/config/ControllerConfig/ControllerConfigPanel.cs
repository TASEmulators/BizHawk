using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	// this is a little messy right now because of remnants of the old config system
	public partial class ControllerConfigPanel : UserControl
	{
		// the dictionary that results are saved to
		Dictionary<string, string> RealConfigObject;
		// if nonnull, the list of keys to use.  used to have the config panel operate on a smaller list than the whole dictionary;
		// for instance, to show only a single player
		List<string> RealConfigButtons;

		public List<string> buttons = new List<string>();

		public int InputMarginLeft = UIHelper.ScaleX(0);
		public int LabelPadding = UIHelper.ScaleX(5);

		public int MarginTop = UIHelper.ScaleY(0);
		public int Spacing = UIHelper.ScaleY(24);
		public int InputSize = UIHelper.ScaleX(170);
		public int ColumnWidth = UIHelper.ScaleX(280);
		public int LabelWidth = UIHelper.ScaleX(60);

		public ToolTip Tooltip;

		protected List<InputCompositeWidget> Inputs = new List<InputCompositeWidget>();
		protected List<Label> Labels = new List<Label>();

		private Size _panelSize = new Size(0, 0);

		public ControllerConfigPanel()
		{
			InitializeComponent();
		}

		private void ControllerConfigPanel_Load(object sender, EventArgs e)
		{
			
		}

		public void ClearAll()
		{
			Inputs.ForEach(x => x.Clear());
		}

		/// <summary>
		/// save to config
		/// </summary>
		/// <param name="SaveConfigObject">if non-null, save to possibly different config object than originally initialized from</param>
		public void Save(Dictionary<string, string>SaveConfigObject = null)
		{
			var saveto = SaveConfigObject ?? RealConfigObject;
			for (int button = 0; button < buttons.Count; button++)
				saveto[buttons[button]] = Inputs[button].Bindings;
		}

		public bool Autotab = false;
		public void LoadSettings(Dictionary<string, string> configobj, bool autotab, List<string> configbuttons = null, int? width = null, int? height = null)
		{
			Autotab = autotab;
			if (width.HasValue && height.HasValue)
			{
				_panelSize = new Size(width.Value, height.Value);
			}
			else
			{
				_panelSize = Size;
			}
			
			RealConfigObject = configobj;
			RealConfigButtons = configbuttons;
			SetButtonList();
			Startup();
			SetWidgetStrings();
		}

		protected void SetButtonList()
		{
			buttons.Clear();
			IEnumerable<string> bl = RealConfigButtons ?? (IEnumerable<string>)RealConfigObject.Keys;
			foreach (string s in bl)
				buttons.Add(s);
		}

		protected void SetWidgetStrings()
		{
			for (int button = 0; button < buttons.Count; button++)
			{
				string s;
				if (!RealConfigObject.TryGetValue(buttons[button], out s))
					s = "";
				Inputs[button].Bindings = s;
			}
		}

		protected void Startup()
		{
			int x = InputMarginLeft;
			int y = MarginTop - Spacing;
			for (int i = 0; i < buttons.Count; i++)
			{
				y += Spacing;
				if (y > (_panelSize.Height - UIHelper.ScaleY(62)))
				{
					y = MarginTop;
					x += ColumnWidth;
				}

				InputCompositeWidget iw = new InputCompositeWidget
				{
					Location = new Point(x, y),
					Size = new Size(InputSize, UIHelper.ScaleY(23)),
					TabIndex = i,
					AutoTab = this.Autotab
				};

				iw.SetupTooltip(Tooltip, null);

				iw.BringToFront();
				Controls.Add(iw);
				Inputs.Add(iw);
				Label label = new Label
					{
						Location = new Point(x + InputSize + LabelPadding, y + UIHelper.ScaleY(3)),
						Size = new Size(UIHelper.ScaleX(100), UIHelper.ScaleY(15)),
						Text = buttons[i].Replace('_', ' ').Trim(),
					};

				//Tooltip.SetToolTip(label, null); //??? not supported yet

				Controls.Add(label);
				Labels.Add(label);
			}
		}

		public void SetAutoTab(bool value)
		{
			Inputs.ForEach(x => x.AutoTab = value);
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ClearAll();
		}
	}
}
