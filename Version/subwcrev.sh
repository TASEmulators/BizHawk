#!/bin/sh

if test -d ../.git ; then
	REV="`git svn info | grep Revision: | cut -d' ' -f2`";
else
	REV="`svn info | grep Revision: | cut -d' ' -f2`";
fi

sed -e 's/\$WCREV\$/'$REV'/g' "$1/svnrev_template" > "$1/svnrev.cs.tmp"
cmp -s "$1/svnrev.cs.tmp" "$1/svnrev.cs" || cp "$1/svnrev.cs.tmp" "$1/svnrev.cs"
