using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Logs into retroachievements.org for RetroAchievements
	/// </summary>
	public partial class RCheevosLoginForm : Form
	{
		public RCheevosLoginForm(Func<string, string, Task<bool>> loginCallback)
		{
			InitializeComponent();
			_loginCallback = loginCallback;
		}

		private readonly Func<string, string, Task<bool>> _loginCallback;

		private async void btnLogin_Click(object sender, EventArgs e)
		{
			var res = await _loginCallback(txtUsername.Text, txtPassword.Text);
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

