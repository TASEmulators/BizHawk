using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	class ToolHelpers
	{
		public static ToolStripMenuItem[] GenerateMemoryDomainMenuItems(Action<int> SetCallback, string SelectedDomain = "")
		{
			var items = new List<ToolStripMenuItem>();

			if (Global.Emulator.MemoryDomains.Any())
			{
				int counter = 0;
				foreach (var domain in Global.Emulator.MemoryDomains)
				{
					string temp = domain.ToString();
					var item = new ToolStripMenuItem { Text = temp };

					int index = counter;
					item.Click += (o, ev) => SetCallback(index);

					if (temp == SelectedDomain)
					{
						item.Checked = true;
					}

					items.Add(item);
					counter++;
				}
			}

			return items.ToArray();
		}
	}
}
