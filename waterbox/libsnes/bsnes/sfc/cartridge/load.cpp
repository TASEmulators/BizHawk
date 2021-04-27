auto Cartridge::loadBoard(string board) -> Markup::Node {
  string output;

  if(board.beginsWith("SNSP-")) board.replace("SNSP-", "SHVC-", 1L);
  if(board.beginsWith("MAXI-")) board.replace("MAXI-", "SHVC-", 1L);
  if(board.beginsWith("MJSC-")) board.replace("MJSC-", "SHVC-", 1L);
  if(board.beginsWith("EA-"  )) board.replace("EA-",   "SHVC-", 1L);
  if(board.beginsWith("WEI-" )) board.replace("WEI-",  "SHVC-", 1L);

	// const char* constant_boards_bml_file_hardcoded = "database\n  revision: 2020-06-07\n\n//Boards (Production)\n\ndatabase\n  revision: 2020-01-21\n\nboard: BANDAI-PT-923\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff mask=0x8000\n  slot type=SufamiTurbo\n    rom\n      map address=20-3f,a0-bf:8000-ffff mask=0x8000\n    ram\n      map address=60-6f,e0-ef:0000-ffff\n  slot type=SufamiTurbo\n    rom\n      map address=40-5f,c0-df:0000-ffff mask=0x8000\n    ram\n      map address=70-7d,f0-ff:0000-ffff\n\nboard: BSC-1A5B9P-01\n  memory type=RAM content=Save\n    map address=10-17:5000-5fff mask=0xf000\n  processor identifier=MCC\n    map address=00-0f:5000-5fff\n    mcu\n      map address=00-3f,80-bf:8000-ffff\n      map address=40-7d,c0-ff:0000-ffff\n      map address=20-3f,a0-bf:6000-7fff\n      memory type=ROM content=Program\n      memory type=RAM content=Download\n      slot type=BSMemory\n\nboard: BSC-1A5M-02\n  memory type=ROM content=Program\n    map address=00-1f:8000-ffff mask=0xe08000 base=0x000000\n    map address=20-3f:8000-ffff mask=0xe08000 base=0x100000\n    map address=80-9f:8000-ffff mask=0xe08000 base=0x200000\n    map address=a0-bf:8000-ffff mask=0xe08000 base=0x100000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n  slot type=BSMemory\n    map address=c0-ef:0000-ffff\n\nboard: BSC-1A7M-(01,10)\n  memory type=ROM content=Program\n    map address=00-1f:8000-ffff mask=0xe08000 base=0x000000\n    map address=20-3f:8000-ffff mask=0xe08000 base=0x100000\n    map address=80-9f:8000-ffff mask=0xe08000 base=0x200000\n    map address=a0-bf:8000-ffff mask=0xe08000 base=0x100000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n  slot type=BSMemory\n    map address=c0-ef:0000-ffff\n\nboard: BSC-1J3M-01\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff\n    map address=40-5f,c0-df:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n  slot type=BSMemory\n    map address=20-3f,a0-bf:8000-ffff\n    map address=60-7d,e0-ff:0000-ffff\n\nboard: BSC-1J5M-01\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff\n    map address=40-5f,c0-df:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n  slot type=BSMemory\n    map address=20-3f,a0-bf:8000-ffff\n    map address=60-7d,e0-ff:0000-ffff\n\nboard: BSC-1L3B-01\n  processor architecture=W65C816S\n    map address=00-3f,80-bf:2200-23ff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x408000\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n      slot type=BSMemory\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=40-4f:0000-ffff\n    memory type=RAM content=Internal\n      map address=00-3f,80-bf:3000-37ff size=0x800\n\nboard: BSC-1L5B-01\n  processor architecture=W65C816S\n    map address=00-3f,80-bf:2200-23ff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x408000\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n      slot type=BSMemory\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=40-4f:0000-ffff\n    memory type=RAM content=Internal\n      map address=00-3f,80-bf:3000-37ff size=0x800\n\nboard: SGB-R-10\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-7d,c0-ff:0000-7fff mask=0x8000\n  processor identifier=ICD revision=2\n    map address=00-3f,80-bf:6000-67ff,7000-7fff\n    memory type=ROM content=Boot architecture=SM83\n    slot type=GameBoy\n\nboard: SHVC-1A0N-(01,02,10,20,30)\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-7d,c0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-1A1B-(04,05,06)\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-ffff\n\nboard: SHVC-1A1M-(01,10,11,20)\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-1A3B-(11,12,13)\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-ffff\n\nboard: SHVC-1A3B-20\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-1A3M-(10,20,21,30)\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-1A5B-(02,04)\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-ffff\n\nboard: SHVC-1A5M-(01,11,20)\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-1B0N-(02,03,10)\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff mask=0x8000\n  processor architecture=uPD7725\n    map address=30-3f,b0-bf:8000-ffff mask=0x3fff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: SHVC-1B5B-02\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-ffff\n  processor architecture=uPD7725\n    map address=20-3f,a0-bf:8000-ffff mask=0x3fff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: SHVC-1C0N\n  processor architecture=GSU\n    map address=00-3f,80-bf:3000-34ff\n    memory type=ROM content=Program\n      map address=00-1f,80-9f:8000-ffff mask=0x8000\n    memory type=RAM content=Save\n      map address=60-7d,e0-ff:0000-ffff\n\nboard: SHVC-1C0N5S-01\n  processor architecture=GSU\n    map address=00-3f,80-bf:3000-34ff\n    memory type=ROM content=Program\n      map address=00-1f,80-9f:8000-ffff mask=0x8000\n    memory type=RAM content=Save\n      map address=60-7d,e0-ff:0000-ffff\n\nboard: SHVC-1CA0N5S-01\n  processor architecture=GSU\n    map address=00-3f,80-bf:3000-34ff\n    memory type=ROM content=Program\n      map address=00-3f,80-bf:8000-ffff mask=0x8000\n      map address=40-5f,c0-df:0000-ffff\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=70-71,f0-f1:0000-ffff\n\nboard: SHVC-1CA0N6S-01\n  processor architecture=GSU\n    map address=00-3f,80-bf:3000-34ff\n    memory type=ROM content=Program\n      map address=00-3f,80-bf:8000-ffff mask=0x8000\n      map address=40-5f,c0-df:0000-ffff\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=70-71,f0-f1:0000-ffff\n\nboard: SHVC-1CA6B-01\n  processor architecture=GSU\n    map address=00-3f,80-bf:3000-34ff\n    memory type=ROM content=Program\n      map address=00-3f,80-bf:8000-ffff mask=0x8000\n      map address=40-5f,c0-df:0000-ffff\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=70-71,f0-f1:0000-ffff\n\nboard: SHVC-1CB0N7S-01\n  processor architecture=GSU\n    map address=00-3f,80-bf:3000-34ff\n    memory type=ROM content=Program\n      map address=00-3f:8000-ffff mask=0x8000\n      map address=40-5f:0000-ffff\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=70-71:0000-ffff\n\nboard: SHVC-1CB5B-(01,20)\n  processor architecture=GSU\n    map address=00-3f,80-bf:3000-34ff\n    memory type=ROM content=Program\n      map address=00-3f:8000-ffff mask=0x8000\n      map address=40-5f:0000-ffff\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=70-71:0000-ffff\n\nboard: SHVC-1CB7B-01\n  processor architecture=GSU\n    map address=00-3f,80-bf:3000-34ff\n    memory type=ROM content=Program\n      map address=00-3f:8000-ffff mask=0x8000\n      map address=40-5f:0000-ffff\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=70-71:0000-ffff\n\nboard: SHVC-1DC0N-01\n  processor architecture=HG51BS169\n    map address=00-3f,80-bf:6c00-6fff,7c00-7fff\n    memory type=ROM content=Program\n      map address=00-3f,80-bf:8000-ffff mask=0x8000\n    memory type=RAM content=Save\n      map address=70-77:0000-7fff\n    memory type=ROM content=Data architecture=HG51BS169\n    memory type=RAM content=Data architecture=HG51BS169\n      map address=00-3f,80-bf:6000-6bff,7000-7bff mask=0xf000\n    oscillator\n\nboard: SHVC-1DS0B-20\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  processor architecture=uPD96050\n    map address=60-67,e0-e7:0000-3fff\n    memory type=ROM content=Program architecture=uPD96050\n    memory type=ROM content=Data architecture=uPD96050\n    memory type=RAM content=Data architecture=uPD96050\n      map address=68-6f,e8-ef:0000-7fff mask=0x8000\n    oscillator\n\nboard: SHVC-1J0N-(01,10,20)\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n\nboard: SHVC-1J1M-(11,20)\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n\nboard: SHVC-1J3B-01\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n\nboard: SHVC-1J3M-(01,11,20)\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n\nboard: SHVC-1J5M-(01,11,20)\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n\nboard: SHVC-1K0N-01\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  processor architecture=uPD7725\n    map address=00-1f,80-9f:6000-7fff mask=0xfff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: SHVC-1K1B-01\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n  processor architecture=uPD7725\n    map address=00-1f,80-9f:6000-7fff mask=0xfff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: SHVC-1K1X-10\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n  processor architecture=uPD7725\n    map address=00-1f,80-9f:6000-7fff mask=0xfff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: SHVC-1L0N3S-01\n  processor architecture=W65C816S\n    map address=00-3f,80-bf:2200-23ff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x408000\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=40-4f:0000-ffff\n    memory type=RAM content=Internal\n      map address=00-3f,80-bf:3000-37ff size=0x800\n\nboard: SHVC-1L3B-(02,11)\n  processor architecture=W65C816S\n    map address=00-3f,80-bf:2200-23ff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x408000\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=40-4f:0000-ffff\n    memory type=RAM content=Internal\n      map address=00-3f,80-bf:3000-37ff size=0x800\n\nboard: SHVC-1L5B-(11,20)\n  processor architecture=W65C816S\n    map address=00-3f,80-bf:2200-23ff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x408000\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=40-4f:0000-ffff\n    memory type=RAM content=Internal\n      map address=00-3f,80-bf:3000-37ff size=0x800\n\nboard: SHVC-1N0N-(01,10)\n  processor identifier=SDD1\n    map address=00-3f,80-bf:4800-480f\n    mcu\n      map address=00-3f,80-bf:8000-ffff\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n\nboard: SHVC-2A0N-01#A\n  memory type=ROM content=Program\n    map address=00-2f,80-af:8000-ffff mask=0x8000\n    map address=40-6f,c0-ef:0000-ffff mask=0x8000\n\nboard: SHVC-2A0N-(01,10,11,20)\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-7d,c0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-2A1M-01\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-2A3B-01\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-2A3M-01#A\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-2A3M-(01,11,20)\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-2A5M-01\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-2B3B-01\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n  processor architecture=uPD7725\n    map address=60-6f,e0-ef:0000-7fff mask=0x3fff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: SHVC-2DC0N-01\n  processor architecture=HG51BS169\n    map address=00-3f,80-bf:6c00-6fff,7c00-7fff\n    memory type=ROM content=Program\n      map address=00-3f,80-bf:8000-ffff mask=0x8000\n    memory type=RAM content=Save\n      map address=70-77:0000-7fff\n    memory type=ROM content=Data architecture=HG51BS169\n    memory type=RAM content=Data architecture=HG51BS169\n      map address=00-3f,80-bf:6000-6bff,7000-7bff mask=0xf000\n    oscillator\n\nboard: SHVC-2E3M-01\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff mask=0x8000\n  processor identifier=OBC1\n    map address=00-3f,80-bf:6000-7fff mask=0xe000\n    map address=70-71,f0-f1:6000-7fff,e000-ffff mask=0xe000\n    memory type=RAM content=Save\n\nboard: SHVC-2J0N-(01,10,11,20)\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n\nboard: SHVC-2J3M-(01,11,20)\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=10-1f,30-3f,90-9f,b0-bf:6000-7fff mask=0xe000\n\nboard: SHVC-2J5M-01\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=10-1f,30-3f,90-9f,b0-bf:6000-7fff mask=0xe000\n\nboard: SHVC-3J0N-01\n  memory type=ROM content=Program\n    map address=00-2f,80-af:8000-ffff\n    map address=40-6f,c0-ef:0000-ffff\n\nboard: SHVC-BA0N-(01,10)\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-7d,c0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-BA1M-01\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-BA3M-(01,10)\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-BJ0N-(01,20)\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n\nboard: SHVC-BJ1M-(10,20)\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n\nboard: SHVC-BJ3M-(10,20)\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n\nboard: SHVC-LDH3C-01\n  processor identifier=SPC7110\n    map address=00-3f,80-bf:4800-483f\n    map address=50,58:0000-ffff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x800000\n      map address=c0-ff:0000-ffff mask=0xc00000\n      memory type=ROM content=Program\n      memory type=ROM content=Data\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff mask=0xe000\n  rtc manufacturer=Epson\n    map address=00-3f,80-bf:4840-4842\n    memory type=RTC content=Time manufacturer=Epson\n\nboard: SHVC-LJ3M-01\n  memory type=ROM content=Program\n    map address=00-3f:8000-ffff base=0x400000\n    map address=40-7d:0000-ffff base=0x400000\n    map address=80-bf:8000-ffff mask=0xc00000\n    map address=c0-ff:0000-ffff mask=0xc00000\n  memory type=RAM content=Save\n    map address=80-bf:6000-7fff mask=0xe000\n\nboard: SHVC-LN3B-01\n  memory type=RAM content=Save\n    map address=00-3f,80-bf:6000-7fff mask=0xe000\n    map address=70-73:0000-ffff\n  processor identifier=SDD1\n    map address=00-3f,80-bf:4800-480f\n    mcu\n      map address=00-3f,80-bf:8000-ffff\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n\nboard: SHVC-SGB2-01\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-7d,c0-ff:0000-7fff mask=0x8000\n  processor identifier=ICD revision=2\n    map address=00-3f,80-bf:6000-67ff,7000-7fff\n    memory type=ROM content=Boot architecture=SM83\n    oscillator\n    slot type=GameBoy\n\nboard: SHVC-YA0N-01\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-7d,c0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-YJ0N-01\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n\n//Boards (Prototypes)\n\nboard: SHVC-2P3B-01\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-7d,c0-ff:0000-7fff mask=0x8000\n\nboard: SHVC-4PV5B-01\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-7d,c0-ff:0000-7fff mask=0x8000\n\n//Boards (Generic)\n\ndatabase\n  revision: 2020-06-07\n\nboard: ARM-LOROM-RAM\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-6f,c0-ef:0000-7fff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-ffff\n  processor architecture=ARM6\n    map address=00-3f,80-bf:3800-38ff\n    memory type=ROM content=Program architecture=ARM6\n    memory type=ROM content=Data architecture=ARM6\n    memory type=RAM content=Data architecture=ARM6\n    oscillator\n\nboard: BS-HIROM-RAM\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff\n    map address=40-5f,c0-df:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n  slot type=BSMemory\n    map address=20-3f,a0-bf:8000-ffff\n    map address=60-7d,e0-ff:0000-ffff\n\nboard: BS-LOROM-RAM\n  memory type=ROM content=Program\n    map address=00-1f:8000-ffff mask=0xe08000 base=0x000000\n    map address=20-3f:8000-ffff mask=0xe08000 base=0x100000\n    map address=80-9f:8000-ffff mask=0xe08000 base=0x200000\n    map address=a0-bf:8000-ffff mask=0xe08000 base=0x100000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n  slot type=BSMemory\n    map address=c0-ef:0000-ffff\n\nboard: BS-MCC-RAM\n  memory type=RAM content=Save\n    map address=10-17:5000-5fff mask=0xf000\n  processor identifier=MCC\n    map address=00-0f:5000-5fff\n    mcu\n      map address=00-3f,80-bf:8000-ffff\n      map address=40-7d,c0-ff:0000-ffff\n      map address=20-3f,a0-bf:6000-7fff\n      memory type=ROM content=Program\n      memory type=RAM content=Download\n      slot type=BSMemory\n\nboard: BS-SA1-RAM\n  processor architecture=W65C816S\n    map address=00-3f,80-bf:2200-23ff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x408000\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n      slot type=BSMemory\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=40-4f:0000-ffff\n    memory type=RAM content=Internal\n      map address=00-3f,80-bf:3000-37ff size=0x800\n\nboard: EVENT-CC92\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n  processor manufacturer=NEC architecture=uPD78214\n    identifier: Campus Challenge '92\n    map address=c0,e0:0000\n    mcu\n      map address=00-1f,80-9f:8000-ffff\n      memory type=ROM content=Program\n      memory type=ROM content=Level-1\n      memory type=ROM content=Level-2\n      memory type=ROM content=Level-3\n    dip\n  processor manufacturer=NEC architecture=uPD7725\n    map address=20-3f,a0-bf:8000-ffff mask=0x7fff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: EVENT-PF94\n  memory type=RAM content=Save\n    map address=30-3f,b0-bf:6000-7fff mask=0xe000\n  processor manufacturer=NEC architecture=uPD78214\n    identifier: PowerFest '94\n    map address=10,20:6000\n    mcu\n      map address=00-3f,80-bf:8000-ffff\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n      memory type=ROM content=Level-1\n      memory type=ROM content=Level-2\n      memory type=ROM content=Level-3\n    dip\n  processor manufacturer=NEC architecture=uPD7725\n    map address=00-0f,80-8f:6000-7fff mask=0xfff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: EXHIROM\n  memory type=ROM content=Program\n    map address=00-3f:8000-ffff base=0x400000\n    map address=40-7d:0000-ffff base=0x400000\n    map address=80-bf:8000-ffff mask=0xc00000\n    map address=c0-ff:0000-ffff mask=0xc00000\n\nboard: EXHIROM-RAM\n  memory type=ROM content=Program\n    map address=00-3f:8000-ffff base=0x400000\n    map address=40-7d:0000-ffff base=0x400000\n    map address=80-bf:8000-ffff mask=0xc00000\n    map address=c0-ff:0000-ffff mask=0xc00000\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n\nboard: EXHIROM-RAM-SHARPRTC\n  memory type=ROM content=Program\n    map address=00-3f:8000-ffff base=0x400000\n    map address=40-7d:0000-ffff base=0x400000\n    map address=80-bf:8000-ffff mask=0xc00000\n    map address=c0-ff:0000-ffff mask=0xc00000\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n  rtc manufacturer=Sharp\n    map address=00-3f,80-bf:2800-2801\n    memory type=RTC content=Time manufacturer=Sharp\n\nboard: EXLOROM\n  memory type=ROM content=Program\n    map address=00-7d:8000-ffff mask=0x808000 base=0x400000\n    map address=80-ff:8000-ffff mask=0x808000 base=0x000000\n\nboard: EXLOROM-RAM\n  memory type=ROM content=Program\n    map address=00-7d:8000-ffff mask=0x808000 base=0x400000\n    map address=80-ff:8000-ffff mask=0x808000 base=0x000000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: EXNEC-LOROM\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  processor architecture=uPD96050\n    map address=60-67,e0-e7:0000-3fff\n    memory type=ROM content=Program architecture=uPD96050\n    memory type=ROM content=Data architecture=uPD96050\n    memory type=RAM content=Data architecture=uPD96050\n      map address=68-6f,e8-ef:0000-7fff mask=0x8000\n    oscillator\n\nboard: EXSPC7110-RAM-EPSONRTC\n  memory type=ROM content=Expansion\n    map address=40-4f:0000-ffff\n  processor identifier=SPC7110\n    map address=00-3f,80-bf:4800-483f\n    map address=50,58:0000-ffff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x800000\n      map address=c0-ff:0000-ffff mask=0xc00000\n      memory type=ROM content=Program\n      memory type=ROM content=Data\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff mask=0xe000\n  rtc manufacturer=Epson\n    map address=00-3f,80-bf:4840-4842\n    memory type=RTC content=Time manufacturer=Epson\n\nboard: GB-LOROM\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n    map address=40-7d,c0-ff:0000-7fff mask=0x8000\n  processor identifier=ICD revision=2\n    map address=00-3f,80-bf:6000-67ff,7000-7fff\n    memory type=ROM content=Boot architecture=SM83\n    oscillator\n    slot type=GameBoy\n\nboard: GSU-RAM\n  processor architecture=GSU\n    map address=00-3f,80-bf:3000-34ff\n    memory type=ROM content=Program\n      map address=00-3f,80-bf:8000-ffff mask=0x8000\n      map address=40-5f,c0-df:0000-ffff\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=70-71,f0-f1:0000-ffff\n\nboard: HIROM\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n\nboard: HIROM-RAM\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n\nboard: HITACHI-LOROM\n  processor architecture=HG51BS169\n    map address=00-3f,80-bf:6c00-6fff,7c00-7fff\n    memory type=ROM content=Program\n      map address=00-3f,80-bf:8000-ffff mask=0x8000\n    memory type=RAM content=Save\n      map address=70-77:0000-7fff\n    memory type=ROM content=Data architecture=HG51BS169\n    memory type=RAM content=Data architecture=HG51BS169\n      map address=00-3f,80-bf:6000-6bff,7000-7bff mask=0xf000\n    oscillator\n\nboard: LOROM\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n\nboard: LOROM-RAM\n  memory type=ROM content=Program\n    map address=00-7d,80-ff:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n\nboard: LOROM-RAM#A\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-ffff mask=0x8000\n\nboard: NEC-HIROM\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  processor architecture=uPD7725\n    map address=00-1f,80-9f:6000-7fff mask=0xfff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: NEC-HIROM-RAM\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff\n    map address=40-7d,c0-ff:0000-ffff\n  memory type=RAM content=Save\n    map address=20-3f,a0-bf:6000-7fff mask=0xe000\n  processor architecture=uPD7725\n    map address=00-1f,80-9f:6000-7fff mask=0xfff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: NEC-LOROM\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff mask=0x8000\n  processor architecture=uPD7725\n    map address=30-3f,b0-bf:8000-ffff mask=0x3fff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: NEC-LOROM-RAM\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-7fff mask=0x8000\n  processor architecture=uPD7725\n    map address=60-6f,e0-ef:0000-7fff mask=0x3fff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: NEC-LOROM-RAM#A\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff mask=0x8000\n  memory type=RAM content=Save\n    map address=70-7d,f0-ff:0000-ffff\n  processor architecture=uPD7725\n    map address=20-3f,a0-bf:8000-ffff mask=0x3fff\n    memory type=ROM content=Program architecture=uPD7725\n    memory type=ROM content=Data architecture=uPD7725\n    memory type=RAM content=Data architecture=uPD7725\n    oscillator\n\nboard: OBC1-LOROM-RAM\n  memory type=ROM content=Program\n    map address=00-3f,80-bf:8000-ffff mask=0x8000\n  processor identifier=OBC1\n    map address=00-3f,80-bf:6000-7fff mask=0xe000\n    map address=70-71,f0-f1:6000-7fff,e000-ffff mask=0xe000\n    memory type=RAM content=Save\n\nboard: SA1\n  processor architecture=W65C816S\n    map address=00-3f,80-bf:2200-23ff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x408000\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n    memory type=RAM content=Internal\n      map address=00-3f,80-bf:3000-37ff size=0x800\n\nboard: SA1-RAM\n  processor architecture=W65C816S\n    map address=00-3f,80-bf:2200-23ff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x408000\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff size=0x2000\n      map address=40-4f:0000-ffff\n    memory type=RAM content=Internal\n      map address=00-3f,80-bf:3000-37ff size=0x800\n\nboard: SDD1\n  processor identifier=SDD1\n    map address=00-3f,80-bf:4800-480f\n    mcu\n      map address=00-3f,80-bf:8000-ffff\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n\nboard: SDD1-RAM\n  memory type=RAM content=Save\n    map address=00-3f,80-bf:6000-7fff mask=0xe000\n    map address=70-73:0000-ffff mask=0x8000\n  processor identifier=SDD1\n    map address=00-3f,80-bf:4800-480f\n    mcu\n      map address=00-3f,80-bf:8000-ffff\n      map address=c0-ff:0000-ffff\n      memory type=ROM content=Program\n\nboard: SPC7110-RAM\n  processor identifier=SPC7110\n    map address=00-3f,80-bf:4800-483f\n    map address=50,58:0000-ffff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x800000\n      map address=c0-ff:0000-ffff mask=0xc00000\n      memory type=ROM content=Program\n      memory type=ROM content=Data\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff mask=0xe000\n\nboard: SPC7110-RAM-EPSONRTC\n  processor identifier=SPC7110\n    map address=00-3f,80-bf:4800-483f\n    map address=50,58:0000-ffff\n    mcu\n      map address=00-3f,80-bf:8000-ffff mask=0x800000\n      map address=c0-ff:0000-ffff mask=0xc00000\n      memory type=ROM content=Program\n      memory type=ROM content=Data\n    memory type=RAM content=Save\n      map address=00-3f,80-bf:6000-7fff mask=0xe000\n  rtc manufacturer=Epson\n    map address=00-3f,80-bf:4840-4842\n    memory type=RTC content=Time manufacturer=Epson\n\nboard: ST-LOROM\n  memory type=ROM content=Program\n    map address=00-1f,80-9f:8000-ffff mask=0x8000\n  slot type=SufamiTurbo\n    rom\n      map address=20-3f,a0-bf:8000-ffff mask=0x8000\n    ram\n      map address=60-6f,e0-ef:0000-ffff\n  slot type=SufamiTurbo\n    rom\n      map address=40-5f,c0-df:0000-ffff mask=0x8000\n    ram\n      map address=70-7d,f0-ff:0000-ffff\n\n";
	if(auto fp = platform->open(ID::System, "boards.bml", File::Read, File::Required)) {
    auto document = BML::unserialize(fp->reads()); // (string(constant_boards_bml_file_hardcoded));
    for(auto leaf : document.find("board")) {
      auto id = leaf.text();
      bool matched = id == board;
      if(!matched && id.match("*(*)*")) {
        auto part = id.transform("()", "||").split("|");
        for(auto& revision : part(1).split(",")) {
          if(string{part(0), revision, part(2)} == board) matched = true;
        }
      }
      if(matched) return leaf;
    }
  }

  return {};
}

auto Cartridge::loadCartridge(Markup::Node node, const char* rom_data, unsigned rom_size) -> void {
  board = node["board"];
  if(!board) board = loadBoard(game.board);

  if(region() == "Auto") {
    auto region = game.region;
    if(region.endsWith("BRA")
    || region.endsWith("CAN")
    || region.endsWith("HKG")
    || region.endsWith("JPN")
    || region.endsWith("KOR")
    || region.endsWith("LTN")
    || region.endsWith("ROC")
    || region.endsWith("USA")
    || region.beginsWith("SHVC-")
    || region == "NTSC") {
      information.region = "NTSC";
    } else {
      information.region = "PAL";
    }
  }

  if(auto node = board["memory(type=ROM,content=Program)"]) loadROM(node, rom_data, rom_size);
  if(auto node = board["memory(type=ROM,content=Expansion)"]) loadROM(node);  //todo: handle this better
  if(auto node = board["memory(type=RAM,content=Save)"]) loadRAM(node);
  if(auto node = board["processor(identifier=ICD)"]) loadICD(node);
  if(auto node = board["processor(identifier=MCC)"]) loadMCC(node);
  if(auto node = board["slot(type=BSMemory)"]) loadBSMemory(node);
  if(auto node = board["slot(type=SufamiTurbo)[0]"]) loadSufamiTurboA(node);
  if(auto node = board["slot(type=SufamiTurbo)[1]"]) loadSufamiTurboB(node);
  if(auto node = board["dip"]) loadDIP(node);
  if(auto node = board["processor(architecture=uPD78214)"]) loadEvent(node);
  if(auto node = board["processor(architecture=W65C816S)"]) loadSA1(node);
  if(auto node = board["processor(architecture=GSU)"]) loadSuperFX(node);
  if(auto node = board["processor(architecture=ARM6)"]) loadARMDSP(node);
  if(auto node = board["processor(architecture=HG51BS169)"]) loadHitachiDSP(node, game.board.match("2DC*") ? 2 : 1);
  if(auto node = board["processor(architecture=uPD7725)"]) loaduPD7725(node);
  if(auto node = board["processor(architecture=uPD96050)"]) loaduPD96050(node);
  if(auto node = board["rtc(manufacturer=Epson)"]) loadEpsonRTC(node);
  if(auto node = board["rtc(manufacturer=Sharp)"]) loadSharpRTC(node);
  if(auto node = board["processor(identifier=SPC7110)"]) loadSPC7110(node);
  if(auto node = board["processor(identifier=SDD1)"]) loadSDD1(node);
  if(auto node = board["processor(identifier=OBC1)"]) loadOBC1(node);

  // if(auto fp = platform->open(pathID(), "msu1/data.rom", File::Read)) loadMSU1();
}

auto Cartridge::loadCartridgeBSMemory(Markup::Node node) -> void {
  if(auto memory = Emulator::Game::Memory{node["game/board/memory(content=Program)"]}) {
    bsmemory.ROM = memory.type == "ROM";
    bsmemory.memory.allocate(memory.size);
    if(auto fp = platform->open(bsmemory.pathID, memory.name(), File::Read, File::Required)) {
      fp->read(bsmemory.memory.data(), memory.size);
    }
  }
}

auto Cartridge::loadCartridgeSufamiTurboA(Markup::Node node) -> void {
  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=ROM,content=Program)"]}) {
    sufamiturboA.rom.allocate(memory.size);
    if(auto fp = platform->open(sufamiturboA.pathID, memory.name(), File::Read, File::Required)) {
      fp->read(sufamiturboA.rom.data(), memory.size);
    }
  }

  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=RAM,content=Save)"]}) {
    sufamiturboA.ram.allocate(memory.size);
    if(auto fp = platform->open(sufamiturboA.pathID, memory.name(), File::Read)) {
      fp->read(sufamiturboA.ram.data(), memory.size);
    }
  }
}

auto Cartridge::loadCartridgeSufamiTurboB(Markup::Node node) -> void {
  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=ROM,content=Program)"]}) {
    sufamiturboB.rom.allocate(memory.size);
    if(auto fp = platform->open(sufamiturboB.pathID, memory.name(), File::Read, File::Required)) {
      fp->read(sufamiturboB.rom.data(), memory.size);
    }
  }

  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=RAM,content=Save)"]}) {
    sufamiturboB.ram.allocate(memory.size);
    if(auto fp = platform->open(sufamiturboB.pathID, memory.name(), File::Read)) {
      fp->read(sufamiturboB.ram.data(), memory.size);
    }
  }
}

//

auto Cartridge::loadMemory(Memory& ram, Markup::Node node, bool required, const char* rom_data, unsigned rom_size) -> void {
  if(auto memory = game.memory(node)) {
    ram.allocate(memory->size);
    if(memory->type == "RAM" && !memory->nonVolatile) return;
    if(memory->type == "RTC" && !memory->nonVolatile) return;
		// if (rom_data) {
			memcpy(ram.data(), rom_data, min(ram.size(), rom_size));
		// } else
    // if(auto fp = platform->open(pathID(), memory->name(), File::Read, required)) {
      // fp->read(ram.data(), min(fp->size(), ram.size()));
    // }
  }
}

template<typename T>  //T = ReadableMemory, WritableMemory, ProtectableMemory
auto Cartridge::loadMap(Markup::Node map, T& memory) -> uint {
  auto addr = map["address"].text();
  auto size = map["size"].natural();
  auto base = map["base"].natural();
  auto mask = map["mask"].natural();
  if(size == 0) size = memory.size();
  if(size == 0) return print("loadMap(): size=0\n"), 0;  //does this ever actually occur?
  return bus.map({&T::read, &memory}, {&T::write, &memory}, addr, size, base, mask);
}

auto Cartridge::loadMap(
  Markup::Node map,
  const function<uint8 (uint, uint8)>& reader,
  const function<void  (uint, uint8)>& writer
) -> uint {
  auto addr = map["address"].text();
  auto size = map["size"].natural();
  auto base = map["base"].natural();
  auto mask = map["mask"].natural();
  return bus.map(reader, writer, addr, size, base, mask);
}

//memory(type=ROM,content=Program)
auto Cartridge::loadROM(Markup::Node node, const char* rom_data, unsigned rom_size) -> void {
  loadMemory(rom, node, File::Required, rom_data, rom_size);
  for(auto leaf : node.find("map")) loadMap(leaf, rom);
}

//memory(type=RAM,content=Save)
auto Cartridge::loadRAM(Markup::Node node) -> void {
	// abort();
  loadMemory(ram, node, File::Optional);
  for(auto leaf : node.find("map")) loadMap(leaf, ram);
}

//processor(identifier=ICD)
auto Cartridge::loadICD(Markup::Node node) -> void {
  has.GameBoySlot = true;
  has.ICD = true;

  icd.Revision = node["revision"].natural();
  if(auto oscillator = game.oscillator()) {
    icd.Frequency = oscillator->frequency;
  } else {
    icd.Frequency = 0;
  }

  //Game Boy core loads data through ICD interface
  for(auto map : node.find("map")) {
    loadMap(map, {&ICD::readIO, &icd}, {&ICD::writeIO, &icd});
  }
}

//processor(identifier=MCC)
auto Cartridge::loadMCC(Markup::Node node) -> void {
  has.MCC = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&MCC::read, &mcc}, {&MCC::write, &mcc});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&MCC::mcuRead, &mcc}, {&MCC::mcuWrite, &mcc});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(mcc.rom, memory, File::Required);
    }
    if(auto memory = mcu["memory(type=RAM,content=Download)"]) {
      loadMemory(mcc.psram, memory, File::Optional);
    }
    if(auto slot = mcu["slot(type=BSMemory)"]) {
      loadBSMemory(slot);
    }
  }
}

//slot(type=BSMemory)
auto Cartridge::loadBSMemory(Markup::Node node) -> void {
  has.BSMemorySlot = true;

  if(auto loaded = platform->load(ID::BSMemory, "BS Memory", "bs")) {
    bsmemory.pathID = loaded.pathID;
    loadBSMemory();

    for(auto map : node.find("map")) {
      loadMap(map, bsmemory);
    }
  }
}

//slot(type=SufamiTurbo)[0]
auto Cartridge::loadSufamiTurboA(Markup::Node node) -> void {
  has.SufamiTurboSlotA = true;

  if(auto loaded = platform->load(ID::SufamiTurboA, "Sufami Turbo", "st")) {
    sufamiturboA.pathID = loaded.pathID;
    loadSufamiTurboA();

    for(auto map : node.find("rom/map")) {
      loadMap(map, sufamiturboA.rom);
    }

    for(auto map : node.find("ram/map")) {
      loadMap(map, sufamiturboA.ram);
    }
  }
}

//slot(type=SufamiTurbo)[1]
auto Cartridge::loadSufamiTurboB(Markup::Node node) -> void {
  has.SufamiTurboSlotB = true;

  if(auto loaded = platform->load(ID::SufamiTurboB, "Sufami Turbo", "st")) {
    sufamiturboB.pathID = loaded.pathID;
    loadSufamiTurboB();

    for(auto map : node.find("rom/map")) {
      loadMap(map, sufamiturboB.rom);
    }

    for(auto map : node.find("ram/map")) {
      loadMap(map, sufamiturboB.ram);
    }
  }
}

//dip
auto Cartridge::loadDIP(Markup::Node node) -> void {
  has.DIP = true;
  dip.value = platform->dipSettings(node);

  for(auto map : node.find("map")) {
    loadMap(map, {&DIP::read, &dip}, {&DIP::write, &dip});
  }
}

//processor(architecture=uPD78214)
auto Cartridge::loadEvent(Markup::Node node) -> void {
  has.Event = true;
  event.board = Event::Board::Unknown;
  if(node["identifier"].text() == "Campus Challenge '92") event.board = Event::Board::CampusChallenge92;
  if(node["identifier"].text() == "PowerFest '94") event.board = Event::Board::PowerFest94;

  for(auto map : node.find("map")) {
    loadMap(map, {&Event::read, &event}, {&Event::write, &event});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&Event::mcuRead, &event}, {&Event::mcuWrite, &event});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(event.rom[0], memory, File::Required);
    }
    if(auto memory = mcu["memory(type=ROM,content=Level-1)"]) {
      loadMemory(event.rom[1], memory, File::Required);
    }
    if(auto memory = mcu["memory(type=ROM,content=Level-2)"]) {
      loadMemory(event.rom[2], memory, File::Required);
    }
    if(auto memory = mcu["memory(type=ROM,content=Level-3)"]) {
      loadMemory(event.rom[3], memory, File::Required);
    }
  }
}

//processor(architecture=W65C816S)
auto Cartridge::loadSA1(Markup::Node node) -> void {
  has.SA1 = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&SA1::readIOCPU, &sa1}, {&SA1::writeIOCPU, &sa1});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&SA1::ROM::readCPU, &sa1.rom}, {&SA1::ROM::writeCPU, &sa1.rom});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(sa1.rom, memory, File::Required);
    }
    if(auto slot = mcu["slot(type=BSMemory)"]) {
      loadBSMemory(slot);
    }
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(sa1.bwram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, {&SA1::BWRAM::readCPU, &sa1.bwram}, {&SA1::BWRAM::writeCPU, &sa1.bwram});
    }
  }

  if(auto memory = node["memory(type=RAM,content=Internal)"]) {
    loadMemory(sa1.iram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, {&SA1::IRAM::readCPU, &sa1.iram}, {&SA1::IRAM::writeCPU, &sa1.iram});
    }
  }
}

//processor(architecture=GSU)
auto Cartridge::loadSuperFX(Markup::Node node) -> void {
  has.SuperFX = true;

  if(auto oscillator = game.oscillator()) {
    superfx.Frequency = oscillator->frequency;  //GSU-1, GSU-2
  } else {
    superfx.Frequency = system.cpuFrequency();  //MARIO CHIP 1
  }

  for(auto map : node.find("map")) {
    loadMap(map, {&SuperFX::readIO, &superfx}, {&SuperFX::writeIO, &superfx});
  }

  if(auto memory = node["memory(type=ROM,content=Program)"]) {
    loadMemory(superfx.rom, memory, File::Required);
    for(auto map : memory.find("map")) {
      loadMap(map, superfx.cpurom);
    }
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(superfx.ram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, superfx.cpuram);
    }
  }
}

//processor(architecture=ARM6)
auto Cartridge::loadARMDSP(Markup::Node node) -> void {
  has.ARMDSP = true;

  for(auto& word : armdsp.programROM) word = 0x00;
  for(auto& word : armdsp.dataROM) word = 0x00;
  for(auto& word : armdsp.programRAM) word = 0x00;

  if(auto oscillator = game.oscillator()) {
    armdsp.Frequency = oscillator->frequency;
  } else {
    armdsp.Frequency = 21'440'000;
  }

  for(auto map : node.find("map")) {
    loadMap(map, {&ArmDSP::read, &armdsp}, {&ArmDSP::write, &armdsp});
  }

  if(auto memory = node["memory(type=ROM,content=Program,architecture=ARM6)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read, File::Required)) {
        for(auto n : range(128 * 1024)) armdsp.programROM[n] = fp->read();
      }
    }
  }

  if(auto memory = node["memory(type=ROM,content=Data,architecture=ARM6)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read, File::Required)) {
        for(auto n : range(32 * 1024)) armdsp.dataROM[n] = fp->read();
      }
    }
  }

  if(auto memory = node["memory(type=RAM,content=Data,architecture=ARM6)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(16 * 1024)) armdsp.programRAM[n] = fp->read();
      }
    }
  }
}

//processor(architecture=HG51BS169)
auto Cartridge::loadHitachiDSP(Markup::Node node, uint roms) -> void {
  for(auto& word : hitachidsp.dataROM) word = 0x000000;
  for(auto& word : hitachidsp.dataRAM) word = 0x00;

  if(auto oscillator = game.oscillator()) {
    hitachidsp.Frequency = oscillator->frequency;
  } else {
    hitachidsp.Frequency = 20'000'000;
  }
  hitachidsp.Roms = roms;  //1 or 2
  hitachidsp.Mapping = 0;  //0 or 1

  if(auto memory = node["memory(type=ROM,content=Program)"]) {
    loadMemory(hitachidsp.rom, memory, File::Required);
    for(auto map : memory.find("map")) {
      loadMap(map, {&HitachiDSP::readROM, &hitachidsp}, {&HitachiDSP::writeROM, &hitachidsp});
    }
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(hitachidsp.ram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, {&HitachiDSP::readRAM, &hitachidsp}, {&HitachiDSP::writeRAM, &hitachidsp});
    }
  }

  if(configuration.hacks.coprocessor.preferHLE) {
    has.Cx4 = true;
    for(auto map : node.find("map")) {
      loadMap(map, {&Cx4::read, &cx4}, {&Cx4::write, &cx4});
    }
    if(auto memory = node["memory(type=RAM,content=Data,architecture=HG51BS169)"]) {
      for(auto map : memory.find("map")) {
        loadMap(map, {&Cx4::read, &cx4}, {&Cx4::write, &cx4});
      }
    }
    return;
  }

  if(auto memory = node["memory(type=ROM,content=Data,architecture=HG51BS169)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(1 * 1024)) hitachidsp.dataROM[n] = fp->readl(3);
      } else {
        for(auto n : range(1 * 1024)) {
          hitachidsp.dataROM[n]  = hitachidsp.staticDataROM[n * 3 + 0] <<  0;
          hitachidsp.dataROM[n] |= hitachidsp.staticDataROM[n * 3 + 1] <<  8;
          hitachidsp.dataROM[n] |= hitachidsp.staticDataROM[n * 3 + 2] << 16;
        }
      }
    }
  }

  if(auto memory = node["memory(type=RAM,content=Data,architecture=HG51BS169)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(3 * 1024)) hitachidsp.dataRAM[n] = fp->readl(1);
      }
    }
    for(auto map : memory.find("map")) {
      loadMap(map, {&HitachiDSP::readDRAM, &hitachidsp}, {&HitachiDSP::writeDRAM, &hitachidsp});
    }
  }

  has.HitachiDSP = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&HitachiDSP::readIO, &hitachidsp}, {&HitachiDSP::writeIO, &hitachidsp});
  }
}

//processor(architecture=uPD7725)
auto Cartridge::loaduPD7725(Markup::Node node) -> void {
  for(auto& word : necdsp.programROM) word = 0x000000;
  for(auto& word : necdsp.dataROM) word = 0x0000;
  for(auto& word : necdsp.dataRAM) word = 0x0000;

  if(auto oscillator = game.oscillator()) {
    necdsp.Frequency = oscillator->frequency;
  } else {
    necdsp.Frequency = 7'600'000;
  }

  bool failed = false;

  if(auto memory = node["memory(type=ROM,content=Program,architecture=uPD7725)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(2048)) necdsp.programROM[n] = fp->readl(3);
      } else failed = true;
    }
  }

  if(auto memory = node["memory(type=ROM,content=Data,architecture=uPD7725)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(1024)) necdsp.dataROM[n] = fp->readl(2);
      } else failed = true;
    }
  }

  if(failed || configuration.hacks.coprocessor.preferHLE) {
    auto manifest = BML::serialize(game.document);
    if(manifest.find("identifier: DSP1")) {  //also matches DSP1B
      has.DSP1 = true;
      for(auto map : node.find("map")) {
        loadMap(map, {&DSP1::read, &dsp1}, {&DSP1::write, &dsp1});
      }
      return;
    }
    if(manifest.find("identifier: DSP2")) {
      has.DSP2 = true;
      for(auto map : node.find("map")) {
        loadMap(map, {&DSP2::read, &dsp2}, {&DSP2::write, &dsp2});
      }
      return;
    }
    if(manifest.find("identifier: DSP4")) {
      has.DSP4 = true;
      for(auto map : node.find("map")) {
        loadMap(map, {&DSP4::read, &dsp4}, {&DSP4::write, &dsp4});
      }
      return;
    }
  }

  if(failed) {
    //throw an error to the user
    platform->open(ID::SuperFamicom, "DSP3", File::Read, File::Required);
    return;
  }

  if(auto memory = node["memory(type=RAM,content=Data,architecture=uPD7725)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(256)) necdsp.dataRAM[n] = fp->readl(2);
      }
    }
    for(auto map : memory.find("map")) {
      loadMap(map, {&NECDSP::readRAM, &necdsp}, {&NECDSP::writeRAM, &necdsp});
    }
  }

  has.NECDSP = true;
  necdsp.revision = NECDSP::Revision::uPD7725;

  for(auto map : node.find("map")) {
    loadMap(map, {&NECDSP::read, &necdsp}, {&NECDSP::write, &necdsp});
  }
}

//processor(architecture=uPD96050)
auto Cartridge::loaduPD96050(Markup::Node node) -> void {
  for(auto& word : necdsp.programROM) word = 0x000000;
  for(auto& word : necdsp.dataROM) word = 0x0000;
  for(auto& word : necdsp.dataRAM) word = 0x0000;

  if(auto oscillator = game.oscillator()) {
    necdsp.Frequency = oscillator->frequency;
  } else {
    necdsp.Frequency = 11'000'000;
  }

  bool failed = false;

  if(auto memory = node["memory(type=ROM,content=Program,architecture=uPD96050)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(16384)) necdsp.programROM[n] = fp->readl(3);
      } else failed = true;
    }
  }

  if(auto memory = node["memory(type=ROM,content=Data,architecture=uPD96050)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(2048)) necdsp.dataROM[n] = fp->readl(2);
      } else failed = true;
    }
  }

  if(failed || configuration.hacks.coprocessor.preferHLE) {
    auto manifest = BML::serialize(game.document);
    if(manifest.find("identifier: ST010")) {
      has.ST0010 = true;
      if(auto memory = node["memory(type=RAM,content=Data,architecture=uPD96050)"]) {
        for(auto map : memory.find("map")) {
          loadMap(map, {&ST0010::read, &st0010}, {&ST0010::write, &st0010});
        }
      }
      return;
    }
  }

  if(failed) {
    //throw an error to the user
    platform->open(ID::SuperFamicom, "ST011", File::Read, File::Required);
    return;
  }

  if(auto memory = node["memory(type=RAM,content=Data,architecture=uPD96050)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(2048)) necdsp.dataRAM[n] = fp->readl(2);
      }
    }
    for(auto map : memory.find("map")) {
      loadMap(map, {&NECDSP::readRAM, &necdsp}, {&NECDSP::writeRAM, &necdsp});
    }
  }

  has.NECDSP = true;
  necdsp.revision = NECDSP::Revision::uPD96050;

  for(auto map : node.find("map")) {
    loadMap(map, {&NECDSP::read, &necdsp}, {&NECDSP::write, &necdsp});
  }
}

//rtc(manufacturer=Epson)
auto Cartridge::loadEpsonRTC(Markup::Node node) -> void {
  has.EpsonRTC = true;

  epsonrtc.initialize();

  for(auto map : node.find("map")) {
    loadMap(map, {&EpsonRTC::read, &epsonrtc}, {&EpsonRTC::write, &epsonrtc});
  }

  if(auto memory = node["memory(type=RTC,content=Time,manufacturer=Epson)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        uint8 data[16] = {0};
        for(auto& byte : data) byte = fp->read();
        epsonrtc.load(data);
      }
    }
  }
}

//rtc(manufacturer=Sharp)
auto Cartridge::loadSharpRTC(Markup::Node node) -> void {
  has.SharpRTC = true;

  sharprtc.initialize();

  for(auto map : node.find("map")) {
    loadMap(map, {&SharpRTC::read, &sharprtc}, {&SharpRTC::write, &sharprtc});
  }

  if(auto memory = node["memory(type=RTC,content=Time,manufacturer=Sharp)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        uint8 data[16] = {0};
        for(auto& byte : data) byte = fp->read();
        sharprtc.load(data);
      }
    }
  }
}

//processor(identifier=SPC7110)
auto Cartridge::loadSPC7110(Markup::Node node) -> void {
  has.SPC7110 = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&SPC7110::read, &spc7110}, {&SPC7110::write, &spc7110});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&SPC7110::mcuromRead, &spc7110}, {&SPC7110::mcuromWrite, &spc7110});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(spc7110.prom, memory, File::Required);
    }
    if(auto memory = mcu["memory(type=ROM,content=Data)"]) {
      loadMemory(spc7110.drom, memory, File::Required);
    }
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(spc7110.ram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, {&SPC7110::mcuramRead, &spc7110}, {&SPC7110::mcuramWrite, &spc7110});
    }
  }
}

//processor(identifier=SDD1)
auto Cartridge::loadSDD1(Markup::Node node) -> void {
  has.SDD1 = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&SDD1::ioRead, &sdd1}, {&SDD1::ioWrite, &sdd1});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&SDD1::mcuRead, &sdd1}, {&SDD1::mcuWrite, &sdd1});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(sdd1.rom, memory, File::Required);
    }
  }
}

//processor(identifier=OBC1)
auto Cartridge::loadOBC1(Markup::Node node) -> void {
  has.OBC1 = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&OBC1::read, &obc1}, {&OBC1::write, &obc1});
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(obc1.ram, memory, File::Optional);
  }
}

//file::exists("msu1/data.rom")
auto Cartridge::loadMSU1() -> void {
  has.MSU1 = true;

  bus.map({&MSU1::readIO, &msu1}, {&MSU1::writeIO, &msu1}, "00-3f,80-bf:2000-2007");
}
