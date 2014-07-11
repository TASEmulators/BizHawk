using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MovieHeaderEditor : Form
	{
		private readonly IMovie Movie;
		public MovieHeaderEditor(IMovie movie)
		{
			Movie = movie;
			InitializeComponent();
		}

		private void MovieHeaderEditor_Load(object sender, EventArgs e)
		{
			AuthorTextBox.Text = Movie.Author;
			EmulatorVersionTextBox.Text = Movie.EmulatorVersion;
			PlatformTextBox.Text = Movie.SystemID;
			CoreTextBox.Text = Movie.Core;
			BoardNameTextBox.Text = Movie.BoardName;
			GameNameTextBox.Text = Movie.GameName;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			Movie.Author = AuthorTextBox.Text;
			if (MakeDefaultCheckbox.Checked)
			{
				Global.Config.DefaultAuthor = AuthorTextBox.Text;
			}

			Movie.EmulatorVersion = EmulatorVersionTextBox.Text;
			Movie.SystemID = PlatformTextBox.Text;
			Movie.Core = CoreTextBox.Text;
			Movie.BoardName = BoardNameTextBox.Text;
			Movie.GameName = GameNameTextBox.Text;

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
