{ lib ? (import <nixpkgs> {}).lib
}: let
	/** returns negation of needle's length iff not found */
	indexOf = needle: haystack: let
		len = lib.stringLength needle;
		naiveImpl = i: if lib.substring i len haystack == needle then i else naiveImpl (i + 1);
		cleverImpl = lib.converge
			(i: let
				substr = lib.substring i len haystack;
			in if lib.strings.levenshteinAtMost (len - 1) needle substr
				then if needle == substr then i else i + 1
				else i + len)
			0;
	in if lib.hasInfix needle haystack
		then if len < 85000000 then naiveImpl 0 else cleverImpl # from testing on a ~1600 KLOC file, cleverImpl starts being faster around this point (presumably because it makes fewer allocations)--though it's still only ~600 ms execution time
		else -len;
	/** returns haystack iff not found */
	substringAfter = needle: haystack: lib.substring
		((indexOf needle haystack) + (lib.stringLength needle))
		(lib.stringLength haystack)
		haystack;
	/** returns empty string iff not found */
	substringBefore = needle: haystack: lib.substring 0 (indexOf needle haystack) haystack;
	/**
	 * Filters a list `l` by evaluating a predicate on each element and its index (0-based).
	 * The predicate callback `f` has same parameters as for `imap0`, but should return a bool,
	 * `true` iff the value should be included in the resulting list.
	 * The resulting list only includes the filtered-for elements, not their indices.
	 */
	ifilter0 = f: l: lib.pipe l [
		(lib.imap0 (i: v: { inherit i v; }))
		(lib.filter (a: f a.i a.v))
		(lib.catAttrs "v")
	];
	ac = lib.attrNames (import ./.. {}); # could also check shell.nix, but that's completely programmatic and a strict subset of default.nix so there's not much point
	ex = lib.pipe (lib.readFile ./nix_expr_usage_docs.md) [
		(substringAfter "MARKER_FOR_HELPER_SCRIPT_START")
		(substringBefore "MARKER_FOR_HELPER_SCRIPT_END")
		(lib.splitString "`")
		(ifilter0 (i: _: lib.mod i 2 == 1))
	];
in { missing = lib.subtractLists ac ex; extra = lib.subtractLists ex ac; }
