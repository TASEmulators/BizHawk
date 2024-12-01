using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Defines the schema for all the currently available controls for an IEmulator instance
	/// </summary>
	/// <seealso cref="IEmulator" />
	public class ControllerDefinition
	{
		private IList<string> _buttons = new List<string>();

		private bool _mutable = true;

		private IReadOnlyList<IReadOnlyList<(string, AxisSpec?)>>? _orderedControls;

		/// <summary>starts with console buttons, then each player's buttons individually</summary>
		public IReadOnlyList<IReadOnlyList<(string Name, AxisSpec? AxisSpec)>> ControlsOrdered
		{
			get
			{
				if (_orderedControls is not null) return _orderedControls;
				if (!_mutable) return _orderedControls = GenOrderedControls();
				const string ERR_MSG = $"this {nameof(ControllerDefinition)} has not yet been built and sealed, so it is not safe to enumerate this while it could still be mutated";
				throw new InvalidOperationException(ERR_MSG);
			}
		}

		public readonly string Name;

		private Dictionary<string, char>? _mnemonicsCache;
		public IReadOnlyDictionary<string, char>? MnemonicsCache => _mnemonicsCache;

		/// <remarks>
		/// TODO: this should probably be called in <see cref="MakeImmutable"/>,
		/// </remarks>
		public void BuildMnemonicsCache(string sysID)
		{
			if (_mutable)
				throw new InvalidOperationException($"this {nameof(ControllerDefinition)} has not yet been built and sealed; can't build mnemonics cache");

			_mnemonicsCache ??= BoolButtons.ToDictionary(
				static buttonName => buttonName,
				buttonName => Bk2MnemonicLookup.Lookup(buttonName, sysID));
		}

		public ControllerDefinition(string name)
			=> Name = name;

		public ControllerDefinition(ControllerDefinition copyFrom, string? withName = null)
			: this(withName ?? copyFrom.Name)
		{
			BoolButtons.AddRange(copyFrom.BoolButtons);
			foreach (var kvp in copyFrom.Axes) Axes.Add(kvp);
			HapticsChannels.AddRange(copyFrom.HapticsChannels);
			CategoryLabels = copyFrom.CategoryLabels;
			_mnemonicsCache = copyFrom._mnemonicsCache;
			MakeImmutable();
		}

		/// <summary>
		/// Gets or sets a list of all button types that have a boolean (on/off) value
		/// </summary>
		public IList<string> BoolButtons
		{
			get => _buttons;
			set
			{
				AssertMutable();
				_buttons = value;
			}
		}

		public readonly AxisDict Axes = new AxisDict();

		/// <summary>Contains names of virtual haptic feedback channels, e.g. <c>{ "P1 Mono" }</c>, <c>{ "P2 Left", "P2 Right" }</c>.</summary>
		public IList<string> HapticsChannels { get; private set; } = new List<string>();

		/// <summary>
		/// Gets the category labels. These labels provide a means of categorizing controls in various controller display and config screens
		/// </summary>
		public IDictionary<string, string> CategoryLabels { get; private set; } = new Dictionary<string, string>();

		public void ApplyAxisConstraints(string constraintClass, IDictionary<string, int> axes)
		{
			if (!Axes.HasContraints) return;
			foreach (var (k, v) in Axes)
			{
				var constraint = v.Constraint;
				if (constraint == null || constraint.Class != constraintClass) continue;
				switch (constraint)
				{
					case CircularAxisConstraint circular:
						var xAxis = k;
						var yAxis = circular.PairedAxis;
						(axes[xAxis], axes[yAxis]) = circular.ApplyTo(
							CollectionExtensions.GetValueOrDefault(axes, xAxis),
							CollectionExtensions.GetValueOrDefault(axes, yAxis));
						break;
				}
			}
		}

		private void AssertMutable()
		{
			const string ERR_MSG = "this " + nameof(ControllerDefinition) + " has been built and sealed, and may not be mutated";
			if (!_mutable) throw new InvalidOperationException(ERR_MSG);
		}

		protected virtual IReadOnlyList<IReadOnlyList<(string Name, AxisSpec? AxisSpec)>> GenOrderedControls()
		{
			var ret = new List<(string, AxisSpec?)>[PlayerCount + 1];
			for (var i = 0; i < ret.Length; i++) ret[i] = new();
			foreach ((string buttonName, var axisSpec) in Axes) ret[PlayerNumber(buttonName)].Add((buttonName, axisSpec));
			foreach (var btn in BoolButtons) ret[PlayerNumber(btn)].Add((btn, null));
			return ret;
		}

		/// <summary>permanently disables the ability to mutate this instance; returns this reference</summary>
		public ControllerDefinition MakeImmutable()
		{
			BoolButtons = BoolButtons.ToImmutableList();
			Axes.MakeImmutable();
			HapticsChannels = HapticsChannels.ToImmutableList();
			CategoryLabels = CategoryLabels.ToImmutableDictionary();
			_mutable = false;
			return this;
		}

		public static int PlayerNumber(string buttonName)
		{
			var match = PlayerRegex.Match(buttonName);
			return match.Success
				? int.Parse(match.Groups[1].Value)
				: 0;
		}

		private static readonly Regex PlayerRegex = new Regex("^P(\\d+) ");

		public int PlayerCount
		{
			get
			{
				var allNames = Axes.Keys.Concat(BoolButtons).ToList();
				var player = allNames
					.Select(PlayerNumber)
					.DefaultIfEmpty(0)
					.Max();

				if (player > 0)
				{
					return player;
				}

				// Hack for things like gameboy/ti-83 as opposed to genesis with no controllers plugged in
				return allNames.Exists(static b => b.StartsWithOrdinal("Up")) ? 1 : 0;
			}
		}

		public bool Any()
		{
			return BoolButtons.Any() || Axes.Any();
		}
	}
}
