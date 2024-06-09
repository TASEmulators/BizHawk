$targetDir = "$PSScriptRoot/../.git/hooks"
if (Test-Path $targetDir -PathType Container) { # is Git repo
	$PSCommandFilename = Split-Path $PSCommandPath -Leaf
	foreach ($f in Get-ChildItem "$PSScriptRoot/git_hooks") {
		$target = Join-Path $targetDir (Split-Path $f -LeafBase)
		if (!(Test-Path $target -PathType Leaf)) { # target file doesn't exist
			echo "[$PSCommandFilename] creating Git hook $($f.Name)"
			Copy-Item "$PSScriptRoot/git_hook_shim.sh" $target
			#TODO use symlinks on Linux
		} elseif ($(Get-FileHash $target).Hash -ne $(Get-FileHash $f.FullName).Hash) { # files differ
			$head = Get-Content $target -TotalCount 3
			echo "[$PSCommandFilename] found existing Git hook $($f.Name), please resolve conflict manually"
			#TODO should REALLY make the scripts extensible then...
			exit 1
		}
		# else no-op
	}
}
