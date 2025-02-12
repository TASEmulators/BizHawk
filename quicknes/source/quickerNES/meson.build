# quickerNES Core sources

quickerNESAPUSrc = [
 'core/apu/apu.cpp', 
 'core/apu/oscs.cpp', 
 'core/apu/buffer.cpp', 
 'core/apu/blipBuffer.cpp', 
 'core/apu/NESEffectsBuffer.cpp', 
 'core/apu/effectsBuffer.cpp', 
 'core/apu/multiBuffer.cpp',
 'core/apu/namco/apu_namco.cpp', 
 'core/apu/vrc6/apu_vrc6.cpp', 
 'core/apu/vrc7/emu2413.cpp', 
 'core/apu/vrc7/emu2413_state.cpp', 
 'core/apu/vrc7/apu_vrc7.cpp',
 'core/apu/fme7/apu_fme7.cpp', 
]

quickerNESPPUSrc = [
 'core/ppu/ppu.cpp', 
 'core/ppu/ppuImpl.cpp', 
 'core/ppu/ppuRendering.cpp',
]

quickerNESSrc = quickerNESAPUSrc + quickerNESPPUSrc + [
 'core/mappers/mapper.cpp', 
 'core/emu.cpp', 
 'core/cpu.cpp'
]

quickerNESCompileArgs = [ ]

# Checking for arkanoid input support
if get_option('enableArkanoidInputs') == true
 quickerNESCompileArgs += '-D_QUICKERNES_SUPPORT_ARKANOID_INPUTS'
endif

# quickerNES Core Configuration

 quickerNESDependency = declare_dependency(
  compile_args        : quickerNESCompileArgs,
  include_directories : include_directories(['.', '..']),
  sources             : [ quickerNESSrc ],
  dependencies        : [ 
						  																		dependency('sdl2'),
		                      ]
 )