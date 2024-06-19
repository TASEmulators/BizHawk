#!/usr/bin/env -S pwsh
$msg = Get-Content $args[0] -TotalCount 1 # this commit hook is always passed the commit message scratch file's path, so read the first line of that
if ($msg -Match "^fix(?:ed|es)? #\d+$") {
	echo "An issue reference alone is not a suitable commit message. Vetoed."
	exit 1
}
if ($msg.Length -lt 20) { # arbitrary
	if ($msg.Length -lt 8) { # semi-arbitrary; I figured "Fix typo" would be the shortest reasonable message --yoshi
		echo "Your commit message is too short. Vetoed."
		exit 1
	}
	echo "Your commit message is a bit short. Do you have more to add? (If you included a longer description already, ignore this.)"
}
