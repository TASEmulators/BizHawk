{ lib
# infrastructure
, writeShellScript
, writeText
# rundeps
, bizhawkAssemblies
, mkfifo
, mktemp
, nixGL
# other parameters
, debugPInvokes
, debugDotnetHostCrashes
, initConfig # pretend this is JSON; the following env. vars will be substituted by the wrapper script (if surrounded by double-percent e.g. `%%BIZHAWK_DATA_HOME%%`): `BIZHAWK_DATA_HOME`
, isManualLocalBuild # i.e. dotnet build in nix-shell; skips everything involving BIZHAWK_DATA_HOME, such as copying Assets and initConfig
}: let
	hawkVersion = bizhawkAssemblies.hawkSourceInfo.version;
	commentLineIf = b: lib.optionalString b "# ";
	/**
	 * shell script to copy std{out,err} to files but leave them going to console;
	 * call as `redirect-output-to-files /target/stdout.txt /target/stderr.txt /path/to/bin arg1 arg2...`
	 */
	redirectOutputToFiles = writeShellScript "redirect-output-to-files" ''
		mktemp='${lib.getBin mktemp}/bin/mktemp'
		o="$("$mktemp" -u)"
		e="$("$mktemp" -u)"
		'${lib.getBin mkfifo}/bin/mkfifo' "$o" "$e"
		tee "$1" <"$o" &
		shift
		tee "$1" <"$e" | sed "s/.*/$(tput setaf 1)&$(tput sgr0)/" >&2 &
		shift
		exec "$@" >"$o" 2>"$e"
	'';
	emuhawk = let
		initConfigFile = writeText "config.json" (builtins.toJSON ({
			LastWrittenFrom = if builtins.length (builtins.splitVersion hawkVersion) < 3 then "${hawkVersion}.0" else hawkVersion; # 2.x.0 never included the trailing 0 in display name, and didn't include it in the internal value after 2.5(?); need to do version range check here
			PathEntries = {
				Paths = [
					({ System = "Global_NULL"; Type = "Base"; Path = "%%BIZHAWK_DATA_HOME%%"; }
						// lib.optionalAttrs bizhawkAssemblies.hawkSourceInfo.pathConfigNeedsOrdinal { Ordinal = 1; })
				];
			};
		} // initConfig));
		assetManagementScript = ''if [ "$XDG_DATA_HOME" ]; then
			BIZHAWK_DATA_HOME="$XDG_DATA_HOME"
		else
			BIZHAWK_DATA_HOME="$HOME/.local/share"
		fi
		export BIZHAWK_DATA_HOME="$BIZHAWK_DATA_HOME/emuhawk-monort-${hawkVersion}"
		if [ ! -e "$BIZHAWK_DATA_HOME" ]; then
			mkdir -p "$BIZHAWK_DATA_HOME"
			cd '${bizhawkAssemblies.assets}'
			find . -type f -not -wholename './nix-support/*' -exec install -DvT {} "$BIZHAWK_DATA_HOME/{}" \;
		fi
		cd "$BIZHAWK_DATA_HOME"
		if [ ! -e 'config.json' ]; then
			sed "s@%%BIZHAWK_DATA_HOME%%@$PWD@g" '${initConfigFile}' >config.json
			printf "wrote initial config to %s\n" "$PWD/config.json"
		fi

		export BIZHAWK_INT_SYSLIB_PATH='${bizhawkAssemblies.extraUnmanagedDeps}/lib'
		'';
		noAssetManagementScript = ''cd "$BIZHAWK_HOME"

		export BIZHAWK_INT_SYSLIB_PATH="$PWD/dll"
		'';
	in writeShellScript "emuhawk-mono-wrapper" ''
		set -e

		if [ ! -e "$BIZHAWK_HOME/EmuHawk.exe" ]; then
			printf "no such file: %s\n" "$BIZHAWK_HOME/EmuHawk.exe"
			exit 1
		fi

		${if isManualLocalBuild then noAssetManagementScript else assetManagementScript}
		ldLibPath="$BIZHAWK_INT_SYSLIB_PATH:${lib.makeLibraryPath bizhawkAssemblies.buildInputs}"
		if [ -z "$LD_LIBRARY_PATH" ]; then
			export LD_LIBRARY_PATH="$ldLibPath"
		else
			export LD_LIBRARY_PATH="$LD_LIBRARY_PATH:$ldLibPath"
		fi
		${if bizhawkAssemblies.hawkSourceInfo.hasAssemblyResolveHandler then "" else ''export MONO_PATH="$BIZHAWK_HOME/dll/nlua:$BIZHAWK_HOME/dll"
		''}${lib.optionalString (!debugPInvokes) "# "}export MONO_LOG_LEVEL=debug MONO_LOG_MASK=dll # pass `--arg debugPInvokes true` to nix-build to enable
		${lib.optionalString debugDotnetHostCrashes "# "}export MONO_CRASH_NOFILE=1 # pass `--arg debugDotnetHostCrashes true` to nix-build to disable
		if [ "$1" = '--mono-no-redirect' ]; then
			printf "(passing --mono-no-redirect is no longer necessary)\n" >&2
			shift
		fi
		printf "(capturing output in %s/EmuHawkMono_last*.txt)\n" "$PWD" >&2
		exec '${redirectOutputToFiles}' EmuHawkMono_laststdout.txt EmuHawkMono_laststderr.txt \
			'${lib.getBin bizhawkAssemblies.mono}/bin/mono' \
				"$BIZHAWK_HOME/EmuHawk.exe" --config=config.json "$@"
	'';
in {
	inherit emuhawk;
	discohawk = writeShellScript "discohawk-mono-wrapper" ''
		set -e

		if [ ! -e "$BIZHAWK_HOME/DiscoHawk.exe" ]; then
			printf "no such file: %s\n" "$BIZHAWK_HOME/DiscoHawk.exe"
			exit 1
		fi

		if [ "$XDG_DATA_HOME" ]; then
			BIZHAWK_DATA_HOME="$XDG_DATA_HOME"
		else
			BIZHAWK_DATA_HOME="$HOME/.local/share"
		fi
		export BIZHAWK_DATA_HOME="$BIZHAWK_DATA_HOME/emuhawk-monort-${hawkVersion}"
		cd "$BIZHAWK_DATA_HOME"

		export MONO_PATH="$BIZHAWK_HOME/dll"
		${lib.optionalString (!debugPInvokes) "# "}export MONO_LOG_LEVEL=debug MONO_LOG_MASK=dll # pass `--arg debugPInvokes true` to nix-build to enable
		exec '${lib.getBin bizhawkAssemblies.mono}/bin/mono' "$BIZHAWK_HOME/DiscoHawk.exe" "$@"
	'';
	emuhawkNonNixOS = writeShellScript "emuhawk-mono-wrapper-non-nixos" ''exec '${nixGL}/bin/nixGL' '${emuhawk}' "$@"'';
}
