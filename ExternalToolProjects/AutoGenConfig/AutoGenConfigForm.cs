using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;

using static BizHawk.Experiment.AutoGenConfig.ConfigEditorUIGenerators;

namespace BizHawk.Experiment.AutoGenConfig
{
	[ExternalTool("AutoGenConfig")]
	public class AutoGenConfigForm : Form, IExternalToolForm
	{
		private static readonly IList<(PropertyInfo, IConfigPropEditorUIGen<Control>)> CachedControlGenerators;

		public static ComparisonColors ComparisonColors = new ComparisonColors
		{
			Changed = Color.FromArgb(unchecked((int) 0xFF9F3F00)),
			ChangedInvalid = Color.DarkRed,
			ChangedUnset = Color.FromArgb(unchecked((int) 0xFF9F1F5F)),
			Unchanged = Color.FromArgb(unchecked((int) 0xFF00003F)),
			UnchangedDefault = Color.Black
		};

		public static readonly IDictionary<string, object?> DefaultValues;

		static AutoGenConfigForm()
		{
			CachedControlGenerators = new List<(PropertyInfo, IConfigPropEditorUIGen<Control>)>();
			DefaultValues = new Dictionary<string, object?>();
			foreach (var pi in typeof(Config).GetProperties())
			{
				CachedControlGenerators.Add((pi, FallbackGenerators.TryGetValue(pi.PropertyType, out var gen) ? gen : FinalFallbackGenerator));
				DefaultValues[pi.Name] = pi.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault()
					?.Let(it => ((DefaultValueAttribute) it).Value)
					?? TrueGenericDefault(pi.PropertyType);
			}
		}

		/// <returns>value types: default(T); ref types: calls default (no-arg) ctor if it exists, else null</returns>
		private static object? TrueGenericDefault(Type t)
		{
			try
			{
				return Activator.CreateInstance(t);
			}
			catch
			{
				return null;
			}
		}

		public readonly IDictionary<string, object?> BaselineValues = new Dictionary<string, object?>();

		[RequiredApi]
		private IEmu? EmuHawkAPI { get; set; }

		public override string Text => "AutoGenConfig";

		public bool UpdateBefore => false;

		public AutoGenConfigForm()
		{
			ClientSize = new Size(640, 720);
			SuspendLayout();
			Controls.Add(new FlowLayoutPanel {
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				BorderStyle = BorderStyle.FixedSingle,
				Controls = {
					new Label { AutoSize = true, Text = "Legend:" },
					new Label { AutoSize = true, ForeColor = ComparisonColors.UnchangedDefault, Text = "default, unchanged" },
					new Label { AutoSize = true, ForeColor = ComparisonColors.Unchanged, Text = "custom, unchanged" },
					new Label { AutoSize = true, ForeColor = ComparisonColors.ChangedUnset, Text = "custom => default" },
					new Label { AutoSize = true, ForeColor = ComparisonColors.ChangedInvalid, Text = "invalid" },
					new Label { AutoSize = true, ForeColor = ComparisonColors.Changed, Text = "custom A => custom B" }
				},
				Location = new Point(4, 4),
				Padding = new Padding(0, 4, 0, 0),
				Size = new Size(ClientSize.Width - 8, 24),
				WrapContents = false
			});
			FlowLayoutPanel flpMain;
			Controls.Add(flpMain = new FlowLayoutPanel {
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				AutoScroll = true,
				FlowDirection = FlowDirection.TopDown,
				Location = new Point(4, 32),
				Size = new Size(ClientSize.Width - 8, ClientSize.Height - 64),
				WrapContents = false
			});
			Controls.Add(new FlowLayoutPanel {
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				AutoScroll = true,
				AutoSize = true,
				Controls = {
					new Button {
						Size = new Size(128, 24),
						Text = "Discard Changes"
					}.Also(it => it.Click += (clickEventSender, clickEventArgs) => Close()),
					new Button {
						Size = new Size(128, 24),
						Text = "Review and Save..."
					}.Also(it => it.Click += (clickEventSender, clickEventArgs) => Close())
				},
				FlowDirection = FlowDirection.RightToLeft,
				Location = new Point(ClientSize.Width - 201, ClientSize.Height - 31),
				WrapContents = false
			});
			Load += (loadEventSender, loadEventArgs) =>
			{
				var config = (EmuHawkAPI as EmuApi ?? throw new Exception("required API wasn't fulfilled")).ForbiddenConfigReference;
				flpMain.Controls.AddRange(CachedControlGenerators.Select(it => it.Item2.GenerateControl(it.Item1, config, BaselineValues)).ToArray());
			};
			ResumeLayout();
		}

		public bool AskSaveChanges() => true;

		public void FastUpdate() {}

		public void NewUpdate(ToolFormUpdateType type) {}

		public void Restart() {}

		public void UpdateValues() {}
	}
}
