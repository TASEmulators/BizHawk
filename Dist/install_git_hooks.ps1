#!/usr/bin/env pwsh
$targetDir = "$PSScriptRoot/../.git/hooks"
if (Test-Path $targetDir -PathType Container) { # is Git repo
	$PSCommandFilename = Split-Path $PSCommandPath -Leaf
	$shimChecksum = (Get-FileHash "$PSScriptRoot/git_hook_shim.sh").Hash
	foreach ($f in Get-ChildItem "$PSScriptRoot/git_hooks") {
		$target = Join-Path $targetDir (Split-Path $f -LeafBase)
		if (!(Test-Path $target -PathType Leaf)) { # target file doesn't exist
			echo "[$PSCommandFilename] creating Git hook $($f.Name)"
			Copy-Item "$PSScriptRoot/git_hook_shim.sh" $target
			#TODO use symlinks on Linux
		} elseif ((Get-FileHash $target).Hash -ne $shimChecksum) { # files differ
			$head = Get-Content $target -TotalCount 3
			echo "[$PSCommandFilename] found existing Git hook $($f.Name), please resolve conflict manually"
			exit 1
		}
		# else no-op
	}
}
