$targetDir = "$PSScriptRoot/../.git/hooks"
if (Test-Path $targetDir -PathType Container) { # is Git repo
	$magicHeader = '# placed here by BizHawk build scripts and may be updated automatically'
	$PSCommandFilename = Split-Path $PSCommandPath -Leaf
	foreach ($f in Get-ChildItem "$PSScriptRoot/git_hooks") {
		$target = Join-Path $targetDir $f.Name
		if (Test-Path $target -PathType Leaf) { # target file exists
			if ($(Get-FileHash $target).Hash -ne $(Get-FileHash $f.FullName).Hash) { # files differ
				$head = Get-Content $target -TotalCount 3
				if ($magicHeader -in $head) {
					echo "[$PSCommandFilename] updating existing Git hook $($f.Name)"
					Copy-Item $f $target
				} else {
					echo "[$PSCommandFilename] found existing Git hook $($f.Name), please resolve conflict manually"
					# should probably make the scripts extensible then...
					exit 1
				}
			}
		} else {
			echo "[$PSCommandFilename] creating Git hook $($f.Name)"
			Copy-Item $f $target
		}
	}
}
