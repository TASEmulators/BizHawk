using System.Diagnostics;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Logs into retroachievements.org for RetroAchievements
	/// </summary>
	public partial class RCheevosLoginForm : Form
	{
		public RCheevosLoginForm(Func<string, string, bool> loginCallback)
		{
			InitializeComponent();
			_loginCallback = loginCallback;
		}

		private readonly Func<string, string, bool> _loginCallback;

		private void btnLogin_Click(object sender, EventArgs e)
		{
			var res = _loginCallback(txtUsername.Text, txtPassword.Text);
			if (res)
			{
				MessageBox.Show("Login successful");
				Close();
			}
			else
			{
				MessageBox.Show("Login failed");
			}
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start("https://retroachievements.org/createaccount.php");
		}
	}
}

