using BizHawk.Client.Common;
using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	// this is a little messy right now because of remnants of the old config system
	public partial class MessageRow : UserControl
	{
		private Action<MessagePosition> _selectedCallback;

		public MessagePosition _messagePosition { get; private set; } = new MessagePosition();

		public MessageRow()
		{
			InitializeComponent();
		}

		public void Bind(
			string displayName,
			MessagePosition position,
			Action<MessagePosition> selectedCallback)
		{
			_messagePosition = position;
			_selectedCallback = selectedCallback;
			RowRadio.Text = displayName;
			SetText();
		}

		public new void Refresh()
		{
			SetText();
			base.Refresh();
		}

		private void SetText()
		{
			LocationLabel.Text = _messagePosition.ToCoordinateStr();
		}

		private void RowRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (RowRadio.Checked)
			{
				_selectedCallback.Invoke(_messagePosition);
			}
		}
	}
}
