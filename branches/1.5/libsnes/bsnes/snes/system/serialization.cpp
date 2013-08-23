#ifdef SYSTEM_CPP

serializer System::serialize() {
  serializer s(serialize_size);

  unsigned signature = 0x31545342, version = Info::SerializerVersion, crc32 = cartridge.crc32();
  char description[512], profile[16];
  memset(&description, 0, sizeof description);
  memset(&profile, 0, sizeof profile);
  strmcpy(profile, Info::Profile, sizeof profile);

  s.integer(signature);
  s.integer(version);
  s.integer(crc32);
  s.array(description);
  s.array(profile);

  serialize_all(s);
  return s;
}

bool System::unserialize(serializer &s) {
  unsigned signature, version, crc32;
  char description[512], profile[16];

  s.integer(signature);
  s.integer(version);
  s.integer(crc32);
  s.array(description);
  s.array(profile);

  if(signature != 0x31545342) return false;
  if(version != Info::SerializerVersion) return false;
//if(crc32 != cartridge.crc32()) return false;
  if(strcmp(profile, Info::Profile)) return false;

  power();
  serialize_all(s);
  return true;
}

//========
//internal
//========

void System::serialize(serializer &s) {
  s.integer((unsigned&)region);
  s.integer((unsigned&)expansion);
}

//zero 01-dec-2012 - these will embed strings in the savestates, so you can debug them more easily. but itll break the savestate format
//#define DEBUGSAVESTATE(X) s.array(#X)
#define DEBUGSAVESTATE(X) 

void System::serialize_all(serializer &s) {
	DEBUGSAVESTATE(cart);
  cartridge.serialize(s);
	DEBUGSAVESTATE(system);
  system.serialize(s);
	DEBUGSAVESTATE(random);
  random.serialize(s);
	DEBUGSAVESTATE(cpu);
  cpu.serialize(s);
	DEBUGSAVESTATE(smp);
  smp.serialize(s);
	DEBUGSAVESTATE(ppu);
  ppu.serialize(s);
	DEBUGSAVESTATE(dsp);
  dsp.serialize(s);
	DEBUGSAVESTATE(input);
  input.serialize(s);

	DEBUGSAVESTATE(sufamiturbo);
  if(cartridge.mode() == Cartridge::Mode::SufamiTurbo) sufamiturbo.serialize(s);
  #if defined(GAMEBOY)
	DEBUGSAVESTATE(icd2);
  if(cartridge.mode() == Cartridge::Mode::SuperGameBoy) icd2.serialize(s);
  #endif
	DEBUGSAVESTATE(superfx);
  if(cartridge.has_superfx()) superfx.serialize(s);
	DEBUGSAVESTATE(sa1);
  if(cartridge.has_sa1()) sa1.serialize(s);
	DEBUGSAVESTATE(necdsp);
  if(cartridge.has_necdsp()) necdsp.serialize(s);
	DEBUGSAVESTATE(hitachidsp);
  if(cartridge.has_hitachidsp()) hitachidsp.serialize(s);
	DEBUGSAVESTATE(armdsp);
  if(cartridge.has_armdsp()) armdsp.serialize(s);
	DEBUGSAVESTATE(srtc);
  if(cartridge.has_srtc()) srtc.serialize(s);
	DEBUGSAVESTATE(sdd1);
  if(cartridge.has_sdd1()) sdd1.serialize(s);
	DEBUGSAVESTATE(spc7110);
  if(cartridge.has_spc7110()) spc7110.serialize(s);
	DEBUGSAVESTATE(obc1);
  if(cartridge.has_obc1()) obc1.serialize(s);
	DEBUGSAVESTATE(msu1);
  if(cartridge.has_msu1()) msu1.serialize(s);
}

//perform dry-run state save:
//determines exactly how many bytes are needed to save state for this cartridge,
//as amount varies per game (eg different RAM sizes, special chips, etc.)
void System::serialize_init() {
  serializer s;

  unsigned signature = 0, version = 0, crc32 = 0;
  char profile[16], description[512];

  s.integer(signature);
  s.integer(version);
  s.integer(crc32);
  s.array(profile);
  s.array(description);

  serialize_all(s);
  serialize_size = s.size();
}

#endif
