using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MessageRow : UserControl
	{
		private Action<MessagePosition> _selectedCallback;
		private bool _programmaticallyUpdating = false;

		public MessagePosition MessagePosition { get; private set; } = new MessagePosition();

		public MessageRow()
		{
			InitializeComponent();
		}

		public void Bind(
			string displayName,
			MessagePosition position,
			Action<MessagePosition> selectedCallback)
		{
			MessagePosition = position;
			_selectedCallback = selectedCallback;
			RowRadio.Text = displayName;
			SetText();
		}

		public bool Checked
		{
			get => RowRadio.Checked;
			set
			{
				_programmaticallyUpdating = true;
				RowRadio.Checked = value;
				_programmaticallyUpdating = false;
			}
		}

		public void SetText()
		{
			LocationLabel.Text = MessagePosition.ToCoordinateStr();
		}

		private void RowRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdating && RowRadio.Checked)
			{
				_selectedCallback.Invoke(MessagePosition);
			}
		}
	}
}
