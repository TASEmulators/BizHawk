CC = gcc

CFLAGS = -Wall -Wextra -O3
LFLAGS = -shared

all:	blip_buf

blip_buf: blip_buf.c blip_buf.h
	$(CC) $(CFLAGS) blip_buf.c -o blip_buf.dll $(LFLAGS)
