processors := $(call unique,$(processors))

objects += $(if $(findstring arm7tdmi,$(processors)),processor-arm7tdmi)
objects += $(if $(findstring gsu,$(processors)),processor-gsu)
objects += $(if $(findstring hg51b,$(processors)),processor-hg51b)
objects += $(if $(findstring sm83,$(processors)),processor-sm83)
objects += $(if $(findstring spc700,$(processors)),processor-spc700)
objects += $(if $(findstring upd96050,$(processors)),processor-upd96050)
objects += $(if $(findstring wdc65816,$(processors)),processor-wdc65816)

obj/processor-arm7tdmi.o: processor/arm7tdmi/arm7tdmi.cpp
obj/processor-gsu.o:      processor/gsu/gsu.cpp
obj/processor-hg51b.o:    processor/hg51b/hg51b.cpp
obj/processor-sm83.o:     processor/sm83/sm83.cpp
obj/processor-spc700.o:   processor/spc700/spc700.cpp
obj/processor-upd96050.o: processor/upd96050/upd96050.cpp
obj/processor-wdc65816.o: processor/wdc65816/wdc65816.cpp
