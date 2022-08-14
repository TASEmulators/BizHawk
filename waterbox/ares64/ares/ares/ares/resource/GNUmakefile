build := stable
nall.path := ../../../nall
include $(nall.path)/GNUmakefile

all:
	sourcery resource.bml resource.cpp resource.hpp

clean:
	$(call delete,resource.cpp)
	$(call delete,resource.hpp)
