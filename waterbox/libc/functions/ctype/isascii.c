#include <ctype.h>

int isascii(int c)
{
	return (unsigned)c < 128;
}
