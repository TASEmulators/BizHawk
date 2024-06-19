using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Experiment.AutoGenConfig
{
	public static class ConfigEditorUIGenerators
	{
		public interface IConfigPropEditorUIGen
		{
			bool MatchesBaseline(Control c, ConfigEditorMetadata metadata);

			/// <returns>
			/// A <see cref="Control"/> with <see cref="Control.Name"/> set to the property name (including nesting) and <see cref="Control.Tag"/> set to a <see cref="ConfigPropEditorUITag"/>.<br/>
			/// Multiple <see cref="Control">Controls</see> may be needed, the returned value is always the topmost parent (probably a <see cref="FlowLayoutPanel"/>).
			/// </returns>
			/// <remarks><see cref="ConfigEditorMetadata.BaselineValues"/> in <paramref name="metadata"/> will be updated.</remarks>
			Control GenerateControl(string nesting, PropertyInfo pi, object config, ConfigEditorMetadata metadata);

			/// <returns>value represented by <paramref name="c"/> or its nested <see cref="Control">Controls</see></returns>
			object? GetTValue(Control c);

			string SerializeTValue(object? v);

			/// <returns><see langword="true"/> iff equal</returns>
			bool TValueEquality(object? a, object? b);
		}

		public sealed class CheckBoxForBoolEditorUIGen : ConfigPropEditorUIGenValT<CheckBox, bool>
		{
			protected override void ControlEventHandler(object sender, EventArgs args)
			{
				var cb = (CheckBox) sender;
				cb.ForeColor = GetComparisonColor(cb.Name, cb.Checked, (ConfigPropEditorUITag) cb.Tag);
			}

			protected override CheckBox GenerateControl(string nesting, PropertyInfo pi, object config, ConfigEditorMetadata metadata)
			{
				if (pi.PropertyType != typeof(bool)) throw new Exception();
				var baseline = (bool) pi.GetValue(config);
				var nestedName = $"{nesting}/{pi.Name}";
				metadata.BaselineValues[nestedName] = baseline;
				var tag = new ConfigPropEditorUITag(metadata, this);
				return new CheckBox
				{
					AutoSize = true,
					Checked = baseline,
					ForeColor = GetUnchangedComparisonColor(nestedName, in baseline, tag),
					Name = nestedName,
					Tag = tag,
					Text = GetPropertyNameDesc(pi)
				}.Also(it => it.CheckedChanged += ControlEventHandler);
			}

			protected override bool GetTValue(CheckBox c) => c.Checked;

			protected override bool TValueEquality(bool a, bool b) => a == b;
		}

		public sealed class ComparisonColors
		{
			public readonly Color Changed;

			public readonly Color ChangedInvalid;

			public readonly Color ChangedUnset;

			public readonly Color Unchanged;

			public readonly Color UnchangedDefault;

			public ComparisonColors(Color changed, Color changedInvalid, Color changedUnset, Color unchanged, Color unchangedDefault)
			{
				Changed = changed;
				ChangedInvalid = changedInvalid;
				ChangedUnset = changedUnset;
				Unchanged = unchanged;
				UnchangedDefault = unchangedDefault;
			}

			public static readonly ComparisonColors Defaults = new ComparisonColors(
				Color.FromArgb(unchecked((int) 0xFFBF5F1F)),
				Color.FromArgb(unchecked((int) 0xFF9F0000)),
				Color.FromArgb(unchecked((int) 0xFFBF1F5F)),
				Color.FromArgb(unchecked((int) 0xFF00003F)),
				Color.Black
			);
		}

		/// <summary>Holds computed data that, with reference to a specified config-containing type, cannot change during the lifetime of the program.</summary>
		public sealed class ConfigEditorCache
		{
			public readonly IDictionary<string, object?> DefaultValues = new Dictionary<string, object?>();

			private readonly IReadOnlyDictionary<Type, IConfigPropEditorUIGen> FallbackGenerators;

			private readonly IConfigPropEditorUIGen FinalFallbackGenerator;

			public readonly IList<(string, FieldInfo)> Groups = new List<(string, FieldInfo)>();

			public readonly IList<(string, PropertyInfo, IConfigPropEditorUIGen)> PropEditorUIGenerators = new List<(string, PropertyInfo, IConfigPropEditorUIGen)>();

			public ConfigEditorCache(Type configType, IReadOnlyDictionary<Type, IConfigPropEditorUIGen>? fallbackGenerators = null, IConfigPropEditorUIGen? finalFallbackGenerator = null)
			{
				FallbackGenerators = fallbackGenerators == null
					? DefaultFallbackGeneratorSet
					: new Dictionary<Type, IConfigPropEditorUIGen>().Also(it =>
					{
						// Concat will effectively use parameter to overwrite where specified
						foreach (var kvp in DefaultFallbackGeneratorSet.Concat(fallbackGenerators)) it[kvp.Key] = kvp.Value;
					});
				FinalFallbackGenerator = finalFallbackGenerator ?? new UnrepresentablePropEditorUIGen();
				static object? TrueGenericDefault(Type type)
				{
					try
					{
						return Activator.CreateInstance(type);
					}
					catch
					{
						return null;
					}
				}
				void TraversePropertiesOf(Type type, string nesting)
				{
					foreach (var pi in type.GetProperties()
						.Where(pi => pi.GetCustomAttributes(typeof(EditableAttribute), false).All(attr => ((EditableAttribute) attr).AllowEdit)))
					{
						var gen = FallbackGenerators.TryGetValue(pi.PropertyType, out var fallbackGen) ? fallbackGen : FinalFallbackGenerator;
						if (pi.GetCustomAttributes(typeof(EditorUIGeneratorAttribute), false).FirstOrDefault() is EditorUIGeneratorAttribute attr
						    && typeof(IConfigPropEditorUIGen).IsAssignableFrom(attr.GeneratorType)
						    && TrueGenericDefault(attr.GeneratorType) is IConfigPropEditorUIGen overrideGen)
						{
							gen = overrideGen;
						}
						PropEditorUIGenerators.Add((nesting, pi, gen));
						DefaultValues[$"{nesting}/{pi.Name}"] = pi.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault()
							?.Let(it => ((DefaultValueAttribute) it).Value)
							?? TrueGenericDefault(pi.PropertyType);
					}
					foreach (var fi in type.GetFields()
						.Where(fi => fi.CustomAttributes.Any(cad => cad.AttributeType == typeof(ConfigGroupingStructAttribute))))
					{
						Groups.Add((nesting, fi));
						TraversePropertiesOf(fi.FieldType, $"{nesting}/{fi.Name}");
					}
				}
				TraversePropertiesOf(configType, string.Empty);
			}

			private static readonly IReadOnlyDictionary<Type, IConfigPropEditorUIGen> DefaultFallbackGeneratorSet = new Dictionary<Type, IConfigPropEditorUIGen> {
				[typeof(bool)] = new CheckBoxForBoolEditorUIGen(),
				[typeof(int)] = new NumericUpDownForInt32EditorUIGen(),
				[typeof(string)] = new TextBoxForStringEditorUIGen()
			};
		}

		public sealed class ConfigEditorMetadata
		{
			public readonly IDictionary<string, object?> BaselineValues = new Dictionary<string, object?>();

			/// <inheritdoc cref="ConfigEditorCache"/>
			public readonly ConfigEditorCache Cache;

			public readonly ComparisonColors ComparisonColors;

			/// <param name="cache">global cache</param>
			/// <param name="colors">default of <see langword="null"/> uses <see cref="ConfigEditorUIGenerators.ComparisonColors.Defaults"/></param>
			public ConfigEditorMetadata(ConfigEditorCache cache, ComparisonColors? colors = null)
			{
				Cache = cache;
				ComparisonColors = colors ?? ComparisonColors.Defaults;
			}
		}

		public sealed class ConfigPropEditorUITag
		{
			public readonly IConfigPropEditorUIGen Generator;

			public readonly ConfigEditorMetadata Metadata;

			public ConfigPropEditorUITag(ConfigEditorMetadata metadata, IConfigPropEditorUIGen generator)
			{
				Metadata = metadata;
				Generator = generator;
			}
		}

		public abstract class ConfigPropEditorUIGen<TListed, TValue> : IConfigPropEditorUIGen
			where TListed : Control
		{
			protected abstract void ControlEventHandler(object sender, EventArgs args);

			/// <inheritdoc cref="IConfigPropEditorUIGen.GenerateControl"/>
			protected abstract TListed GenerateControl(string nesting, PropertyInfo pi, object config, ConfigEditorMetadata metadata);

			Control IConfigPropEditorUIGen.GenerateControl(string nesting, PropertyInfo pi, object config, ConfigEditorMetadata metadata) => GenerateControl(nesting, pi, config, metadata);

			/// <inheritdoc cref="IConfigPropEditorUIGen.GetTValue"/>
			protected abstract TValue GetTValue(TListed c);

			object? IConfigPropEditorUIGen.GetTValue(Control c) => GetTValue((TListed) c);

			/// <remarks>
			/// Default implementation didn't play nice with <see langword="null"/>, so multiple behaviours are available for custom generators:
			/// inherit from <see cref="ConfigPropEditorUIGenRefT{T,T}"/> or <see cref="ConfigPropEditorUIGenValT{T,T}"/> instead.
			/// </remarks>
			protected abstract bool MatchesBaseline(TListed c, ConfigEditorMetadata metadata);

			bool IConfigPropEditorUIGen.MatchesBaseline(Control c, ConfigEditorMetadata metadata) => MatchesBaseline((TListed) c, metadata);

			protected virtual string SerializeTValue(TValue v) => v?.ToString() ?? NULL_SERIALIZATION;

#pragma warning disable CS8600
#pragma warning disable CS8604
			string IConfigPropEditorUIGen.SerializeTValue(object? v) => SerializeTValue((TValue) v);
#pragma warning restore CS8604
#pragma warning restore CS8600

			/// <inheritdoc cref="IConfigPropEditorUIGen.TValueEquality"/>
			protected abstract bool TValueEquality(TValue a, TValue b);

#pragma warning disable CS8600
#pragma warning disable CS8604
			bool IConfigPropEditorUIGen.TValueEquality(object? a, object? b) => TValueEquality((TValue) a, (TValue) b);
#pragma warning restore CS8604
#pragma warning restore CS8600

			protected const string NULL_SERIALIZATION = "(null)";

			protected static Color GetComparisonColor<T>(string nestedName, T? currentValue, ConfigPropEditorUITag tag)
				where T : class
				=> tag.Generator.TValueEquality(currentValue, (T?) tag.Metadata.BaselineValues[nestedName])
					? GetUnchangedComparisonColor(nestedName, currentValue, tag)
					: tag.Generator.TValueEquality(currentValue, (T?) tag.Metadata.Cache.DefaultValues[nestedName])
						? tag.Metadata.ComparisonColors.ChangedUnset
						: tag.Metadata.ComparisonColors.Changed;

			protected static Color GetComparisonColor<T>(string nestedName, in T currentValue, ConfigPropEditorUITag tag)
				where T : struct
				=> tag.Generator.TValueEquality(currentValue, tag.Metadata.BaselineValues[nestedName] is T baseline ? baseline : throw new Exception())
					? GetUnchangedComparisonColor(nestedName, in currentValue, tag)
					: tag.Generator.TValueEquality(currentValue, tag.Metadata.Cache.DefaultValues[nestedName] is T defaultValue ? defaultValue : throw new Exception())
						? tag.Metadata.ComparisonColors.ChangedUnset
						: tag.Metadata.ComparisonColors.Changed;

			protected static string GetPropertyNameDesc(PropertyInfo pi)
				=> pi.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault()
					?.Let(it => $"{pi.Name}: {((DescriptionAttribute) it).Description}")
					?? pi.Name;

			protected static Color GetUnchangedComparisonColor<T>(string nestedName, T? currentValue, ConfigPropEditorUITag tag)
				where T : class
				=> tag.Generator.TValueEquality(currentValue, (T?) tag.Metadata.Cache.DefaultValues[nestedName])
					? tag.Metadata.ComparisonColors.UnchangedDefault
					: tag.Metadata.ComparisonColors.Unchanged;

			protected static Color GetUnchangedComparisonColor<T>(string nestedName, in T currentValue, ConfigPropEditorUITag tag)
				where T : struct
				=> tag.Generator.TValueEquality(currentValue, tag.Metadata.Cache.DefaultValues[nestedName] is T defaultValue ? defaultValue : throw new Exception())
					? tag.Metadata.ComparisonColors.UnchangedDefault
					: tag.Metadata.ComparisonColors.Unchanged;
		}

		public abstract class ConfigPropEditorUIGenRefT<TListed, TValue> : ConfigPropEditorUIGen<TListed, TValue?>
			where TListed : Control
			where TValue : class
		{
			/// <remarks>
			/// Checked in <see cref="MatchesBaseline"/> in the case where the baseline value is <see langword="null"/>.
			/// If its implementation returns <see langword="false"/>, an exception will be thrown, otherwise <see cref="ConfigPropEditorUIGen{T,T}.TValueEquality"/>' implementation will be called with <see langword="null"/>.
			/// </remarks>
			protected abstract bool AllowNull { get; }

			/// <inheritdoc cref="ConfigPropEditorUIGen{T,T}.MatchesBaseline"/>
			protected override bool MatchesBaseline(TListed c, ConfigEditorMetadata metadata)
				=> metadata.BaselineValues[c.Name] is TValue v
					? TValueEquality(GetTValue(c), v)
					: AllowNull ? TValueEquality(GetTValue(c), null) : throw new Exception();
		}

		public abstract class ConfigPropEditorUIGenValT<TListed, TValue> : ConfigPropEditorUIGen<TListed, TValue>
			where TListed : Control
			where TValue : struct
		{
			/// <inheritdoc cref="ConfigPropEditorUIGen{T,T}.MatchesBaseline"/>
			protected override bool MatchesBaseline(TListed c, ConfigEditorMetadata metadata)
				=> metadata.BaselineValues[c.Name] is TValue v ? TValueEquality(GetTValue(c), v) : throw new Exception();
		}

		public sealed class NumericUpDownForInt32EditorUIGen : ConfigPropEditorUIGenValT<FlowLayoutPanel, int>
		{
			protected override void ControlEventHandler(object sender, EventArgs args)
			{
				var nud = (NumericUpDown) sender;
				nud.Parent.ForeColor = GetComparisonColor(nud.Parent.Name, (int) nud.Value, (ConfigPropEditorUITag) nud.Parent.Tag);
			}

			protected override FlowLayoutPanel GenerateControl(string nesting, PropertyInfo pi, object config, ConfigEditorMetadata metadata)
			{
				if (pi.PropertyType != typeof(int)) throw new Exception();
				var baseline = (int) pi.GetValue(config);
				var nestedName = $"{nesting}/{pi.Name}";
				metadata.BaselineValues[nestedName] = baseline;
				var tag = new ConfigPropEditorUITag(metadata, this);
				return new FlowLayoutPanel {
					AutoSize = true,
					Controls = {
						new Label { Anchor = AnchorStyles.None, AutoSize = true, Text = GetPropertyNameDesc(pi) },
						new NumericUpDown
						{
							Maximum = int.MaxValue,
							Minimum = int.MinValue,
							Size = new Size(72, 20),
							Value = baseline
						}.Also(it =>
						{
							if (pi.GetCustomAttributes(typeof(RangeAttribute), false).FirstOrDefault() is RangeAttribute range)
							{
								it.Maximum = (int) range.Maximum;
								it.Minimum = (int) range.Minimum;
							}
							it.ValueChanged += ControlEventHandler;
						})
					},
					ForeColor = GetUnchangedComparisonColor(nestedName, in baseline, tag),
					Name = nestedName,
					Tag = tag
				};
			}

			protected override int GetTValue(FlowLayoutPanel c) => (int) ((NumericUpDown) c.Controls[1]).Value;

			protected override bool TValueEquality(int a, int b) => a == b;
		}

		public sealed class TextBoxForStringEditorUIGen : ConfigPropEditorUIGenRefT<FlowLayoutPanel, string>
		{
			protected override bool AllowNull => true;

			protected override void ControlEventHandler(object sender, EventArgs args)
			{
				var tb = (TextBox) sender;
				tb.Parent.ForeColor = GetComparisonColor(tb.Parent.Name, tb.Text, (ConfigPropEditorUITag) tb.Parent.Tag);
			}

			protected override FlowLayoutPanel GenerateControl(string nesting, PropertyInfo pi, object config, ConfigEditorMetadata metadata)
			{
				if (!pi.PropertyType.IsAssignableFrom(typeof(string))) throw new Exception();
				var baseline = (string) pi.GetValue(config);
				var nestedName = $"{nesting}/{pi.Name}";
				metadata.BaselineValues[nestedName] = baseline;
				var tag = new ConfigPropEditorUITag(metadata, this);
				return new FlowLayoutPanel {
					AutoSize = true,
					Controls = {
						new Label { Anchor = AnchorStyles.None, AutoSize = true, Text = GetPropertyNameDesc(pi) },
						new TextBox { AutoSize = true, Text = baseline }.Also(it => it.TextChanged += ControlEventHandler)
					},
					ForeColor = GetUnchangedComparisonColor(nestedName, baseline, tag),
					Name = nestedName,
					Tag = tag
				};
			}

			protected override string? GetTValue(FlowLayoutPanel c) => ((TextBox) c.Controls[1]).Text;

			protected override string SerializeTValue(string? v) => v == null ? NULL_SERIALIZATION : $"\"{v}\"";

			protected override bool TValueEquality(string? a, string? b) => a == b || string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b);
		}

		public sealed class UnrepresentablePropEditorUIGen : ConfigPropEditorUIGen<GroupBox, object?>
		{
			protected override void ControlEventHandler(object sender, EventArgs args) => throw new InvalidOperationException();

			protected override GroupBox GenerateControl(string nesting, PropertyInfo pi, object config, ConfigEditorMetadata metadata)
				=> new GroupBox {
					AutoSize = true,
					Controls = {
						new FlowLayoutPanel {
							AutoSize = true,
							Controls = { new Label { AutoSize = true, Text = $"no editor found for type {pi.PropertyType}" } },
							Location = new Point(4, 16),
							MaximumSize = new Size(int.MaxValue, 20)
						}
					},
					MaximumSize = new Size(int.MaxValue, 40),
					Name = $"{nesting}/{pi.Name}",
					Tag = new ConfigPropEditorUITag(metadata, this),
					Text = pi.Name
				};

			protected override object? GetTValue(GroupBox c) => throw new InvalidOperationException();

			protected override bool MatchesBaseline(GroupBox c, ConfigEditorMetadata metadata) => true;

			protected override bool TValueEquality(object? a, object? b) => throw new InvalidOperationException();
		}
	}
}
