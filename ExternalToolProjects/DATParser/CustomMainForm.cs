using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;

namespace BizHawk.DATTool
{
	[ExternalTool("DATTool", Description = "External DAT Parsing Tools")]
	public class CustomMainForm : ToolFormBase, IExternalToolForm
	{
		protected override string WindowTitleStatic => "DATTools";

		public CustomMainForm()
		{
			static Label CreateArgsLabel(string labelText) => new Label { Anchor = AnchorStyles.None, AutoSize = true, Text = labelText };
			static Button CreateLaunchButton() => new Button { Size = new Size(32, 24), Text = "=>", UseVisualStyleBackColor = true };

			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(320, 200);
			Name = "CustomMainForm";

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
							CreateArgsLabel("Parse External DAT Files (NoIntro / TOSEC)")
						}
					}					
				},
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.TopDown
			});
			ResumeLayout();
		}

#if false
		/// <remarks>This was just sitting in <c>BizHawk.Client.DBMan/Program.cs</c>.</remarks>
		public static string GetExeDirectoryAbsolute()
		{
			var uri = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
			string module = uri.LocalPath + System.Web.HttpUtility.UrlDecode(uri.Fragment);
			return Path.GetDirectoryName(module);
		}
#endif
	}
}
