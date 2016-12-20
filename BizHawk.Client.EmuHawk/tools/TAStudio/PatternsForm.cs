using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PatternsForm : Form
	{
		private TAStudio tastudio;

		public PatternsForm(TAStudio owner)
		{
			InitializeComponent();
			tastudio = owner;

			foreach (var button in Global.MovieSession.MovieControllerAdapter.Definition.BoolButtons)
				ButtonBox.Items.Add(button);
			foreach (var button in Global.MovieSession.MovieControllerAdapter.Definition.FloatControls)
				ButtonBox.Items.Add(button);
			ButtonBox.Items.Add("Default bool Auto-Fire");
			ButtonBox.Items.Add("Default float Auto-Fire");
		}

		private void PatternsForm_Load(object sender, EventArgs e)
		{
			ButtonBox.SelectedIndex = 0;
		}

		List<int> counts = new List<int>();
		List<string> values = new List<string>();
		int loopAt;
		string selectedButton { get { return ButtonBox.Text; } }
		bool isBool { get { return selectedButton == "Default bool Auto-Fire" || Global.MovieSession.MovieControllerAdapter.Definition.BoolButtons.Contains(selectedButton); } }

		private void ButtonBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			GetPattern();
			UpdateDisplay();

			if (isBool)
			{
				OnOffBox.Visible = true;
				ValueNum.Visible = false;
			}
			else
			{
				ValueNum.Visible = true;
				OnOffBox.Visible = false;
			}
			CountNum.Value = counts[0];
		}

		private void PatternList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_updating)
				UpdateDisplay();
		}

		private void InsertButton_Click(object sender, EventArgs e)
		{
			counts.Insert(PatternList.SelectedIndex, 1);
			string defaultStr = "false";
			if (!isBool)
				defaultStr = "0";
			values.Insert(PatternList.SelectedIndex, defaultStr);

			UpdatePattern();
			UpdateDisplay();
		}

		private void DeleteButton_Click(object sender, EventArgs e)
		{
			counts.RemoveAt(PatternList.SelectedIndex);
			values.RemoveAt(PatternList.SelectedIndex);
			UpdatePattern();
			UpdateDisplay();
		}

		private void LagBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdatePattern();
		}

		private void ValueNum_ValueChanged(object sender, EventArgs e)
		{
			if (_updating || PatternList.SelectedIndex == -1 || PatternList.SelectedIndex >= counts.Count)
				return;

			values[PatternList.SelectedIndex] = ValueNum.Value.ToString();
			UpdatePattern();
			UpdateDisplay();
		}
		private void OnOffBox_CheckedChanged(object sender, EventArgs e)
		{
			if (_updating || PatternList.SelectedIndex == -1 || PatternList.SelectedIndex >= counts.Count)
				return;

			values[PatternList.SelectedIndex] = OnOffBox.Checked.ToString();
			UpdatePattern();
			UpdateDisplay();
		}
		private void CountNum_ValueChanged(object sender, EventArgs e)
		{
			if (_updating || PatternList.SelectedIndex == -1 || PatternList.SelectedIndex > counts.Count)
				return;

			if (PatternList.SelectedIndex == counts.Count)
				loopAt = (int)CountNum.Value;
			else
				counts[PatternList.SelectedIndex] = (int)CountNum.Value;
			UpdatePattern();
			UpdateDisplay();
		}

		private bool _updating = false;
		private void UpdateDisplay()
		{
			_updating = true;
			PatternList.SuspendLayout();

			int oldIndex = PatternList.SelectedIndex;
			if (oldIndex == -1)
				oldIndex = 0;

			PatternList.Items.Clear();
			int index = 0;
			for (int i = 0; i < counts.Count; i++)
			{
				string str = index.ToString() + ": ";
				if (isBool)
					str += values[i][0] == 'T' ? "On" : "Off";
				else
					str += values[i].ToString();

				PatternList.Items.Add(str + ("\t(x" + counts[i] + ")"));
				index += counts[i];
			}
			PatternList.Items.Add("Loop to: " + loopAt);

			if (oldIndex >= PatternList.Items.Count)
				oldIndex = PatternList.Items.Count - 1;
			PatternList.SelectedIndex = oldIndex;

			if (PatternList.SelectedIndex != -1 && PatternList.SelectedIndex < values.Count)
			{
				index = Global.MovieSession.MovieControllerAdapter.Definition.BoolButtons.IndexOf(selectedButton);
				if (selectedButton == "Default bool Auto-Fire")
					index = tastudio.BoolPatterns.Length + 1;
				if (index != -1)
				{
					LagBox.Checked = tastudio.BoolPatterns[index].SkipsLag;
					OnOffBox.Checked = values[PatternList.SelectedIndex][0] == 'T';
					CountNum.Value = (decimal)counts[PatternList.SelectedIndex];
				}
				else
				{
					if (selectedButton == "Default float Auto-Fire")
						index = tastudio.FloatPatterns.Length + 1;
					else
						index = Global.MovieSession.MovieControllerAdapter.Definition.FloatControls.IndexOf(selectedButton);

					LagBox.Checked = tastudio.FloatPatterns[index].SkipsLag;
					ValueNum.Value = Convert.ToDecimal(values[PatternList.SelectedIndex]);
					CountNum.Value = (decimal)counts[PatternList.SelectedIndex];
				}
			}
			else if (PatternList.SelectedIndex == values.Count)
				CountNum.Value = (decimal)loopAt;

			PatternList.ResumeLayout();
			_updating = false;
		}

		private void UpdatePattern()
		{
			int index = Global.MovieSession.MovieControllerAdapter.Definition.BoolButtons.IndexOf(selectedButton);
			if (selectedButton == "Default bool Auto-Fire")
				index = tastudio.BoolPatterns.Length + 1;
			if (index != -1)
			{
				List<bool> p = new List<bool>();
				for (int i = 0; i < counts.Count; i++)
				{
					for (int c = 0; c < counts[i]; c++)
						p.Add(Convert.ToBoolean(values[i]));
				}
				tastudio.BoolPatterns[index] = new AutoPatternBool(p.ToArray(), LagBox.Checked, 0, loopAt);
			}
			else
			{
				if (selectedButton == "Default float Auto-Fire")
					index = tastudio.FloatPatterns.Length + 1;
				else
					index = Global.MovieSession.MovieControllerAdapter.Definition.FloatControls.IndexOf(selectedButton);
				List<float> p = new List<float>();
				for (int i = 0; i < counts.Count; i++)
				{
					for (int c = 0; c < counts[i]; c++)
						p.Add(Convert.ToSingle(values[i]));
				}
				tastudio.FloatPatterns[index] = new AutoPatternFloat(p.ToArray(), LagBox.Checked, 0, loopAt);
			}

			tastudio.UpdateAutoFire(selectedButton, null);
		}

		private void GetPattern()
		{
			int index = Global.MovieSession.MovieControllerAdapter.Definition.BoolButtons.IndexOf(selectedButton);
			if (selectedButton == "Default bool Auto-Fire")
				index = tastudio.BoolPatterns.Length + 1;
			if (index != -1)
			{
				bool[] p = tastudio.BoolPatterns[index].Pattern;
				bool lastValue = p[0];
				counts.Clear();
				values.Clear();
				counts.Add(1);
				values.Add(lastValue.ToString());
				for (int i = 1; i < p.Length; i++)
				{
					if (p[i] == lastValue)
						counts[counts.Count - 1]++;
					else
					{
						counts.Add(1);
						values.Add(p[i].ToString());
						lastValue = p[i];
					}
				}
				loopAt = tastudio.BoolPatterns[index].Loop;
			}
			else
			{
				if (selectedButton == "Default float Auto-Fire")
					index = tastudio.FloatPatterns.Length + 1;
				else
					index = Global.MovieSession.MovieControllerAdapter.Definition.FloatControls.IndexOf(selectedButton);
				float[] p = tastudio.FloatPatterns[index].Pattern;
				float lastValue = p[0];
				counts.Clear();
				values.Clear();
				counts.Add(1);
				values.Add(lastValue.ToString());
				for (int i = 1; i < p.Length; i++)
				{
					if (p[i] == lastValue)
						counts[counts.Count - 1]++;
					else
					{
						counts.Add(1);
						values.Add(p[i].ToString());
						lastValue = p[i];
					}
				}
				loopAt = tastudio.FloatPatterns[index].Loop;
			}
		}



	}
}
