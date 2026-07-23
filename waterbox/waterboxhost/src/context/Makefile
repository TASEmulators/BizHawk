all: interop.bin interop_macos.bin

interop.bin: interop.s
	nasm -f bin -Wall -o $@ $<

interop_macos.bin: interop_macos.s
	nasm -f bin -Wall -o $@ $<
