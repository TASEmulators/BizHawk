using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class InputConfigBase : Form
	{
		public void CheckDups()
		{
			Dictionary<string,bool> dups = new Dictionary<string,bool>();
			foreach (var stbc in Controls.OfType<SmartTextBoxControl>())
			{
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