using BizHawk.Client.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	// this is a little messy right now because of remnants of the old config system
	public partial class MessageRow : UserControl
	{
		private MessagePosition _messagePosition = new MessagePosition();

		public MessageRow()
		{
			InitializeComponent();
		}

		public void Bind(string displayName, MessagePosition position)
		{
			_messagePosition = position;
			RowRadio.Text = displayName;
			LocationLabel.Text = position.ToCoordinateStr();
		}
	}
}
