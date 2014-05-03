console.clear()
console.writeline("Unit test for joypad.get and joypad.set")
console.writeline("Core Required: NES (or any multi-player core with U,D,L,R,select,start,A,B as buttons)")
console.writeline("")

local test_event
local test_function
local test_frame = 0

function test_1()
  -- clear previous input
  if test_frame < 4 then
    return
  end

  local test_case = "test_1: set/get round trip"
  local test_result = false

  -- set expected input
  joypad.set({ ["P1 A"] = true, ["P1 B"] = true, ["P1 Select"] = true })

  -- get actual input
  local t = joypad.get()

  -- assert_equal
  if t["P1 A"] and t["P1 B"] and t["P1 Select"] then
    test_result = true
  end

  -- finish
  console.writeline(string.format("%s (%s): %s", test_case, test_event, test_result and "OK" or "FAILED"))
  -- if not test_result then
  --   console.writeline("Returned Value:")
  --   console.writeline(t)
  -- end

  return next_test()
end

local test_2_first = true
local test_2_start_pressed = false
function test_2()
  if test_frame < 4 then
    return
  end

  local test_case = "test_2: get/set round trip"
  if test_2_first then
    test_2_first = false

    console.writeline(test_case)
    console.writeline("Does joypad work normally?")
    console.writeline("Press 'P1 Start' if it works good.")
  end

  if test_frame == 4 then
    console.writeline(string.format("%s: running...", test_event))
  end

  -- round trip (no changes)
  local t = joypad.get()
  joypad.set(t)

  if t["P1 Start"] then
    test_2_start_pressed = true
  else
    if test_2_start_pressed then
      test_2_start_pressed = false
      next_test()
    end
  end
end

local test_3_first = true
function test_3()
  local test_case = "test_3: test if joypad.get() == movie.getinput(now)?"

  if test_3_first then
    test_3_first = false
    console.writeline(test_case)
  end

  console.writeline(string.format("%s: test code is not available yet: SKIPPED", test_event))
  next_test()
end

local tests = { test_1, test_2, test_3 }
local events = { "event.onframestart", "event.onframeend", "emu.frameadvance" }

local test_event_index = 1
local test_function_index = 1
test_event = events[1]
test_function = tests[1]

function test_reset()
  test_frame = 0
end
test_reset()

function next_test()
  test_event_index = test_event_index + 1
  if test_event_index > #events then
    console.writeline("")
    test_event_index = 1
    test_function_index = test_function_index + 1
    if test_function_index > #tests then
      console.writeline("Test Finished.")
      console.writeline("")
      error("Done.")
    end
  end
  test_event = events[test_event_index]
  test_function = tests[test_function_index]
  test_reset()
end

event.onframestart(function()
  if test_event == "event.onframestart" then
    test_function()
    test_frame = test_frame + 1
  end
end)

event.onframeend(function()
  if test_event == "event.onframeend" then
    test_function()
    test_frame = test_frame + 1
  end
end)

while true do
  if test_event == "emu.frameadvance" then
    test_function()
    test_frame = test_frame + 1
  end
  emu.frameadvance()
end
