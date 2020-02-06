using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;
using BizHawk.Common;

using Community.CsharpSqlite.SQLiteClient;

namespace BizHawk.DBManTool
{
	[ExternalTool("DBMan", "DB Manager")]
	public class CustomMainForm : Form, IExternalToolForm
	{
		public CustomMainForm()
		{
			static Label CreateArgsLabel(string labelText) => new Label { Anchor = AnchorStyles.None, AutoSize = true, Text = labelText };
			static TextBox CreateArgsTextBox() => new TextBox { Size = new Size(240, 19) };
			static Button CreateLaunchButton() => new Button { Size = new Size(32, 24), Text = "=>", UseVisualStyleBackColor = true };

			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(480, 200);
			Name = "CustomMainForm";

			var btnDiscHash = CreateLaunchButton();
			var tbDiscHashArgs = CreateArgsTextBox();
			btnDiscHash.Click += (sender, e) => new DiscHash().Run(tbDiscHashArgs.Text.Split(' '));

			var btnPSXDB = CreateLaunchButton();
			var tbPSXDBArgs = CreateArgsTextBox();
			btnPSXDB.Click += (sender, e) => new PsxDBJob().Run(tbPSXDBArgs.Text.Split(' '));

			var btnDBMan = CreateLaunchButton();
			btnDBMan.Click += (sender, e) =>
			{
				try
				{
					DB.Con = new SqliteConnection { ConnectionString = @"Version=3,uri=file://gamedb/game.db" };
					DB.Con.Open();
					new DBMan().Show(this);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}
				finally
				{
					DB.Con?.Dispose();
				}
			};

			var btnDiscCMP = CreateLaunchButton();
			var tbDiscCMPArgs = CreateArgsTextBox();
#if false
			btnDiscCMP.Click += (sender, e) => new DiscCmp().Run(tbDiscCMPArgs.Text.Split(' '));
#endif

			var btnDATConv = CreateLaunchButton();
			btnDATConv.Click += (sender, e) =>
			{
				try
				{
					new DATConverter().Show(this);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}
			};

			SuspendLayout();
			Controls.Add(new FlowLayoutPanel {
				AutoSize = true,
				Controls = {
					new FlowLayoutPanel {
						AutoSize = true,
						Controls = {
							btnDATConv,
							CreateArgsLabel("DAT Converter")
						}
					},
					new FlowLayoutPanel {
						AutoSize = true,
						Controls = {
							btnDiscHash,
							CreateArgsLabel("DBMan.exe --dischash"),
							tbDiscHashArgs
						}
					},
					new FlowLayoutPanel {
						AutoSize = true,
						Controls = {
							btnPSXDB,
							CreateArgsLabel("DBMan.exe --psxdb"),
							tbPSXDBArgs
						}
					},
					new FlowLayoutPanel {
						AutoSize = true,
						Controls = {
							btnDBMan,
							CreateArgsLabel("DBMan.exe --dbman")
						},
						Enabled = !OSTailoredCode.IsUnixHost
					},
					new FlowLayoutPanel {
						AutoSize = true,
						Controls = {
							btnDiscCMP,
							CreateArgsLabel("DBMan.exe --disccmp"),
							tbDiscCMPArgs
						},
						Enabled = false
					}
					
				},
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.TopDown
			});
			ResumeLayout();
		}

		public override string Text => "DBMan";

		public bool UpdateBefore => false;

		public bool AskSaveChanges() => true;

		public void FastUpdate() {}

#if false
		/// <remarks>This was just sitting in <c>BizHawk.Client.DBMan/Program.cs</c>.</remarks>
		public static string GetExeDirectoryAbsolute()
		{
			var uri = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
			string module = uri.LocalPath + System.Web.HttpUtility.UrlDecode(uri.Fragment);
			return Path.GetDirectoryName(module);
		}
#endif

		public void NewUpdate(ToolFormUpdateType type) {}

		public void Restart() {}

		public void UpdateValues() {}
	}
}
