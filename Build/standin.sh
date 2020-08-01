#!/bin/sh
cd "$(dirname "$0")/.." && printf "internal static class SubWCRev\n{\n\tpublic const string SVN_REV = \"%s\";\n\tpublic const string GIT_BRANCH = \"%s\";\n\tpublic const string GIT_SHORTHASH = \"%s\";\n}" "$(git rev-list HEAD --count)" "$(git rev-parse --abbrev-ref HEAD)" "$(git log -1 --format="%h")" >"$1"
