nomalloc = environment({'MALLOC_PERTURB_': '0'})

bash = find_program('bash')
testCommands = ['run_test.sh', quickerNESTester.path(), quickNESTester.path() ]
testTimeout = 120

# Tests for copyrighted game roms (only for local testing or own CI runners)
protectedTestSet = [ 
  'arkanoid.warpless.test',
  'arkanoid.warps.test',
  'castlevania1.anyPercent.test',
  'castlevania1.pacifist.test',
  'galaga.anyPercent.test',
  'ironSword.anyPercent.test',
  'metroid.playaround.test',
  'nigelMansell.anyPercent.test',
  'ninjaGaiden.anyPercent.test',
  'ninjaGaiden.pacifist.test',
  'ninjaGaiden2.anyPercent.test',
  'ninjaGaiden2.pacifist.test',
  'novaTheSquirrel.anyPercent.test',
  'princeOfPersia.anyPercent.test',
  'rcProAmII.race1.test',
  'saintSeiyaKanketsuHen.anyPercent.test',
  'saintSeiyaOugonDensetsu.anyPercent.test',
  'saiyuukiWorld.anyPercent.test',
  'solarJetman.anyPercent.test',
  'sprilo.anyPercent.test',
  'superMarioBros.warpless.test',
  'superMarioBros.warps.test',
  'superMarioBros3.warps.test',
  'superOffroad.anyPercent.test',
  'tennis.anyPercent.test',
]

# Tests for open source free roms (for public cloud testing)
openSourceTestSet = [
  'novaTheSquirrel.anyPercent.test',
  'sprilo.anyPercent.test',
]

# Creating test set based on whether copyrighted roms are to be used
testSet = openSourceTestSet
if get_option('onlyOpenSource') == false
 testSet += protectedTestSet
endif

# Adding tests to the suite
foreach testFile : testSet
  testSuite = testFile.split('.')[0]
  testName = testFile.split('.')[1]
  test(testName,
       bash,
       workdir : meson.current_source_dir(),
       timeout: testTimeout,
       args : [ testCommands, testFile, '--cycleType', 'Full'],
       suite : [ testSuite ])
endforeach

# Special test case for castlevania 3, since it doesn't work with quickNES
if get_option('onlyOpenSource') == false
  testFile = 'castlevania3.playaround.test'
  testSuite = testFile.split('.')[0]
  testName = testFile.split('.')[1]
  test(testName,
       quickerNESTester,
       workdir : meson.current_source_dir(),
       timeout: testTimeout,
       args : [ testFile, '--cycleType', 'Full'],
       suite : [ testSuite ])
endif

# Special test cases for from-save-state start, since it doesn't work with quickNES
if get_option('onlyOpenSource') == false
  testFile = 'saiyuukiWorld.lastHalf.test'
  testSuite = testFile.split('.')[0]
  testName = testFile.split('.')[1]
  test(testName,
       quickerNESTester,
       workdir : meson.current_source_dir(),
       timeout: testTimeout,
       args : [ testFile, '--cycleType', 'Full'],
       suite : [ testSuite ])

  testFile = 'microMachines.race20.test'
  testSuite = testFile.split('.')[0]
  testName = testFile.split('.')[1]
  test(testName,
       quickerNESTester,
       workdir : meson.current_source_dir(),
       timeout: testTimeout,
       args : [ testFile, '--cycleType', 'Full'],
       suite : [ testSuite ])   

  if get_option('enableArkanoidInputs') == true

  testFile = 'arkanoid.arkNESController.test'
  testSuite = testFile.split('.')[0]
  testName = testFile.split('.')[1]
  test(testName,
       quickerNESTester,
       workdir : meson.current_source_dir(),
       timeout: testTimeout,
       args : [ testFile, '--cycleType', 'Full'],
       suite : [ testSuite ])          

  testFile = 'arkanoid2.arkFamicomController.test'
  testSuite = testFile.split('.')[0]
  testName = testFile.split('.')[1]
  test(testName,
       quickerNESTester,
       workdir : meson.current_source_dir(),
       timeout: testTimeout,
       args : [ testFile, '--cycleType', 'Full'],
       suite : [ testSuite ])              

  endif
endif