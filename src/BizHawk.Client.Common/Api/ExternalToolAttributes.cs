#nullable enable

using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
#pragma warning disable MA0057 // oops, should have called these `*Attribute`, too late now --yoshi
	public static class ExternalToolApplicability
	{
		/// <remarks>This class is not deprecated, do not remove it.</remarks>
		[AttributeUsage(AttributeTargets.Class)]
		[Obsolete("this is the default behaviour, you can safely omit this attribute")]
		public sealed class Always : ExternalToolApplicabilityAttributeBase
		{
			public override bool NotApplicableTo(string sysID)
				=> false;

			public override bool NotApplicableTo(string romHash, string? sysID)
				=> false;
		}

		[AttributeUsage(AttributeTargets.Class)]
		public sealed class AnyRomLoaded : ExternalToolApplicabilityAttributeBase
		{
			public override bool NotApplicableTo(string sysID)
				=> sysID is VSystemID.Raw.NULL;

			public override bool NotApplicableTo(string romHash, string? sysID)
				=> sysID is VSystemID.Raw.NULL;
		}

		[AttributeUsage(AttributeTargets.Class)]
		public sealed class RomList : ExternalToolApplicabilityAttributeBase
		{
			private readonly IList<string> _romHashes;

			private readonly string _sysID;

			public RomList(string sysID, params string[] romHashes)
			{
				if (sysID is VSystemID.Raw.NULL) throw new ArgumentException("there are no roms for the NULL system", nameof(sysID));
				if (!romHashes.All(NumericStringExtensions.IsHex)) throw new ArgumentException("misformatted hash", nameof(romHashes));
				_romHashes = romHashes.ToList();
				_sysID = sysID;
			}

			public override bool NotApplicableTo(string sysID)
				=> sysID != _sysID;

			public override bool NotApplicableTo(string romHash, string? sysID)
				=> sysID != _sysID || !_romHashes.Contains(romHash);
		}

		[AttributeUsage(AttributeTargets.Class)]
		public sealed class SingleRom : ExternalToolApplicabilityAttributeBase
		{
			private readonly string _romHash;

			private readonly string _sysID;

			public SingleRom(string sysID, string romHash)
			{
				if (sysID is VSystemID.Raw.NULL) throw new ArgumentException("there are no roms for the NULL system", nameof(sysID));
				if (!romHash.IsHex()) throw new ArgumentException("misformatted hash", nameof(romHash));
				_romHash = romHash;
				_sysID = sysID;
			}

			public override bool NotApplicableTo(string sysID)
				=> sysID != _sysID;

			public override bool NotApplicableTo(string romHash, string? sysID)
				=> sysID != _sysID || romHash != _romHash;
		}

		[AttributeUsage(AttributeTargets.Class)]
		public sealed class SingleSystem : ExternalToolApplicabilityAttributeBase
		{
			private readonly string _sysID;

			public SingleSystem(string sysID)
				=> _sysID = sysID;

			public override bool NotApplicableTo(string sysID)
				=> sysID != _sysID;

			public override bool NotApplicableTo(string romHash, string? sysID)
				=> sysID != _sysID;
		}
	}

	public abstract class ExternalToolApplicabilityAttributeBase : Attribute
	{
		public abstract bool NotApplicableTo(string sysID);

		public abstract bool NotApplicableTo(string romHash, string? sysID);

		public class DuplicateException : Exception {}
	}
#pragma warning disable MA0057

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ExternalToolAttribute : Attribute
	{
		public string? Description { get; set; }

		public string[]? LoadAssemblyFiles { get; set; }

		public readonly string Name;

		public ExternalToolAttribute(string? name)
			=> Name = string.IsNullOrWhiteSpace(name) ? Guid.NewGuid().ToString() : name!;

		public class MissingException : Exception {}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ExternalToolEmbeddedIconAttribute : Attribute
	{
		/// <remarks>The full path, including the assembly name.</remarks>
		public readonly string ResourcePath;

		/// <param name="resourcePath">The full path, including the assembly name.</param>
		public ExternalToolEmbeddedIconAttribute(string resourcePath)
			=> ResourcePath = resourcePath;
	}
}
