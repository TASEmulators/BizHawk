using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class ControllerConfigPanel : UserControl
	{
		object ControllerConfigObject; //Object that values will be saved to (In Config.cs)

		public List<string> buttons = new List<string>();

		public int InputMarginLeft = 0;
		public int LabelPadding = 10;

		public int MarginTop = 0;
		public int Spacing = 30;
		public int InputSize = 200;

		protected List<InputWidget> Inputs = new List<InputWidget>();
		protected List<Label> Labels = new List<Label>();

		public ControllerConfigPanel()
		{
			InitializeComponent();
		}

		private void ControllerConfigPanel_Load(object sender, EventArgs e)
		{
			
		}

		public void Save()
		{
			for (int button = 0; button < buttons.Count; button++)
			{
				FieldInfo buttonF = ControllerConfigObject.GetType().GetField(buttons[button]);
				buttonF.SetValue(ControllerConfigObject, Inputs[button].Text);
			}
		}

		public void LoadSettings(object configobj)
		{
			ControllerConfigObject = configobj;

			SetButtonList();
			Startup();
			SetWidgetStrings();
		}

		private void SetButtonList()
		{
			buttons.Clear();
			MemberInfo[] members = ControllerConfigObject.GetType().GetMembers();

			foreach (MemberInfo member in members)
			{
				Type type = member.GetType();

				if (member.MemberType.ToString() == "Field" && member.ToString().Contains("System.String"))
				{
					buttons.Add(member.Name);
				}
			}
		}

		private void SetWidgetStrings()
		{
			for (int button = 0; button < buttons.Count; button++)
			{
				FieldInfo buttonF = ControllerConfigObject.GetType().GetField(buttons[button]);
				object field = ControllerConfigObject.GetType().GetField(buttons[button]).GetValue(ControllerConfigObject);
				Inputs[button].SetBindings(field.ToString());
			}
		}

		private void Startup()
		{
			for (int i = 0; i < buttons.Count; i++)
			{
				InputWidget iw = new InputWidget();
				iw.Location = new Point(InputMarginLeft, MarginTop + (i * Spacing));
				iw.Size = new Size(InputSize, 23);
				iw.TabIndex = i;
				Controls.Add(iw);
				Inputs.Add(iw);

				Label l = new Label();
				l.Location = new Point(InputMarginLeft + InputSize + LabelPadding, MarginTop + (i * Spacing) + 3);
				l.Text = buttons[i];
				Controls.Add(l);
				Labels.Add(l);
			}
		}
	}
}
