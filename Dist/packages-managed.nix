{ lib, buildDotnetModule, runCommand, hawkSourceInfo }: let
	grabFromReferences = { filePfx, subdir ? "" }: runCommand filePfx {
		inherit (hawkSourceInfo) __contentAddressed;
		meta.sourceProvenance = [ lib.sourceTypes.binaryBytecode ];
	} ''mkdir -p "$out${subdir}"; cp -vt "$out${subdir}" '${hawkSourceInfo.src}/References${subdir}/${filePfx}'*'';
in { #TODO build all but flatBuffersCore, gongShell, slimDX, and systemDataSqliteDropIn from source
	bizhawkAnalyzer = grabFromReferences { filePfx = "BizHawk.Analyzer"; };
	flatBuffersCore = grabFromReferences { filePfx = "FlatBuffers.Core"; };
	flatBuffersGenOutput = grabFromReferences { filePfx = "FlatBuffers.GenOutput"; };
	gongShell = grabFromReferences { filePfx = "GongShell"; };
	hawkQuantizer = grabFromReferences { filePfx = "PcxFileTypePlugin.HawkQuantizer"; };
	isoParser = grabFromReferences { filePfx = "ISOParser"; };
	nlua = grabFromReferences { filePfx = "NLua"; };
	slimDX = grabFromReferences { filePfx = "SlimDX"; subdir = "/x64"; };
	srcGenReflectionCache = grabFromReferences { filePfx = "BizHawk.SrcGen.ReflectionCache"; };
	srcGenSettingsUtil = grabFromReferences { filePfx = "BizHawk.SrcGen.SettingsUtil"; };
	srcGenVersionInfo = grabFromReferences { filePfx = "BizHawk.SrcGen.VersionInfo"; };
	systemDataSqliteDropIn = grabFromReferences { filePfx = "System.Data.SQLite"; subdir = "/x64/SQLite"; };
	virtu = grabFromReferences { filePfx = "Virtu"; };
}
