using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MovieHeaderEditor : Form
	{
		private readonly IMovie _movie;

		public MovieHeaderEditor(IMovie movie)
		{
			_movie = movie;
			InitializeComponent();
		}

		private void MovieHeaderEditor_Load(object sender, EventArgs e)
		{
			AuthorTextBox.Text = _movie.Author;
			EmulatorVersionTextBox.Text = _movie.EmulatorVersion;
			CoreTextBox.Text = _movie.Core;
			BoardNameTextBox.Text = _movie.BoardName;
			GameNameTextBox.Text = _movie.GameName;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_movie.Author = AuthorTextBox.Text;
			if (MakeDefaultCheckbox.Checked)
			{
				Global.Config.DefaultAuthor = AuthorTextBox.Text;
			}

			_movie.EmulatorVersion = EmulatorVersionTextBox.Text;
			_movie.Core = CoreTextBox.Text;
			_movie.BoardName = BoardNameTextBox.Text;
			_movie.GameName = GameNameTextBox.Text;

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void DefaultAuthorButton_Click(object sender, EventArgs e)
		{
			AuthorTextBox.Text = Global.Config.DefaultAuthor;
		}
	}
}
