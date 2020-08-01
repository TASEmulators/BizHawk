#!/bin/sh
sed -e "s;\\\$WCREV\\\$;$(git rev-list HEAD --count);" -e "s;\\\$WCBRANCH\\\$;$(git rev-parse --abbrev-ref HEAD);" -e "s;\\\$WCSHORTHASH\\\$;$(git log -1 --format="%h");" "$1" >"$2"
