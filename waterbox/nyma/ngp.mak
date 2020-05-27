include common.mak

SRCS += \
	$(call cppdir,ngp) \
	$(call cppdir,hw_cpu/z80-fuse) \
	cdrom_dummy.cpp \
	ngp.cpp

include ../common.mak
