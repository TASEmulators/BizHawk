using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
		private static readonly IList<(string, FieldInfo)> CachedGroupings;

		private static readonly IList<(string, PropertyInfo, IConfigPropEditorUIGen<Control>)> CachedPropEditorUIGenerators;

		public static ComparisonColors ComparisonColors = new ComparisonColors
		{
			Changed = Color.FromArgb(unchecked((int) 0xFFBF5F1F)),
			ChangedInvalid = Color.FromArgb(unchecked((int) 0xFF9F0000)),
			ChangedUnset = Color.FromArgb(unchecked((int) 0xFFBF1F5F)),
			Unchanged = Color.FromArgb(unchecked((int) 0xFF00003F)),
			UnchangedDefault = Color.Black
		};

		public static readonly IDictionary<string, object?> DefaultValues;

		static AutoGenConfigForm()
		{
			CachedGroupings = new List<(string, FieldInfo)>();
			CachedPropEditorUIGenerators = new List<(string, PropertyInfo, IConfigPropEditorUIGen<Control>)>();
			DefaultValues = new Dictionary<string, object?>();
			static void TraversePropertiesOf(Type type, string nesting)
			{
				foreach (var pi in type.GetProperties()
					.Where(pi => pi.GetCustomAttributes(typeof(EditableAttribute), false).All(attr => ((EditableAttribute) attr).AllowEdit)))
				{
					CachedPropEditorUIGenerators.Add((nesting, pi, FallbackGenerators.TryGetValue(pi.PropertyType, out var gen) ? gen : FinalFallbackGenerator));
					DefaultValues[$"{nesting}/{pi.Name}"] = pi.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault()
						?.Let(it => ((DefaultValueAttribute) it).Value)
						?? TrueGenericDefault(pi.PropertyType);
				}
				foreach (var fi in type.GetFields()
					.Where(fi => fi.CustomAttributes.Any(cad => cad.AttributeType == typeof(ConfigGroupingStructAttribute))))
				{
					CachedGroupings.Add((nesting, fi));
					TraversePropertiesOf(fi.FieldType, $"{nesting}/{fi.Name}");
				}
			}
			TraversePropertiesOf(typeof(Config), string.Empty);
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

		public readonly IDictionary<string, Control> GroupingUIs = new Dictionary<string, Control>();

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
					new Label { AutoSize = true, ForeColor = ComparisonColors.ChangedUnset, Text = "default, was custom" },
					new Label { AutoSize = true, ForeColor = ComparisonColors.ChangedInvalid, Text = "invalid" },
					new Label { AutoSize = true, ForeColor = ComparisonColors.Changed, Text = "custom, changed" }
				},
				Location = new Point(4, 4),
				Padding = new Padding(0, 4, 0, 0),
				Size = new Size(ClientSize.Width - 8, 24),
				WrapContents = false
			});
			Controls.Add(GroupingUIs[string.Empty] = new FlowLayoutPanel {
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
				// This magic works so long as `GroupingUIs[""]` is set to the main FLP before loading, and we create all the GroupBoxes before trying to populate them.
				foreach (var (nesting, fi) in CachedGroupings)
				{
					GroupingUIs[nesting].Controls.Add(new GroupBox {
						Controls = {
							new FlowLayoutPanel {
								AutoScroll = true,
								AutoSize = true,
								Dock = DockStyle.Fill,
								FlowDirection = FlowDirection.TopDown,
								WrapContents = false
							}.Also(it => GroupingUIs[$"{nesting}/{fi.Name}"] = it)
						},
						Size = new Size(400, 300),
						Text = fi.Name
					});
				}
				var config = (EmuHawkAPI as EmuApi ?? throw new Exception("required API wasn't fulfilled")).ForbiddenConfigReference;
				var groupings = new Dictionary<string, object> { [string.Empty] = config };
				void TraverseGroupings(object groupingObj, string nesting)
				{
					foreach (var (_, fi) in CachedGroupings.Where(tuple => tuple.Item1 == nesting))
					{
						var newNesting = $"{nesting}/{fi.Name}";
						TraverseGroupings(groupings[newNesting] = fi.GetValue(groupingObj), newNesting);
					}
				}
				TraverseGroupings(config, string.Empty);
				foreach (var (nesting, pi, gen) in CachedPropEditorUIGenerators)
				{
					GroupingUIs[nesting].Controls.Add(gen.GenerateControl(nesting, pi, groupings[nesting], BaselineValues));
				}
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
