#!/usr/bin/env -S dotnet run --

#:package System.CommandLine@2.0.9

using System.Collections.Frozen;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

const string DIVIDER = "\n--------------------========================================--------------------\n";
const string FILENAME_PROGRESS = ".in_progress.txt";

Option<bool> discardOption = new("--discard-progress") { Description = "discard any progress from previous run(s)" };
Command updateCommand = new("update-dev", "launch each core in the local dev build and record its status") { discardOption };
updateCommand.SetAction(result => DoUpdateWizard(isBaseline: false, shouldResume: !result.GetValue(discardOption)));
Command baselineCommand = new("update-baseline", "as update-dev, but overwrite the baseline results instead of dev") { discardOption };
baselineCommand.SetAction(result => DoUpdateWizard(isBaseline: true, shouldResume: !result.GetValue(discardOption)));
RootCommand rootCommand = new("Libretro core tester");
rootCommand.Subcommands.Add(updateCommand);
rootCommand.Subcommands.Add(baselineCommand);

var result = CommandLineParser.Parse(rootCommand, args);
if (result.Errors.Count is not 0)
{
	result.Invoke();
	throw new ArgumentException(paramName: nameof(args), message: $"failed to parse command-line arguments: {result.Errors[0].Message}");
}
if (result.Action is not null)
{
	// means `--help` was passed, run whatever behaviour it normally has...
	return result.Invoke();
}
// else uhh idk
return 1;

static int DoUpdateWizard(bool isBaseline, bool shouldResume)
{
	// `cd $(dirname $0)`
	while (!(Directory.Exists(".git") || File.Exists("BizHawk.sln")))
	{
		Environment.CurrentDirectory = Path.GetDirectoryName(Environment.CurrentDirectory)!;
	}
	var repoRootDir = Environment.CurrentDirectory;
	var resultsDir = Path.Combine(repoRootDir, "LibretroBridge", "compatibility_testing");
	var progressFilePath = Path.Combine(resultsDir, FILENAME_PROGRESS);
	// `cd output`
	Environment.CurrentDirectory = Path.Combine(repoRootDir, "output");

	var romsDir = Path.Combine(resultsDir, "roms");
	//TODO fallback(s)?
	if (!Directory.Exists(romsDir)) throw new DirectoryNotFoundException($"no such directory {romsDir}");

	var coreBinariesDir = Environment.GetEnvironmentVariable("BIZHAWKBUILD_LIBRETRO_CORES");
	if (!Directory.Exists(coreBinariesDir)) coreBinariesDir = Path.Combine(repoRootDir, "output", "Libretro", "Cores");
	if (!Directory.Exists(coreBinariesDir)) throw new DirectoryNotFoundException($"no such directory $BIZHAWKBUILD_LIBRETRO_CORES (resolved to {coreBinariesDir})");

	var emuhawkOrWrapper = Environment.GetEnvironmentVariable("BIZHAWKBUILD_WRAPPER_SCRIPT");
	if (!File.Exists(emuhawkOrWrapper)) emuhawkOrWrapper = OperatingSystem.IsWindows() ? "EmuHawk.exe" : "./EmuHawkMono.sh";

	HashSet<string> pastResults = new();
	if (shouldResume && File.Exists(progressFilePath))
	{
		pastResults = new(File.ReadAllLines(progressFilePath).Select(s => s.SubstringAfterLast('@')));
		Console.Error.WriteLine($"Loaded progress from interrupted run (quit and delete {FILENAME_PROGRESS} if this was not intended)");
	}
	Console.Error.WriteLine(DIVIDER);
	var sharedLibFileExt = OperatingSystem.IsWindows() ? ".dll" : ".so";
	foreach (var (coreID, coreBinaryFilePath) in Directory.EnumerateFiles(coreBinariesDir, $"*{sharedLibFileExt}")
		.Select(filePath => (filePath.SubstringAfterLast('/').RemoveSuffix(sharedLibFileExt).RemoveSuffix("_libretro"), filePath))
		.OrderBy(tuple => tuple.Item1))
	{
		if (pastResults.Contains(coreID))
		{
			Console.Error.WriteLine($"Already checked {coreID} in prior run; skipping (`--discard-progress` to start over)\n{DIVIDER}");
			continue;
		}
		if (!Extensions.SysIDLookup.TryGetValue(coreID, out var sysID) || sysID is Extensions.UNK)
		{
			Console.Error.WriteLine($"Relevant sysID for {coreID} is not known; skipping\n{DIVIDER}");
			continue;
		}
		if (sysID is Extensions.NOGAME)
		{
			Console.Error.WriteLine($"Launching {coreID} (nogame)");
			LaunchEmuHawk(emuhawkOrWrapper: emuhawkOrWrapper, $"*LibretroNoGame*{coreBinaryFilePath}");
		}
		else
		{
			var romsSubdir = Path.Combine(romsDir, sysID);
			if (!Directory.Exists(romsSubdir)
				|| Directory.EnumerateFiles(romsSubdir, "*.*").ToArray() is not { Length: not 0 } romFilenames)
			{
				Console.Error.WriteLine($"No {sysID} roms available; skipping {coreID}\n{DIVIDER}");
				continue;
			}
			foreach (var romFilename in romFilenames)
			{
				Console.Error.WriteLine($"Launching {coreID} ({sysID}) with {romFilename}");
				LaunchEmuHawk(emuhawkOrWrapper: emuhawkOrWrapper, $"*Libretro*{{\"Path\":\"{romFilename}\",\"CorePath\":\"{coreBinaryFilePath}\"}}");
			}
		}
		var progressLine = $"{PromptForResult()}@{coreID}\n";
		File.AppendAllText(progressFilePath, contents: progressLine);
		Console.Error.WriteLine($"Recording {progressLine}{DIVIDER}");
	}
	Console.Error.WriteLine("All cores checked; collating results");
	var finalResults = File.ReadAllLines(progressFilePath)
		.Select(progressLine => progressLine.Split("@"))
		.Reverse().DistinctBy(fields => fields[1]) // take most recent if duplicate
		.ToDictionary(
			fields => fields[1],
			fields => fields[0] switch
			{
				"working" => "working",
				"broken" => "broken",
				_ => "unknown"
			});
	foreach (var k in Extensions.SysIDLookup.Keys) _ = finalResults.TryAdd(k, "unknown");
	var numUnknown = finalResults.Values.Count(v => v is "unknown");
	Console.Error.WriteLine($"{numUnknown} core(s) were skipped or explicitly marked unknown");
	var resultsFilePath = Path.Combine(resultsDir, isBaseline ? "baseline.json" : "dev.json");
	if (File.Exists(resultsFilePath)) Console.Error.WriteLine($"{resultsFilePath} will be overwritten (it's checked-in, so this shouldn't be a problem)");
	Console.Error.WriteLine("Launching EmuHawk once more so you can note the version/commit info (on older builds, you will have to check Help > About... manually)");
	LaunchEmuHawk(emuhawkOrWrapper: emuhawkOrWrapper, "--version");
	Console.Error.Write("Enter version: ");
	File.WriteAllText(resultsFilePath, contents: JsonSerializer.Serialize(
		new ResultsFile(Console.ReadLine() ?? string.Empty, new(finalResults)),
		JsonCustomContext.Default.ResultsFile).Replace("\\u002B", "+") + "\n");
	File.Delete(progressFilePath);
	Console.Error.WriteLine($"Wrote {(isBaseline ? "baseline" : "dev build")} results");
	return 0;
}

static bool LaunchEmuHawk(string emuhawkOrWrapper, string arg1)
{
	using Process proc = new()
	{
		StartInfo = new()
		{
			ArgumentList = { arg1 },
			CreateNoWindow = true,
			FileName = emuhawkOrWrapper,
			RedirectStandardError = false,
			RedirectStandardInput = false,
			RedirectStandardOutput = false,
			UseShellExecute = false,
		},
	};
	proc.Start();
	proc.WaitForExit();
	Console.Error.WriteLine("EmuHawk exited");
	return default;
}

static string PromptForResult()
{
	Console.Error.Write("How did that go [working/broken/unknown]? ");
	while (true)
	{
		var response = Console.ReadLine()?.Trim() ?? string.Empty;
		if (response is "working" or "broken" or "unknown") return response;
		Console.Error.Write("Respond with just \"working\", \"broken\", or \"unknown\" (or ^C to quit; all results up to now are already saved) ");
	}
}

internal static class Extensions
{
	extension(string str)
	{
		public string RemoveSuffix(string sfx)
			=> str.EndsWith(sfx) ? str[..^(sfx.Length)] : str;

		public string SubstringAfterLast(char c)
			=> str.Split(c).Last();
	}

	public const string NOGAME = "NoGame";

	public const string UNK = "UNK";

	public static IReadOnlyDictionary<string, string> SysIDLookup = new KeyValuePair<string, string>[] {
		new("2048", NOGAME),
		new("81", UNK),
		new("atari800", UNK),
		new("blastem", UNK),
		new("bluemsx", UNK),
		new("bsnes", "SNES"),
		new("bsnes_hd_beta", "SNES"),
		new("bsnes_mercury_accuracy", "SNES"),
		new("bsnes_mercury_balanced", "SNES"),
		new("bsnes_mercury_performance", "SNES"),
		new("citra", "3DS"),
		new("desmume", "NDS"),
		new("desmume2015", "NDS"),
		new("dolphin", "GameCube"),
		new("dosbox", UNK),
		new("dosbox_pure", UNK),
		new("easyrpg", UNK),
		new("fbalpha2012", UNK),
		new("fbneo", UNK),
		new("fceumm", UNK),
		new("flycast", "Dreamcast"),
		new("fmsx", UNK),
		new("freeintv", "INTV"),
		new("fuse", UNK),
		new("gambatte", "GB"),
		new("genesis_plus_gx", "GEN"),
		new("gpsp", UNK),
		new("gw", UNK),
		new("handy", "Lynx"),
		new("hatari", UNK),
		new("libblastem", UNK),
		new("libuv", UNK),
		new("mame2000", "Arcade"),
		new("mame2003", "Arcade"),
		new("mame2003_plus", "Arcade"),
		new("mame2010", "Arcade"),
		new("mame2015", "Arcade"),
		new("mednafen_gba", "GBA"),
		new("mednafen_lynx", "Lynx"),
		new("mednafen_ngp", "NGP"),
		new("mednafen_pce", "PCE"),
		new("mednafen_pce_fast", "PCE"),
		new("mednafen_pcfx", "PCFX"),
		new("mednafen_psx", "PSX"),
		new("mednafen_psx_hw", "PSX"),
		new("mednafen_saturn", "SAT"),
		new("mednafen_supafaust", "SNES"),
		new("mednafen_supergrafx", "SGX"),
		new("mednafen_vb", "VB"),
		new("mednafen_wswan", "WSWAN"),
		new("melonds", "NDS"),
		new("melondsds", "NDS"),
		new("mesen", "NES"),
		new("mesen-s", "SNES"),
		new("mesens", "SNES"),
		new("meteor", "GBA"),
		new("mgba", "GBA"),
		new("mrboom", UNK),
		new("mupen64plus_next", "N64"),
		new("neocd", "NeoGeoCD"),
		new("nestopia", "NES"),
		new("np2kai", UNK),
		new("nxengine", UNK),
		new("o2em", "O2"),
		new("opera", "3DO"),
		new("parallel_n64", "N64"),
		new("pcsx_rearmed", UNK),
		new("pcsx2", "PS2"),
		new("picodrive", "32X"),
		new("play", UNK),
		new("pokemini", UNK),
		new("ppsspp", "PSP"),
		new("prboom", UNK),
		new("prosystem", UNK),
		new("puae", "Amiga"),
		new("puae2021", "Amiga"),
		new("quicknes", "NES"),
		new("same_cdi", "PhillipsCDi"),
		new("sameboy", "GB"),
		new("scummvm", UNK),
		new("smsplus", "SMS"),
		new("snes9x", "SNES"),
		new("snes9x2002", "SNES"),
		new("snes9x2005", "SNES"),
		new("snes9x2005_plus", "SNES"),
		new("snes9x2010", "SNES"),
		new("stella", "A26"),
		new("stella2014", "A26"),
		new("swanstation", "WSWAN"),
		new("tgbdual", UNK),
		new("thepowdertoy", NOGAME),
		new("tic80", "TIC80"),
		new("vba_next", "GBA"),
		new("vbam", "GBA"),
		new("vecx", "VEC"),
		new("vice_x128", UNK),
		new("vice_x64", UNK),
		new("vice_x64dtv", UNK),
		new("vice_x64sc", UNK),
		new("vice_xcbm2", UNK),
		new("vice_xcbm5x0", UNK),
		new("vice_xpet", UNK),
		new("vice_xplus4", UNK),
		new("vice_xscpu64", UNK),
		new("vice_xvic", UNK),
		new("virtualjaguar", "Jaguar"),
		new("yabause", UNK),
	}.ToFrozenDictionary();
}

[JsonSerializable(typeof(ResultsFile), GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSourceGenerationOptions(IndentCharacter = '\t', IndentSize = 1, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
internal partial class JsonCustomContext : JsonSerializerContext {}

internal readonly record struct ResultsFile(string VersionInfo, SortedDictionary<string, string> Results);
