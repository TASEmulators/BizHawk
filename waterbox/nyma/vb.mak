include common.mak

SRCS += \
	$(filter-out %debug.cpp,$(call cppdir,vb)) \
	$(call cppdir,hw_cpu/v810) \
	cdrom_dummy.cpp \
	vb.cpp

include ../common.mak
