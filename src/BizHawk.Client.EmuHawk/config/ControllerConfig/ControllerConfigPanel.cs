using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	// this is a little messy right now because of remnants of the old config system
	public partial class ControllerConfigPanel : UserControl
	{
		// the dictionary that results are saved to
		private Dictionary<string, string> _realConfigObject;

		// if nonnull, the list of keys to use.  used to have the config panel operate on a smaller list than the whole dictionary;
		// for instance, to show only a single player
		private List<string> _realConfigButtons;

		private readonly List<string> _buttons = new List<string>();

		private readonly int _inputMarginLeft = UIHelper.ScaleX(0);
		private readonly int _labelPadding = UIHelper.ScaleX(5);
		private readonly int _marginTop = UIHelper.ScaleY(0);
		private readonly int _ySpacing = UIHelper.ScaleY(24);
		private readonly int _inputSize = UIHelper.ScaleX(170);
		private readonly int _charWidth = UIHelper.ScaleX(9);

		public ToolTip Tooltip { get; set; }

		private readonly List<InputCompositeWidget> _inputs = new List<InputCompositeWidget>();

		private Size _panelSize = new Size(0, 0);

		private bool _autoTab;

		private readonly IReadOnlyList<string> _effectiveModList;

		public ControllerConfigPanel(IReadOnlyList<string> effectiveModList)
		{
			_effectiveModList = effectiveModList;
			InitializeComponent();
		}

		private void ClearAll()
		{
			_inputs.ForEach(x => x.Clear());
		}

		/// <summary>
		/// save to config
		/// </summary>
		/// <param name="saveConfigObject">if non-null, save to possibly different config object than originally initialized from</param>
		public void Save(Dictionary<string, string> saveConfigObject = null)
		{
			var saveTo = saveConfigObject ?? _realConfigObject;
			for (int button = 0; button < _buttons.Count; button++)
			{
				var bindings = _inputs[button].Bindings;
				if (!string.IsNullOrWhiteSpace(bindings))
				{
					saveTo[_buttons[button]] = bindings;
				}
				else
				{
					saveTo.Remove(_buttons[button]);
				}
			}
		}

		public void LoadSettings(Dictionary<string, string> config, bool autoTab, List<string> buttons = null, int? width = null, int? height = null)
		{
			_autoTab = autoTab;
			if (width.HasValue && height.HasValue)
			{
				_panelSize = new Size(width.Value, height.Value);
			}
			else
			{
				_panelSize = Size;
			}
			
			_realConfigObject = config;
			_realConfigButtons = buttons;
			SetButtonList();
			Startup();
			SetWidgetStrings();
		}

		private void SetButtonList()
		{
			_buttons.Clear();
			IEnumerable<string> bl = _realConfigButtons ?? (IEnumerable<string>)_realConfigObject.Keys;
			foreach (string s in bl)
			{
				_buttons.Add(s);
			}
		}

		private void SetWidgetStrings()
		{
			for (int button = 0; button < _buttons.Count; button++)
			{
				if (!_realConfigObject.TryGetValue(_buttons[button], out var s))
				{
					s = "";
				}

				_inputs[button].Bindings = s;
			}
		}

		private void Startup()
		{
			var labelWidth = _buttons.Max(b => b.Length) * _charWidth;
			var columnWidth = _inputSize + (_labelPadding * 2) + labelWidth;
			int x = _inputMarginLeft;
			int y = _marginTop - _ySpacing;
			for (int i = 0; i < _buttons.Count; i++)
			{
				y += _ySpacing;
				if (y > (_panelSize.Height - UIHelper.ScaleY(62)))
				{
					y = _marginTop;
					x += columnWidth;
				}

				var iw = new InputCompositeWidget(_effectiveModList)
				{
					Location = new Point(x, y),
					Size = new Size(_inputSize, UIHelper.ScaleY(23)),
					TabIndex = i,
					AutoTab = _autoTab
				};

				iw.SetupTooltip(Tooltip, null);

				iw.BringToFront();
				Controls.Add(iw);
				_inputs.Add(iw);
				var label = new Label
				{
					Location = new Point(x + _inputSize + _labelPadding, y + UIHelper.ScaleY(3)),
					Size = new Size(labelWidth, UIHelper.ScaleY(15)),
					Text = _buttons[i].Replace('_', ' ').Trim()
				};

				Tooltip.SetToolTip(label, label.Text);

				Controls.Add(label);
			}
		}

		public void SetAutoTab(bool value)
		{
			_inputs.ForEach(x => x.AutoTab = value);
		}

		private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ClearAll();
		}
	}
}
