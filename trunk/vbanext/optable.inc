#define F(fcn) &Gigazoid::fcn

#define REP16(insn) \
    F(insn),F(insn),F(insn),F(insn),F(insn),F(insn),F(insn),F(insn),\
    F(insn),F(insn),F(insn),F(insn),F(insn),F(insn),F(insn),F(insn)
#define REP256(insn) \
    REP16(insn),REP16(insn),REP16(insn),REP16(insn),\
    REP16(insn),REP16(insn),REP16(insn),REP16(insn),\
    REP16(insn),REP16(insn),REP16(insn),REP16(insn),\
    REP16(insn),REP16(insn),REP16(insn),REP16(insn)

#define arm_UI armUnknownInsn
#define arm_BP armUnknownInsn

#define thumbUI thumbUnknownInsn
#define thumbBP thumbUnknownInsn

void (Gigazoid::*const Gigazoid::armInsnTable[4096])(u32 opcode) = {
    F(arm000),F(arm001),F(arm002),F(arm003),F(arm004),F(arm005),F(arm006),F(arm007),  // F(000)
    F(arm000),F(arm009),F(arm002),F(arm00B),F(arm004),F(arm_UI),F(arm006),F(arm_UI),  // F(008)
    F(arm010),F(arm011),F(arm012),F(arm013),F(arm014),F(arm015),F(arm016),F(arm017),  // F(010)
    F(arm010),F(arm019),F(arm012),F(arm01B),F(arm014),F(arm01D),F(arm016),F(arm01F),  // F(018)
    F(arm020),F(arm021),F(arm022),F(arm023),F(arm024),F(arm025),F(arm026),F(arm027),  // F(020)
    F(arm020),F(arm029),F(arm022),F(arm_UI),F(arm024),F(arm_UI),F(arm026),F(arm_UI),  // F(028)
    F(arm030),F(arm031),F(arm032),F(arm033),F(arm034),F(arm035),F(arm036),F(arm037),  // F(030)
    F(arm030),F(arm039),F(arm032),F(arm_UI),F(arm034),F(arm01D),F(arm036),F(arm01F),  // F(038)
    F(arm040),F(arm041),F(arm042),F(arm043),F(arm044),F(arm045),F(arm046),F(arm047),  // F(040)
    F(arm040),F(arm_UI),F(arm042),F(arm04B),F(arm044),F(arm_UI),F(arm046),F(arm_UI),  // F(048)
    F(arm050),F(arm051),F(arm052),F(arm053),F(arm054),F(arm055),F(arm056),F(arm057),  // F(050)
    F(arm050),F(arm_UI),F(arm052),F(arm05B),F(arm054),F(arm05D),F(arm056),F(arm05F),  // F(058)
    F(arm060),F(arm061),F(arm062),F(arm063),F(arm064),F(arm065),F(arm066),F(arm067),  // F(060)
    F(arm060),F(arm_UI),F(arm062),F(arm_UI),F(arm064),F(arm_UI),F(arm066),F(arm_UI),  // F(068)
    F(arm070),F(arm071),F(arm072),F(arm073),F(arm074),F(arm075),F(arm076),F(arm077),  // F(070)
    F(arm070),F(arm_UI),F(arm072),F(arm_UI),F(arm074),F(arm05D),F(arm076),F(arm05F),  // F(078)
    F(arm080),F(arm081),F(arm082),F(arm083),F(arm084),F(arm085),F(arm086),F(arm087),  // F(080)
    F(arm080),F(arm089),F(arm082),F(arm08B),F(arm084),F(arm_UI),F(arm086),F(arm_UI),  // F(088)
    F(arm090),F(arm091),F(arm092),F(arm093),F(arm094),F(arm095),F(arm096),F(arm097),  // F(090)
    F(arm090),F(arm099),F(arm092),F(arm09B),F(arm094),F(arm09D),F(arm096),F(arm09F),  // F(098)
    F(arm0A0),F(arm0A1),F(arm0A2),F(arm0A3),F(arm0A4),F(arm0A5),F(arm0A6),F(arm0A7),  // F(0A0)
    F(arm0A0),F(arm0A9),F(arm0A2),F(arm_UI),F(arm0A4),F(arm_UI),F(arm0A6),F(arm_UI),  // F(0A8)
    F(arm0B0),F(arm0B1),F(arm0B2),F(arm0B3),F(arm0B4),F(arm0B5),F(arm0B6),F(arm0B7),  // F(0B0)
    F(arm0B0),F(arm0B9),F(arm0B2),F(arm_UI),F(arm0B4),F(arm09D),F(arm0B6),F(arm09F),  // F(0B8)
    F(arm0C0),F(arm0C1),F(arm0C2),F(arm0C3),F(arm0C4),F(arm0C5),F(arm0C6),F(arm0C7),  // F(0C0)
    F(arm0C0),F(arm0C9),F(arm0C2),F(arm0CB),F(arm0C4),F(arm_UI),F(arm0C6),F(arm_UI),  // F(0C8)
    F(arm0D0),F(arm0D1),F(arm0D2),F(arm0D3),F(arm0D4),F(arm0D5),F(arm0D6),F(arm0D7),  // F(0D0)
    F(arm0D0),F(arm0D9),F(arm0D2),F(arm0DB),F(arm0D4),F(arm0DD),F(arm0D6),F(arm0DF),  // F(0D8)
    F(arm0E0),F(arm0E1),F(arm0E2),F(arm0E3),F(arm0E4),F(arm0E5),F(arm0E6),F(arm0E7),  // F(0E0)
    F(arm0E0),F(arm0E9),F(arm0E2),F(arm_UI),F(arm0E4),F(arm_UI),F(arm0E6),F(arm_UI),  // F(0E8)
    F(arm0F0),F(arm0F1),F(arm0F2),F(arm0F3),F(arm0F4),F(arm0F5),F(arm0F6),F(arm0F7),  // F(0F0)
    F(arm0F0),F(arm0F9),F(arm0F2),F(arm_UI),F(arm0F4),F(arm0DD),F(arm0F6),F(arm0DF),  // F(0F8)

    F(arm100),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),  // F(100)
    F(arm_UI),F(arm109),F(arm_UI),F(arm10B),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),  // F(108)
    F(arm110),F(arm111),F(arm112),F(arm113),F(arm114),F(arm115),F(arm116),F(arm117),  // F(110)
    F(arm110),F(arm_UI),F(arm112),F(arm11B),F(arm114),F(arm11D),F(arm116),F(arm11F),  // F(118)
    F(arm120),F(arm121),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_BP),  // F(120)
    F(arm_UI),F(arm_UI),F(arm_UI),F(arm12B),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),  // F(128)
    F(arm130),F(arm131),F(arm132),F(arm133),F(arm134),F(arm135),F(arm136),F(arm137),  // F(130)
    F(arm130),F(arm_UI),F(arm132),F(arm13B),F(arm134),F(arm13D),F(arm136),F(arm13F),  // F(138)
    F(arm140),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),  // F(140)
    F(arm_UI),F(arm149),F(arm_UI),F(arm14B),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),  // F(148)
    F(arm150),F(arm151),F(arm152),F(arm153),F(arm154),F(arm155),F(arm156),F(arm157),  // F(150)
    F(arm150),F(arm_UI),F(arm152),F(arm15B),F(arm154),F(arm15D),F(arm156),F(arm15F),  // F(158)
    F(arm160),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),  // F(160)
    F(arm_UI),F(arm_UI),F(arm_UI),F(arm16B),F(arm_UI),F(arm_UI),F(arm_UI),F(arm_UI),  // F(168)
    F(arm170),F(arm171),F(arm172),F(arm173),F(arm174),F(arm175),F(arm176),F(arm177),  // F(170)
    F(arm170),F(arm_UI),F(arm172),F(arm17B),F(arm174),F(arm17D),F(arm176),F(arm17F),  // F(178)
    F(arm180),F(arm181),F(arm182),F(arm183),F(arm184),F(arm185),F(arm186),F(arm187),  // F(180)
    F(arm180),F(arm_UI),F(arm182),F(arm18B),F(arm184),F(arm_UI),F(arm186),F(arm_UI),  // F(188)
    F(arm190),F(arm191),F(arm192),F(arm193),F(arm194),F(arm195),F(arm196),F(arm197),  // F(190)
    F(arm190),F(arm_UI),F(arm192),F(arm19B),F(arm194),F(arm19D),F(arm196),F(arm19F),  // F(198)
    F(arm1A0),F(arm1A1),F(arm1A2),F(arm1A3),F(arm1A4),F(arm1A5),F(arm1A6),F(arm1A7),  // F(1A0)
    F(arm1A0),F(arm_UI),F(arm1A2),F(arm1AB),F(arm1A4),F(arm_UI),F(arm1A6),F(arm_UI),  // F(1A8)
    F(arm1B0),F(arm1B1),F(arm1B2),F(arm1B3),F(arm1B4),F(arm1B5),F(arm1B6),F(arm1B7),  // F(1B0)
    F(arm1B0),F(arm_UI),F(arm1B2),F(arm1BB),F(arm1B4),F(arm1BD),F(arm1B6),F(arm1BF),  // F(1B8)
    F(arm1C0),F(arm1C1),F(arm1C2),F(arm1C3),F(arm1C4),F(arm1C5),F(arm1C6),F(arm1C7),  // F(1C0)
    F(arm1C0),F(arm_UI),F(arm1C2),F(arm1CB),F(arm1C4),F(arm_UI),F(arm1C6),F(arm_UI),  // F(1C8)
    F(arm1D0),F(arm1D1),F(arm1D2),F(arm1D3),F(arm1D4),F(arm1D5),F(arm1D6),F(arm1D7),  // F(1D0)
    F(arm1D0),F(arm_UI),F(arm1D2),F(arm1DB),F(arm1D4),F(arm1DD),F(arm1D6),F(arm1DF),  // F(1D8)
    F(arm1E0),F(arm1E1),F(arm1E2),F(arm1E3),F(arm1E4),F(arm1E5),F(arm1E6),F(arm1E7),  // F(1E0)
    F(arm1E0),F(arm_UI),F(arm1E2),F(arm1EB),F(arm1E4),F(arm_UI),F(arm1E6),F(arm_UI),  // F(1E8)
    F(arm1F0),F(arm1F1),F(arm1F2),F(arm1F3),F(arm1F4),F(arm1F5),F(arm1F6),F(arm1F7),  // F(1F0)
    F(arm1F0),F(arm_UI),F(arm1F2),F(arm1FB),F(arm1F4),F(arm1FD),F(arm1F6),F(arm1FF),  // F(1F8)

    REP16(arm200),REP16(arm210),REP16(arm220),REP16(arm230),  // 200
    REP16(arm240),REP16(arm250),REP16(arm260),REP16(arm270),  // 240
    REP16(arm280),REP16(arm290),REP16(arm2A0),REP16(arm2B0),  // 280
    REP16(arm2C0),REP16(arm2D0),REP16(arm2E0),REP16(arm2F0),  // 2C0
    REP16(arm_UI),REP16(arm310),REP16(arm320),REP16(arm330),  // 300
    REP16(arm_UI),REP16(arm350),REP16(arm360),REP16(arm370),  // 340
    REP16(arm380),REP16(arm390),REP16(arm3A0),REP16(arm3B0),  // 380
    REP16(arm3C0),REP16(arm3D0),REP16(arm3E0),REP16(arm3F0),  // 3C0

    REP16(arm400),REP16(arm410),REP16(arm400),REP16(arm410),  // 400
    REP16(arm440),REP16(arm450),REP16(arm440),REP16(arm450),  // 440
    REP16(arm480),REP16(arm490),REP16(arm480),REP16(arm490),  // 480
    REP16(arm4C0),REP16(arm4D0),REP16(arm4C0),REP16(arm4D0),  // 4C0
    REP16(arm500),REP16(arm510),REP16(arm520),REP16(arm530),  // 500
    REP16(arm540),REP16(arm550),REP16(arm560),REP16(arm570),  // 540
    REP16(arm580),REP16(arm590),REP16(arm5A0),REP16(arm5B0),  // 580
    REP16(arm5C0),REP16(arm5D0),REP16(arm5E0),REP16(arm5F0),  // 5C0

    F(arm600),F(arm_UI),F(arm602),F(arm_UI),F(arm604),F(arm_UI),F(arm606),F(arm_UI),  // F(600)
    F(arm600),F(arm_UI),F(arm602),F(arm_UI),F(arm604),F(arm_UI),F(arm606),F(arm_UI),  // F(608)
    F(arm610),F(arm_UI),F(arm612),F(arm_UI),F(arm614),F(arm_UI),F(arm616),F(arm_UI),  // F(610)
    F(arm610),F(arm_UI),F(arm612),F(arm_UI),F(arm614),F(arm_UI),F(arm616),F(arm_UI),  // F(618)
    F(arm600),F(arm_UI),F(arm602),F(arm_UI),F(arm604),F(arm_UI),F(arm606),F(arm_UI),  // F(620)
    F(arm600),F(arm_UI),F(arm602),F(arm_UI),F(arm604),F(arm_UI),F(arm606),F(arm_UI),  // F(628)
    F(arm610),F(arm_UI),F(arm612),F(arm_UI),F(arm614),F(arm_UI),F(arm616),F(arm_UI),  // F(630)
    F(arm610),F(arm_UI),F(arm612),F(arm_UI),F(arm614),F(arm_UI),F(arm616),F(arm_UI),  // F(638)
    F(arm640),F(arm_UI),F(arm642),F(arm_UI),F(arm644),F(arm_UI),F(arm646),F(arm_UI),  // F(640)
    F(arm640),F(arm_UI),F(arm642),F(arm_UI),F(arm644),F(arm_UI),F(arm646),F(arm_UI),  // F(648)
    F(arm650),F(arm_UI),F(arm652),F(arm_UI),F(arm654),F(arm_UI),F(arm656),F(arm_UI),  // F(650)
    F(arm650),F(arm_UI),F(arm652),F(arm_UI),F(arm654),F(arm_UI),F(arm656),F(arm_UI),  // F(658)
    F(arm640),F(arm_UI),F(arm642),F(arm_UI),F(arm644),F(arm_UI),F(arm646),F(arm_UI),  // F(660)
    F(arm640),F(arm_UI),F(arm642),F(arm_UI),F(arm644),F(arm_UI),F(arm646),F(arm_UI),  // F(668)
    F(arm650),F(arm_UI),F(arm652),F(arm_UI),F(arm654),F(arm_UI),F(arm656),F(arm_UI),  // F(670)
    F(arm650),F(arm_UI),F(arm652),F(arm_UI),F(arm654),F(arm_UI),F(arm656),F(arm_UI),  // F(678)
    F(arm680),F(arm_UI),F(arm682),F(arm_UI),F(arm684),F(arm_UI),F(arm686),F(arm_UI),  // F(680)
    F(arm680),F(arm_UI),F(arm682),F(arm_UI),F(arm684),F(arm_UI),F(arm686),F(arm_UI),  // F(688)
    F(arm690),F(arm_UI),F(arm692),F(arm_UI),F(arm694),F(arm_UI),F(arm696),F(arm_UI),  // F(690)
    F(arm690),F(arm_UI),F(arm692),F(arm_UI),F(arm694),F(arm_UI),F(arm696),F(arm_UI),  // F(698)
    F(arm680),F(arm_UI),F(arm682),F(arm_UI),F(arm684),F(arm_UI),F(arm686),F(arm_UI),  // F(6A0)
    F(arm680),F(arm_UI),F(arm682),F(arm_UI),F(arm684),F(arm_UI),F(arm686),F(arm_UI),  // F(6A8)
    F(arm690),F(arm_UI),F(arm692),F(arm_UI),F(arm694),F(arm_UI),F(arm696),F(arm_UI),  // F(6B0)
    F(arm690),F(arm_UI),F(arm692),F(arm_UI),F(arm694),F(arm_UI),F(arm696),F(arm_UI),  // F(6B8)
    F(arm6C0),F(arm_UI),F(arm6C2),F(arm_UI),F(arm6C4),F(arm_UI),F(arm6C6),F(arm_UI),  // F(6C0)
    F(arm6C0),F(arm_UI),F(arm6C2),F(arm_UI),F(arm6C4),F(arm_UI),F(arm6C6),F(arm_UI),  // F(6C8)
    F(arm6D0),F(arm_UI),F(arm6D2),F(arm_UI),F(arm6D4),F(arm_UI),F(arm6D6),F(arm_UI),  // F(6D0)
    F(arm6D0),F(arm_UI),F(arm6D2),F(arm_UI),F(arm6D4),F(arm_UI),F(arm6D6),F(arm_UI),  // F(6D8)
    F(arm6C0),F(arm_UI),F(arm6C2),F(arm_UI),F(arm6C4),F(arm_UI),F(arm6C6),F(arm_UI),  // F(6E0)
    F(arm6C0),F(arm_UI),F(arm6C2),F(arm_UI),F(arm6C4),F(arm_UI),F(arm6C6),F(arm_UI),  // F(6E8)
    F(arm6D0),F(arm_UI),F(arm6D2),F(arm_UI),F(arm6D4),F(arm_UI),F(arm6D6),F(arm_UI),  // F(6F0)
    F(arm6D0),F(arm_UI),F(arm6D2),F(arm_UI),F(arm6D4),F(arm_UI),F(arm6D6),F(arm_UI),  // F(6F8)

    F(arm700),F(arm_UI),F(arm702),F(arm_UI),F(arm704),F(arm_UI),F(arm706),F(arm_UI),  // F(700)
    F(arm700),F(arm_UI),F(arm702),F(arm_UI),F(arm704),F(arm_UI),F(arm706),F(arm_UI),  // F(708)
    F(arm710),F(arm_UI),F(arm712),F(arm_UI),F(arm714),F(arm_UI),F(arm716),F(arm_UI),  // F(710)
    F(arm710),F(arm_UI),F(arm712),F(arm_UI),F(arm714),F(arm_UI),F(arm716),F(arm_UI),  // F(718)
    F(arm720),F(arm_UI),F(arm722),F(arm_UI),F(arm724),F(arm_UI),F(arm726),F(arm_UI),  // F(720)
    F(arm720),F(arm_UI),F(arm722),F(arm_UI),F(arm724),F(arm_UI),F(arm726),F(arm_UI),  // F(728)
    F(arm730),F(arm_UI),F(arm732),F(arm_UI),F(arm734),F(arm_UI),F(arm736),F(arm_UI),  // F(730)
    F(arm730),F(arm_UI),F(arm732),F(arm_UI),F(arm734),F(arm_UI),F(arm736),F(arm_UI),  // F(738)
    F(arm740),F(arm_UI),F(arm742),F(arm_UI),F(arm744),F(arm_UI),F(arm746),F(arm_UI),  // F(740)
    F(arm740),F(arm_UI),F(arm742),F(arm_UI),F(arm744),F(arm_UI),F(arm746),F(arm_UI),  // F(748)
    F(arm750),F(arm_UI),F(arm752),F(arm_UI),F(arm754),F(arm_UI),F(arm756),F(arm_UI),  // F(750)
    F(arm750),F(arm_UI),F(arm752),F(arm_UI),F(arm754),F(arm_UI),F(arm756),F(arm_UI),  // F(758)
    F(arm760),F(arm_UI),F(arm762),F(arm_UI),F(arm764),F(arm_UI),F(arm766),F(arm_UI),  // F(760)
    F(arm760),F(arm_UI),F(arm762),F(arm_UI),F(arm764),F(arm_UI),F(arm766),F(arm_UI),  // F(768)
    F(arm770),F(arm_UI),F(arm772),F(arm_UI),F(arm774),F(arm_UI),F(arm776),F(arm_UI),  // F(770)
    F(arm770),F(arm_UI),F(arm772),F(arm_UI),F(arm774),F(arm_UI),F(arm776),F(arm_UI),  // F(778)
    F(arm780),F(arm_UI),F(arm782),F(arm_UI),F(arm784),F(arm_UI),F(arm786),F(arm_UI),  // F(780)
    F(arm780),F(arm_UI),F(arm782),F(arm_UI),F(arm784),F(arm_UI),F(arm786),F(arm_UI),  // F(788)
    F(arm790),F(arm_UI),F(arm792),F(arm_UI),F(arm794),F(arm_UI),F(arm796),F(arm_UI),  // F(790)
    F(arm790),F(arm_UI),F(arm792),F(arm_UI),F(arm794),F(arm_UI),F(arm796),F(arm_UI),  // F(798)
    F(arm7A0),F(arm_UI),F(arm7A2),F(arm_UI),F(arm7A4),F(arm_UI),F(arm7A6),F(arm_UI),  // F(7A0)
    F(arm7A0),F(arm_UI),F(arm7A2),F(arm_UI),F(arm7A4),F(arm_UI),F(arm7A6),F(arm_UI),  // F(7A8)
    F(arm7B0),F(arm_UI),F(arm7B2),F(arm_UI),F(arm7B4),F(arm_UI),F(arm7B6),F(arm_UI),  // F(7B0)
    F(arm7B0),F(arm_UI),F(arm7B2),F(arm_UI),F(arm7B4),F(arm_UI),F(arm7B6),F(arm_UI),  // F(7B8)
    F(arm7C0),F(arm_UI),F(arm7C2),F(arm_UI),F(arm7C4),F(arm_UI),F(arm7C6),F(arm_UI),  // F(7C0)
    F(arm7C0),F(arm_UI),F(arm7C2),F(arm_UI),F(arm7C4),F(arm_UI),F(arm7C6),F(arm_UI),  // F(7C8)
    F(arm7D0),F(arm_UI),F(arm7D2),F(arm_UI),F(arm7D4),F(arm_UI),F(arm7D6),F(arm_UI),  // F(7D0)
    F(arm7D0),F(arm_UI),F(arm7D2),F(arm_UI),F(arm7D4),F(arm_UI),F(arm7D6),F(arm_UI),  // F(7D8)
    F(arm7E0),F(arm_UI),F(arm7E2),F(arm_UI),F(arm7E4),F(arm_UI),F(arm7E6),F(arm_UI),  // F(7E0)
    F(arm7E0),F(arm_UI),F(arm7E2),F(arm_UI),F(arm7E4),F(arm_UI),F(arm7E6),F(arm_UI),  // F(7E8)
    F(arm7F0),F(arm_UI),F(arm7F2),F(arm_UI),F(arm7F4),F(arm_UI),F(arm7F6),F(arm_UI),  // F(7F0)
    F(arm7F0),F(arm_UI),F(arm7F2),F(arm_UI),F(arm7F4),F(arm_UI),F(arm7F6),F(arm_BP),  // F(7F8)

    REP16(arm800),REP16(arm810),REP16(arm820),REP16(arm830),  // 800
    REP16(arm840),REP16(arm850),REP16(arm860),REP16(arm870),  // 840
    REP16(arm880),REP16(arm890),REP16(arm8A0),REP16(arm8B0),  // 880
    REP16(arm8C0),REP16(arm8D0),REP16(arm8E0),REP16(arm8F0),  // 8C0
    REP16(arm900),REP16(arm910),REP16(arm920),REP16(arm930),  // 900
    REP16(arm940),REP16(arm950),REP16(arm960),REP16(arm970),  // 940
    REP16(arm980),REP16(arm990),REP16(arm9A0),REP16(arm9B0),  // 980
    REP16(arm9C0),REP16(arm9D0),REP16(arm9E0),REP16(arm9F0),  // 9C0

    REP256(armA00),                                           // A00
    REP256(armB00),                                           // B00
    REP256(arm_UI),                                           // C00
    REP256(arm_UI),                                           // D00

    F(arm_UI),F(armE01),F(arm_UI),F(armE01),F(arm_UI),F(armE01),F(arm_UI),F(armE01),  // F(E00)
    F(arm_UI),F(armE01),F(arm_UI),F(armE01),F(arm_UI),F(armE01),F(arm_UI),F(armE01),  // F(E08)
    F(arm_UI),F(armE01),F(arm_UI),F(armE01),F(arm_UI),F(armE01),F(arm_UI),F(armE01),  // F(E10)
    F(arm_UI),F(armE01),F(arm_UI),F(armE01),F(arm_UI),F(armE01),F(arm_UI),F(armE01),  // F(E18)
    REP16(arm_UI),                                            // E20
    REP16(arm_UI),                                            // E30
    REP16(arm_UI),REP16(arm_UI),REP16(arm_UI),REP16(arm_UI),  // E40
    REP16(arm_UI),REP16(arm_UI),REP16(arm_UI),REP16(arm_UI),  // E80
    REP16(arm_UI),REP16(arm_UI),REP16(arm_UI),REP16(arm_UI),  // EC0

    REP256(armF00),                                           // F00
};

void (Gigazoid::*const Gigazoid::thumbInsnTable[1024])(u32 opcode) = {
  F(thumb00_00),F(thumb00_01),F(thumb00_02),F(thumb00_03),F(thumb00_04),F(thumb00_05),F(thumb00_06),F(thumb00_07),  // F(00)
  F(thumb00_08),F(thumb00_09),F(thumb00_0A),F(thumb00_0B),F(thumb00_0C),F(thumb00_0D),F(thumb00_0E),F(thumb00_0F),
  F(thumb00_10),F(thumb00_11),F(thumb00_12),F(thumb00_13),F(thumb00_14),F(thumb00_15),F(thumb00_16),F(thumb00_17),
  F(thumb00_18),F(thumb00_19),F(thumb00_1A),F(thumb00_1B),F(thumb00_1C),F(thumb00_1D),F(thumb00_1E),F(thumb00_1F),
  F(thumb08_00),F(thumb08_01),F(thumb08_02),F(thumb08_03),F(thumb08_04),F(thumb08_05),F(thumb08_06),F(thumb08_07),  // F(08)
  F(thumb08_08),F(thumb08_09),F(thumb08_0A),F(thumb08_0B),F(thumb08_0C),F(thumb08_0D),F(thumb08_0E),F(thumb08_0F),
  F(thumb08_10),F(thumb08_11),F(thumb08_12),F(thumb08_13),F(thumb08_14),F(thumb08_15),F(thumb08_16),F(thumb08_17),
  F(thumb08_18),F(thumb08_19),F(thumb08_1A),F(thumb08_1B),F(thumb08_1C),F(thumb08_1D),F(thumb08_1E),F(thumb08_1F),
  F(thumb10_00),F(thumb10_01),F(thumb10_02),F(thumb10_03),F(thumb10_04),F(thumb10_05),F(thumb10_06),F(thumb10_07),  // F(10)
  F(thumb10_08),F(thumb10_09),F(thumb10_0A),F(thumb10_0B),F(thumb10_0C),F(thumb10_0D),F(thumb10_0E),F(thumb10_0F),
  F(thumb10_10),F(thumb10_11),F(thumb10_12),F(thumb10_13),F(thumb10_14),F(thumb10_15),F(thumb10_16),F(thumb10_17),
  F(thumb10_18),F(thumb10_19),F(thumb10_1A),F(thumb10_1B),F(thumb10_1C),F(thumb10_1D),F(thumb10_1E),F(thumb10_1F),
  F(thumb18_0),F(thumb18_1),F(thumb18_2),F(thumb18_3),F(thumb18_4),F(thumb18_5),F(thumb18_6),F(thumb18_7),          // F(18)
  F(thumb1A_0),F(thumb1A_1),F(thumb1A_2),F(thumb1A_3),F(thumb1A_4),F(thumb1A_5),F(thumb1A_6),F(thumb1A_7),
  F(thumb1C_0),F(thumb1C_1),F(thumb1C_2),F(thumb1C_3),F(thumb1C_4),F(thumb1C_5),F(thumb1C_6),F(thumb1C_7),
  F(thumb1E_0),F(thumb1E_1),F(thumb1E_2),F(thumb1E_3),F(thumb1E_4),F(thumb1E_5),F(thumb1E_6),F(thumb1E_7),
  F(thumb20),F(thumb20),F(thumb20),F(thumb20),F(thumb21),F(thumb21),F(thumb21),F(thumb21),  // F(20)
  F(thumb22),F(thumb22),F(thumb22),F(thumb22),F(thumb23),F(thumb23),F(thumb23),F(thumb23),
  F(thumb24),F(thumb24),F(thumb24),F(thumb24),F(thumb25),F(thumb25),F(thumb25),F(thumb25),
  F(thumb26),F(thumb26),F(thumb26),F(thumb26),F(thumb27),F(thumb27),F(thumb27),F(thumb27),
  F(thumb28),F(thumb28),F(thumb28),F(thumb28),F(thumb29),F(thumb29),F(thumb29),F(thumb29),  // F(28)
  F(thumb2A),F(thumb2A),F(thumb2A),F(thumb2A),F(thumb2B),F(thumb2B),F(thumb2B),F(thumb2B),
  F(thumb2C),F(thumb2C),F(thumb2C),F(thumb2C),F(thumb2D),F(thumb2D),F(thumb2D),F(thumb2D),
  F(thumb2E),F(thumb2E),F(thumb2E),F(thumb2E),F(thumb2F),F(thumb2F),F(thumb2F),F(thumb2F),
  F(thumb30),F(thumb30),F(thumb30),F(thumb30),F(thumb31),F(thumb31),F(thumb31),F(thumb31),  // F(30)
  F(thumb32),F(thumb32),F(thumb32),F(thumb32),F(thumb33),F(thumb33),F(thumb33),F(thumb33),
  F(thumb34),F(thumb34),F(thumb34),F(thumb34),F(thumb35),F(thumb35),F(thumb35),F(thumb35),
  F(thumb36),F(thumb36),F(thumb36),F(thumb36),F(thumb37),F(thumb37),F(thumb37),F(thumb37),
  F(thumb38),F(thumb38),F(thumb38),F(thumb38),F(thumb39),F(thumb39),F(thumb39),F(thumb39),  // F(38)
  F(thumb3A),F(thumb3A),F(thumb3A),F(thumb3A),F(thumb3B),F(thumb3B),F(thumb3B),F(thumb3B),
  F(thumb3C),F(thumb3C),F(thumb3C),F(thumb3C),F(thumb3D),F(thumb3D),F(thumb3D),F(thumb3D),
  F(thumb3E),F(thumb3E),F(thumb3E),F(thumb3E),F(thumb3F),F(thumb3F),F(thumb3F),F(thumb3F),
  F(thumb40_0),F(thumb40_1),F(thumb40_2),F(thumb40_3),F(thumb41_0),F(thumb41_1),F(thumb41_2),F(thumb41_3),  // F(40)
  F(thumb42_0),F(thumb42_1),F(thumb42_2),F(thumb42_3),F(thumb43_0),F(thumb43_1),F(thumb43_2),F(thumb43_3),
  F(thumbUI),F(thumb44_1),F(thumb44_2),F(thumb44_3),F(thumbUI),F(thumb45_1),F(thumb45_2),F(thumb45_3),
  F(thumbUI),F(thumb46_1),F(thumb46_2),F(thumb46_3),F(thumb47),F(thumb47),F(thumbUI),F(thumbUI),
  F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),  // F(48)
  F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),
  F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),
  F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),F(thumb48),
  F(thumb50),F(thumb50),F(thumb50),F(thumb50),F(thumb50),F(thumb50),F(thumb50),F(thumb50),  // F(50)
  F(thumb52),F(thumb52),F(thumb52),F(thumb52),F(thumb52),F(thumb52),F(thumb52),F(thumb52),
  F(thumb54),F(thumb54),F(thumb54),F(thumb54),F(thumb54),F(thumb54),F(thumb54),F(thumb54),
  F(thumb56),F(thumb56),F(thumb56),F(thumb56),F(thumb56),F(thumb56),F(thumb56),F(thumb56),
  F(thumb58),F(thumb58),F(thumb58),F(thumb58),F(thumb58),F(thumb58),F(thumb58),F(thumb58),  // F(58)
  F(thumb5A),F(thumb5A),F(thumb5A),F(thumb5A),F(thumb5A),F(thumb5A),F(thumb5A),F(thumb5A),
  F(thumb5C),F(thumb5C),F(thumb5C),F(thumb5C),F(thumb5C),F(thumb5C),F(thumb5C),F(thumb5C),
  F(thumb5E),F(thumb5E),F(thumb5E),F(thumb5E),F(thumb5E),F(thumb5E),F(thumb5E),F(thumb5E),
  F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),  // F(60)
  F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),
  F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),
  F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),F(thumb60),
  F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),  // F(68)
  F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),
  F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),
  F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),F(thumb68),
  F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),  // F(70)
  F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),
  F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),
  F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),F(thumb70),
  F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),  // F(78)
  F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),
  F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),
  F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),F(thumb78),
  F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),  // F(80)
  F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),
  F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),
  F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),F(thumb80),
  F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),  // F(88)
  F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),
  F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),
  F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),F(thumb88),
  F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),  // F(90)
  F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),
  F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),
  F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),F(thumb90),
  F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),  // F(98)
  F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),
  F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),
  F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),F(thumb98),
  F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),  // F(A0)
  F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),
  F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),
  F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),F(thumbA0),
  F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),  // F(A8)
  F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),
  F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),
  F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),F(thumbA8),
  F(thumbB0),F(thumbB0),F(thumbB0),F(thumbB0),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),  // F(B0)
  F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),
  F(thumbB4),F(thumbB4),F(thumbB4),F(thumbB4),F(thumbB5),F(thumbB5),F(thumbB5),F(thumbB5),
  F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),
  F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),  // F(B8)
  F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),
  F(thumbBC),F(thumbBC),F(thumbBC),F(thumbBC),F(thumbBD),F(thumbBD),F(thumbBD),F(thumbBD),
  F(thumbBP),F(thumbBP),F(thumbBP),F(thumbBP),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),
  F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),  // F(C0)
  F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),
  F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),
  F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),F(thumbC0),
  F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),  // F(C8)
  F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),
  F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),
  F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),F(thumbC8),
  F(thumbD0),F(thumbD0),F(thumbD0),F(thumbD0),F(thumbD1),F(thumbD1),F(thumbD1),F(thumbD1),  // F(D0)
  F(thumbD2),F(thumbD2),F(thumbD2),F(thumbD2),F(thumbD3),F(thumbD3),F(thumbD3),F(thumbD3),
  F(thumbD4),F(thumbD4),F(thumbD4),F(thumbD4),F(thumbD5),F(thumbD5),F(thumbD5),F(thumbD5),
  F(thumbD6),F(thumbD6),F(thumbD6),F(thumbD6),F(thumbD7),F(thumbD7),F(thumbD7),F(thumbD7),
  F(thumbD8),F(thumbD8),F(thumbD8),F(thumbD8),F(thumbD9),F(thumbD9),F(thumbD9),F(thumbD9),  // F(D8)
  F(thumbDA),F(thumbDA),F(thumbDA),F(thumbDA),F(thumbDB),F(thumbDB),F(thumbDB),F(thumbDB),
  F(thumbDC),F(thumbDC),F(thumbDC),F(thumbDC),F(thumbDD),F(thumbDD),F(thumbDD),F(thumbDD),
  F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbDF),F(thumbDF),F(thumbDF),F(thumbDF),
  F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),  // F(E0)
  F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),
  F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),
  F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),F(thumbE0),
  F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),  // F(E8)
  F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),
  F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),
  F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),F(thumbUI),
  F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),  // F(F0)
  F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),F(thumbF0),
  F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),
  F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),F(thumbF4),
  F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),  // F(F8)
  F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),
  F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),
  F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),F(thumbF8),
};

#undef F
