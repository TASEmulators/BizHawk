all: demo demo_chip demo_fixed demo_stereo demo.c demo_chip.c demo_fixed.c demo_stereo.c blip_buf.c

demo: demo.c blip_buf.c
	$(CC) -o demo demo.c blip_buf.c wave_writer.c

demo_chip: demo_chip.c blip_buf.c
	$(CC) -o demo_chip demo_chip.c blip_buf.c wave_writer.c

demo_fixed: demo_fixed.c blip_buf.c
	$(CC) -o demo_fixed demo_fixed.c blip_buf.c wave_writer.c

demo_stereo: demo_stereo.c blip_buf.c
	$(CC) -o demo_stereo demo_stereo.c blip_buf.c wave_writer.c

demo_sdl: demo_sdl.c blip_buf.c
	$(CC) -o demo_sdl $(shell sdl-config --cflags) $(shell sdl-config --libs) demo_sdl.c blip_buf.c

test: blip_buf.c blip_buf.h
	$(CXX) -o test -DBLARGG_TEST -I . -I tests/tester blip_buf.c tests/*.cpp tests/tester/*.cpp

clean:
	-$(RM) demo demo_chip demo_fixed demo_stereo demo_sdl
