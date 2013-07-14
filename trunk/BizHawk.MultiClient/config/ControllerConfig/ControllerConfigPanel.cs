using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	// this is a little messy right now because of remnants of the old config system

	public partial class ControllerConfigPanel : UserControl
	{
		iControllerConfigObject ControllerConfigObject; //Object that values will be saved to (In Config.cs)

		public List<string> buttons = new List<string>();

		public int InputMarginLeft = 0;
		public int LabelPadding = 10;

		public int MarginTop = 0;
		public int Spacing = 30;
		public int InputSize = 200;
		public int ColumnWidth = 220;
		public int LabelWidth = 100;

		protected List<InputWidget> Inputs = new List<InputWidget>();
		protected List<Label> Labels = new List<Label>();

		public ControllerConfigPanel()
		{
			InitializeComponent();
		}

		private void ControllerConfigPanel_Load(object sender, EventArgs e)
		{
			
		}

		private void DoConflicts()
		{
			List<KeyValuePair<int, string>> BindingList = new List<KeyValuePair<int, string>>();
			HashSet<string> uniqueBindings = new HashSet<string>();

			for (int i = 0; i < Inputs.Count; i++)
			{
				if (!String.IsNullOrWhiteSpace(Inputs[i].Text))
				{
					string[] bindings = Inputs[i].Text.Split(',');
					foreach (string binding in bindings)
					{
						BindingList.Add(new KeyValuePair<int, string>(i, binding));
						uniqueBindings.Add(binding);
					}
				}
			}

			foreach (string binding in uniqueBindings)
			{
				List<KeyValuePair<int, string>> kvps = BindingList.Where(x => x.Value == binding).ToList();
				if (kvps.Count > 1)
				{
					foreach(KeyValuePair<int, string> kvp in kvps)
					{
						Inputs[kvp.Key].Conflicted = true;
					}
				}
			}
		}

		public void ClearAll()
		{
			foreach (InputWidget i in Inputs)
			{
				i.Clear();
			}
		}

		virtual public void Save()
		{
			for (int button = 0; button < buttons.Count; button++)
			{
				FieldInfo buttonF = ControllerConfigObject.GetType().GetField(buttons[button]);
				buttonF.SetValue(ControllerConfigObject, Inputs[button].Text);
			}
		}

		virtual public void LoadSettings(iControllerConfigObject configobj)
		{
			ControllerConfigObject = configobj;

			SetButtonList();
			Startup();
			SetWidgetStrings();
		}

		virtual protected void SetButtonList()
		{
			buttons.Clear();
			MemberInfo[] members = ControllerConfigObject.GetType().GetMembers();

			foreach (MemberInfo member in members)
			{
				if (member.MemberType.ToString() == "Field" && member.ToString().Contains("System.String"))
				{
					buttons.Add(member.Name);
				}
			}
		}

		virtual protected void SetWidgetStrings()
		{
			for (int button = 0; button < buttons.Count; button++)
			{
				object field = ControllerConfigObject.GetType().GetField(buttons[button]).GetValue(ControllerConfigObject);

				if (field == null)
				{
					Inputs[button].SetBindings("");
				}
				else
				{
					Inputs[button].SetBindings(field.ToString());
				}
			}
		}

		protected void Startup()
		{
			int x = InputMarginLeft;
			int y = MarginTop - Spacing;
			for (int i = 0; i < buttons.Count; i++)
			{
				y += Spacing;
				if (y > (Size.Height - 23))
				{
					y = MarginTop;
					x += ColumnWidth;
				}
				InputWidget iw = new InputWidget {Location = new Point(x, y), Size = new Size(InputSize, 23), TabIndex = i};
				iw.BringToFront();
				iw.Enter += InputWidget_Enter;
				iw.Leave += InputWidget_Leave;
				Controls.Add(iw);
				Inputs.Add(iw);
				Label l = new Label
					{
						Location = new Point(x + InputSize + LabelPadding, y + 3),
						Text = buttons[i].Replace('_', ' ').Trim(),
						Width = LabelWidth
					};
				Controls.Add(l);
				Labels.Add(l);
			}
		}

		private void InputWidget_Enter(object sender, EventArgs e)
		{
			DoConflicts();
		}

		private void InputWidget_Leave(object sender, EventArgs e)
		{
			DoConflicts();
		}

		public void SetAutoTab(bool value)
		{
			foreach (InputWidget i in Inputs)
			{
				i.AutoTab = value;
			}
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ClearAll();
		}

		virtual protected void restoreDefaultsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ControllerConfigObject.SetDefaults();
			SetWidgetStrings();
		}
	}
}
