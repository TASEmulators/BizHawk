include common.mak

SRCS += \
	$(call cppdir,ngp) \
	$(call cppdir,hw_cpu/z80-fuse) \
	ngp.cpp

include ../common.mak
