using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common
{
	public static class ExternalToolApplicability
	{
		/// <remarks>This class is not deprecated, do not remove it.</remarks>
		[AttributeUsage(AttributeTargets.Class)]
		[Obsolete("this is the default behaviour, you can safely omit this attribute")]
		public sealed class Always : ExternalToolApplicabilityAttributeBase
		{
			public override bool NotApplicableTo(CoreSystem system) => false;

			public override bool NotApplicableTo(string romHash, CoreSystem? system) => false;
		}

		[AttributeUsage(AttributeTargets.Class)]
		public sealed class AnyRomLoaded : ExternalToolApplicabilityAttributeBase
		{
			public override bool NotApplicableTo(CoreSystem system) => system == CoreSystem.Null;

			public override bool NotApplicableTo(string romHash, CoreSystem? system) => system == CoreSystem.Null;
		}

		[AttributeUsage(AttributeTargets.Class)]
		public sealed class RomWhitelist : ExternalToolApplicabilityAttributeBase
		{
			private readonly IList<string> _romHashes;

			private readonly CoreSystem _system;

			public RomWhitelist(CoreSystem system, params string[] romHashes)
			{
				if (system == CoreSystem.Null) throw new ArgumentException("there are no roms for the NULL system", nameof(system));
				if (!romHashes.All(StringExtensions.IsHex)) throw new ArgumentException("misformatted hash", nameof(romHashes));
				_system = system;
				_romHashes = romHashes.ToList();
			}

			public override bool NotApplicableTo(CoreSystem system) => system != _system;

			public override bool NotApplicableTo(string romHash, CoreSystem? system) => system != _system || !_romHashes.Contains(romHash);
		}

		[AttributeUsage(AttributeTargets.Class)]
		public sealed class SingleRom : ExternalToolApplicabilityAttributeBase
		{
			private readonly string _romHash;

			private readonly CoreSystem _system;

			public SingleRom(CoreSystem system, string romHash)
			{
				if (system == CoreSystem.Null) throw new ArgumentException("there are no roms for the NULL system", nameof(system));
				if (!romHash.IsHex()) throw new ArgumentException("misformatted hash", nameof(romHash));
				_system = system;
				_romHash = romHash;
			}

			public override bool NotApplicableTo(CoreSystem system) => system != _system;

			public override bool NotApplicableTo(string romHash, CoreSystem? system) => system != _system || romHash != _romHash;
		}

		[AttributeUsage(AttributeTargets.Class)]
		public sealed class SingleSystem : ExternalToolApplicabilityAttributeBase
		{
			private readonly CoreSystem _system;

			public SingleSystem(CoreSystem system)
			{
				_system = system;
			}

			public override bool NotApplicableTo(CoreSystem system) => system != _system;

			public override bool NotApplicableTo(string romHash, CoreSystem? system) => system != _system;
		}
	}

	public abstract class ExternalToolApplicabilityAttributeBase : Attribute
	{
		public abstract bool NotApplicableTo(CoreSystem system);

		public abstract bool NotApplicableTo(string romHash, CoreSystem? system);

		public bool NotApplicableTo(string romHash) => NotApplicableTo(romHash, null);

		public class DuplicateException : Exception {}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ExternalToolAttribute : Attribute
	{
		public string Description { get; set; }

		public readonly string Name;

		public ExternalToolAttribute(string name)
		{
			Name = string.IsNullOrWhiteSpace(name) ? Guid.NewGuid().ToString() : name;
		}

		public class MissingException : Exception
		{
			public readonly bool OldAttributeFound;

			public MissingException(bool oldAttributeFound)
			{
				OldAttributeFound = oldAttributeFound;
			}
		}
	}

	public sealed class ExternalToolEmbeddedIconAttribute : Attribute
	{
		/// <remarks>The full path, including the assembly name.</remarks>
		public readonly string ResourcePath;

		/// <param name="resourcePath">The full path, including the assembly name.</param>
		public ExternalToolEmbeddedIconAttribute(string resourcePath)
		{
			ResourcePath = resourcePath;
		}
	}
}
