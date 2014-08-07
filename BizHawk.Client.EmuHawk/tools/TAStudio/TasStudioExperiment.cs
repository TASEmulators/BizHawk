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
	public partial class TasStudioExperiment : Form, IToolForm
	{
		#region IToolForm Implementation

		public bool UpdateBefore { get { return false; } }

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			InputView.Invalidate();
		}

		public void FastUpdate()
		{
			// TODO: think more about this
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

		}

		public bool AskSave()
		{
			return true;
		}

		#endregion

		public TasStudioExperiment()
		{
			InitializeComponent();
		}

		private void TasStudioExperiment_Load(object sender, EventArgs e)
		{

		}

		private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{

		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}
	}
}
