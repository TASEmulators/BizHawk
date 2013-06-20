using System.Collections.Generic;
using System.Windows.Forms;

namespace BizHawk
{
	public class InputConfigBase : Form
	{
		public void CheckDups()
		{
			Dictionary<string,bool> dups = new Dictionary<string,bool>();
			foreach (Control c in Controls)
			{
				SmartTextBoxControl stbc = c as SmartTextBoxControl;
				if (stbc == null) continue;
				if (dups.ContainsKey(stbc.Text))
				{
					MessageBox.Show("DUP!");
					return;
				}
				dups[stbc.Text] = true;
			}
		}
		
	}
}