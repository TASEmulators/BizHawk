This directory holds various "internals" of PDCLib:

- definitions of helper functions not specified by the standard (hidden in the
  _PDCLIB_* namespace);

- definitions of data objects, both internal (like digits.c) and specified by
  the standard (errno.c);

- test drivers for functionality that does not have its own implementation
  file to put the test driver in.

