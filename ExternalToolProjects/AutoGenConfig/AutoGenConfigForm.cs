using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;

using static BizHawk.Experiment.AutoGenConfig.ConfigEditorUIGenerators;

namespace BizHawk.Experiment.AutoGenConfig
{
	[ExternalTool("AutoGenConfig")]
	public class AutoGenConfigForm : Form, IExternalToolForm
	{
		private static readonly WeakReference<ConfigEditorCache> _cache = new WeakReference<ConfigEditorCache>(new ConfigEditorCache(typeof(Config)));

		private static ConfigEditorCache Cache => _cache.TryGetTarget(out var c) ? c : new ConfigEditorCache(typeof(Config)).Also(_cache.SetTarget);

		private readonly IDictionary<string, Control> GroupUIs = new Dictionary<string, Control>();

		private readonly ConfigEditorMetadata Metadata = new ConfigEditorMetadata(Cache);

		[RequiredApi]
		private IEmu? EmuHawkAPI { get; set; }

		public override string Text => "AutoGenConfig";

		public bool UpdateBefore => false;

		public AutoGenConfigForm()
		{
			ClientSize = new Size(640, 720);
			KeyPreview = true;
			SuspendLayout();
			Controls.Add(new FlowLayoutPanel {
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				BorderStyle = BorderStyle.FixedSingle,
				Controls = {
					new Label { AutoSize = true, Text = "Legend:" },
					new Label { AutoSize = true, ForeColor = Metadata.ComparisonColors.UnchangedDefault, Text = "default, unchanged" },
					new Label { AutoSize = true, ForeColor = Metadata.ComparisonColors.Unchanged, Text = "custom, unchanged" },
					new Label { AutoSize = true, ForeColor = Metadata.ComparisonColors.ChangedUnset, Text = "default, was custom" },
					new Label { AutoSize = true, ForeColor = Metadata.ComparisonColors.ChangedInvalid, Text = "invalid" },
					new Label { AutoSize = true, ForeColor = Metadata.ComparisonColors.Changed, Text = "custom, changed" }
				},
				Location = new Point(4, 4),
				Padding = new Padding(0, 4, 0, 0),
				Size = new Size(ClientSize.Width - 8, 24),
				WrapContents = false
			});
			Controls.Add(GroupUIs[string.Empty] = new FlowLayoutPanel {
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				AutoScroll = true,
				FlowDirection = FlowDirection.TopDown,
				Location = new Point(4, 32),
				Size = new Size(ClientSize.Width - 8, ClientSize.Height - 64),
				WrapContents = false
			});
			var discardButton = new Button {
				Size = new Size(128, 24),
				Text = "Discard Changes"
			}.Also(it => it.Click += (clickEventSender, clickEventArgs) => Close());
			Controls.Add(new FlowLayoutPanel {
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				AutoScroll = true,
				AutoSize = true,
				Controls = {
					new Button {
						Size = new Size(128, 24),
						Text = "Review and Save..."
					}.Also(it => it.Click += (clickEventSender, clickEventArgs) =>
					{
						var state = GroupUIs.Values.SelectMany(group => group.Controls.Cast<Control>())
							.Select(c => (c, (c.Tag as ConfigPropEditorUITag)?.Generator))
							.Where(tuple =>
								tuple.Generator != null // Already iterating nested-config groupboxes as GroupUIs.Values; maybe this can be changed to iterate recursively starting with `GroupUIs[""]`?
									&& !tuple.Generator.MatchesBaseline(tuple.c, Metadata)
							)
							.Select(tuple => (tuple.c.Name, tuple.Generator, Baseline: Metadata.BaselineValues[tuple.c.Name], Current: tuple.Generator.GetTValue(tuple.c)))
							.Where(tuple => tuple.Baseline != tuple.Current)
							.ToList();
						if (state.Count == 0) {
							Close();
							return;
						}
						string DescribeChange((string Name, IConfigPropEditorUIGen Generator, object? Baseline, object? Current) change)
							=> $"{change.Name}: {change.Generator.SerializeTValue(change.Baseline)} => {change.Generator.SerializeTValue(change.Current)}{(change.Generator.TValueEquality(change.Current, Metadata.Cache.DefaultValues[change.Name]) ? " (default)" : string.Empty)}";
						if (MessageBox.Show(
							$"Choose OK to save these changes to the config (in-memory, close EmuHawk to save to disk):\n\n{string.Join("\n", state.Select(DescribeChange))}",
							"Save changes?",
							MessageBoxButtons.OKCancel
						) == DialogResult.OK)
						{
							//TODO save
							Close();
						}
					}),
					discardButton
				},
				Location = new Point(ClientSize.Width - 201, ClientSize.Height - 31),
				WrapContents = false
			});
			KeyDown += (keyDownEventSender, keyDownEventArgs) =>
			{
				// Eat TAB and Shift+TAB, do the expected tab behaviour. This means no tab in textboxes.
				if (keyDownEventArgs.KeyCode == Keys.Tab)
				{
					ProcessTabKey(keyDownEventArgs.Modifiers != Keys.Shift);
					keyDownEventArgs.Handled = true;
				}
			};
			Load += (loadEventSender, loadEventArgs) =>
			{
				// This magic works so long as `GroupUIs[""]` is set to the main FLP before loading, and we create all the GroupBoxes before trying to populate them.
				foreach (var (nesting, fi) in Metadata.Cache.Groups)
				{
					GroupUIs[nesting].Controls.Add(new GroupBox {
						Controls = {
							new FlowLayoutPanel {
								AutoScroll = true,
								AutoSize = true,
								Dock = DockStyle.Fill,
								FlowDirection = FlowDirection.TopDown,
								WrapContents = false
							}.Also(it => GroupUIs[$"{nesting}/{fi.Name}"] = it)
						},
						Size = new Size(560, 300),
						Text = fi.Name
					});
				}
				var config = (EmuHawkAPI as EmuApi ?? throw new Exception("required API wasn't fulfilled")).ForbiddenConfigReference;
				var groupings = new Dictionary<string, object> { [string.Empty] = config };
				void TraverseGroupings(object groupingObj, string parentNesting)
				{
					foreach (var (_, fi) in Metadata.Cache.Groups.Where(tuple => tuple.Item1 == parentNesting))
					{
						var nesting = $"{parentNesting}/{fi.Name}";
						TraverseGroupings(groupings[nesting] = fi.GetValue(groupingObj), nesting);
					}
				}
				TraverseGroupings(config, string.Empty);
				foreach (var (nesting, pi, gen) in Metadata.Cache.PropEditorUIGenerators)
				{
					GroupUIs[nesting].Controls.Add(gen.GenerateControl(nesting, pi, groupings[nesting], Metadata));
				}
				discardButton.Select();
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
