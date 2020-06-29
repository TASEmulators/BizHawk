using BizHawk.Client.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	// this is a little messy right now because of remnants of the old config system
	public partial class MessageEdit : UserControl
	{
		private MessagePosition _messagePosition;

		public MessageEdit()
		{
			InitializeComponent();
		}

		public void Bind(MessagePosition messagePosition)
		{
			_messagePosition = messagePosition;
		}

		private void TL_CheckedChanged(object sender, EventArgs e)
		{

		}

		private void BL_CheckedChanged(object sender, EventArgs e)
		{

		}

		private void TR_CheckedChanged(object sender, EventArgs e)
		{

		}

		private void BR_CheckedChanged(object sender, EventArgs e)
		{

		}
	}
}
