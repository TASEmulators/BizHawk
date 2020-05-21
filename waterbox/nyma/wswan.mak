include common.mak

SRCS += \
	$(filter-out %debug.cpp,$(call cppdir,wswan))

include ../common.mak
