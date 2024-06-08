$targetDir = "$PSScriptRoot/../.git/hooks"
if (Test-Path $targetDir -PathType Container) { # is Git repo
	$PSCommandFilename = Split-Path $PSCommandPath -Leaf
	foreach ($f in Get-ChildItem "$PSScriptRoot/git_hook_shims") {
		$target = Join-Path $targetDir $f.Name
		if (!(Test-Path $target -PathType Leaf)) { # target file doesn't exist
			echo "[$PSCommandFilename] creating Git hook $($f.Name)"
			Copy-Item $f $target
			#TODO generate shim? the only difference between different shims would be the filename in the Batch part (and if there was an equivalent to `basename $0` then that would be the same too
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
