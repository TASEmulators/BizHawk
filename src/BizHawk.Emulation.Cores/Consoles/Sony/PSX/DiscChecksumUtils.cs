#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	internal static class DiscChecksumUtils
	{
		private static readonly ConditionalWeakTable<Disc, string> _redumpHashCache = new();

		public static string CalculateDiscHashesImpl(IReadOnlyList<Disc> discs)
			=> string.Join("\n", discs.Select(static disc =>
			{
				if (_redumpHashCache.TryGetValue(disc, out var hash)) return hash;
				try
				{
					hash = $"{new DiscHasher(disc).Calculate_PSX_RedumpHash():X8} {disc.Name}";
				}
				catch
				{
					// ignored
					return string.Empty;
				}
				_redumpHashCache.Add(disc, hash);
				return hash;
			}));

		public static string GenQuickRomDetails(IReadOnlyList<IDiscAsset> discs)
		{
			static string DiscHashWarningText(string discHash)
			{
				var game = Database.CheckDatabase(discHash);
				return game?.Status is null or RomStatus.BadDump or RomStatus.NotInDatabase or RomStatus.Overdump
					? "Disc could not be identified as known-good. Look for a better rip."
					: $@"Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{discHash}
Nonetheless it could be an unrecognized romhack or patched version.
According to redump.org, the ideal hash for entire disc is: CRC32:{game.GetStringValue("dh")}
The file you loaded hasn't been hashed entirely (it would take too long)
Compare it with the full hash calculated by the PSX menu's Hash Discs tool";
			}
			return discs.Count is 0
				? "PSX exe"
				: string.Join("\n", discs.SelectMany(static disc => new[]
				{
					Path.GetFileName(disc.DiscName),
					DiscHashWarningText(new DiscHasher(disc.DiscData).Calculate_PSX_BizIDHash()),
					"-------------------------",
				}));
		}
	}
}
