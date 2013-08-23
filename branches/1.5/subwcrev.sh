#!/bin/sh

cd "`dirname "$0"`"

if test -d .git ; then
        REV="`git svn info | grep Revision: | cut -d' ' -f2`"
else
        REV="`svn info | grep Revision: | cut -d' ' -f2`"
fi

sed -e 's/\$WCREV\$/'$REV'/g' "$1/Properties/svnrev_template" > "$1/Properties/svnrev.cs.tmp"
cmp -s "$1/Properties/svnrev.cs.tmp" "$1/Properties/svnrev.cs" || cp "$1/Properties/svnrev.cs.tmp" "$1/Properties/svnrev.cs"
