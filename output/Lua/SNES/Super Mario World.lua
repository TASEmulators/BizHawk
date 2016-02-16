---------------------------------------------------------------------------
--  Super Mario World (U) Utility Script for BizHawk
--  http://tasvideos.org/Bizhawk.html
--  
--  Author: Rodrigo A. do Amaral (Amaraticando)
--  Git repository: https://github.com/rodamaral/smw-tas
---------------------------------------------------------------------------

--#############################################################################
-- CONFIG:

local INI_CONFIG_FILENAME = "config.ini"  -- relative to the folder of the script
local OLD_EMU_VERSION

local DEFAULT_OPTIONS = {
    -- Display
    display_movie_info = false,  -- BizHawk: has good built-in movie info with custom positioning
    display_misc_info = true,
    display_player_info = true,
    display_player_hitbox = true,  -- can be changed by right-clicking on player
    display_interaction_points = true,  -- can be changed by right-clicking on player
    display_sprite_info = true,
    display_sprite_hitbox = true,  -- you still have to select the sprite with the mouse
    display_extended_sprite_info = false,
    display_cluster_sprite_info = true,
    display_minor_extended_sprite_info = true,
    display_bounce_sprite_info = true,
    display_level_info = false,
    display_yoshi_info = true,
    display_counters = true,
    display_static_camera_region = false,  -- shows the region in which the camera won't scroll horizontally
    draw_tiles_with_click = false,
    
    -- Some extra/debug info
    display_debug_info = false,  -- shows useful info while investigating the game, but not very useful while TASing
    display_debug_player_extra = true,
    display_debug_sprite_extra = true,
    display_debug_sprite_tweakers = true,
    display_debug_extended_sprite = true,
    display_debug_cluster_sprite = true,
    display_debug_minor_extended_sprite = true,
    display_debug_bounce_sprite = true,
    display_debug_controller_data = true,
    display_miscellaneous_sprite_table = false,
    miscellaneous_sprite_table_number = {[1] = true, [2] = true, [3] = true, [4] = true, [5] = true, [6] = true, [7] = true, [8] = true, [9] = true,
                    [10] = true, [11] = true, [12] = true, [13] = true, [14] = true, [15] = true, [16] = true, [17] = true, [18] = true, [19] = true
    },
    
    -- Script settings
    max_tiles_drawn = 20,  -- the max number of tiles to be drawn/registered by the script
    
    -- Lateral gaps (initial values) / bizhawk specific
    left_gap = 100,
    right_gap = 100,
    top_gap = 20,
    bottom_gap = 8,
}

-- Colour settings
local DEFAULT_COLOUR = {
    -- Text
    default_text_opacity = 1.0,
    default_bg_opacity = 0.4,
    text = "#ffffffff",
    background = "#000000ff",
    outline = "#000040ff",
    warning = "#ff0000ff",
    warning_bg = "#0000ffff",
    warning2 = "#ff00ffff",
    weak = "#a9a9a9ff",
    very_weak = "#ffffff60",
    joystick_input = "#ffff00ff",
    joystick_input_bg = "#ffffff30",
    button_text = "#300030ff",
    mainmenu_outline = "#ffffffc0",
    mainmenu_bg = "#000000c0",
    
    -- Counters
    counter_pipe = "#00ff00ff",
    counter_multicoin = "#ffff00ff",
    counter_gray_pow = "#a5a5a5ff",
    counter_blue_pow = "#4242deff",
    counter_dircoin = "#8c5a19ff",
    counter_pballoon = "#f8d870ff",
    counter_star = "#ffd773ff",
    counter_fireflower = "#ff8c00ff",
    
    -- hitbox and related text
    mario = "#ff0000ff",
    mario_bg = "#00000000",
    mario_mounted_bg = "#00000000",
    interaction = "#ffffffff",
    interaction_bg = "#00000020",
    interaction_nohitbox = "#000000a0",
    interaction_nohitbox_bg = "#00000070",
    
    sprites = {"#00ff00ff", "#0000ffff", "#ffff00ff", "#ff00ffff", "#b00040ff"},
    sprites_interaction_pts = "#ffffffff",
    sprites_bg = "#0000b050",
    sprites_clipping_bg = "#000000a0",
    extended_sprites = "#ff8000ff",
    extended_sprites_bg = "#00ff0050",
    special_extended_sprite_bg = "#00ff0060",
    goal_tape_bg = "#ffff0050",
    fireball = "#b0d0ffff",
    baseball = "#0040a0ff",
    cluster_sprites = "#ff80a0ff",
    sumo_brother_flame = "#0040a0ff",
    minor_extended_sprites = "#ff90b0ff",
    awkward_hitbox = "#204060ff",
    awkward_hitbox_bg = "#ff800060",
    
    yoshi = "#00ffffff",
    yoshi_bg = "#00ffff40",
    yoshi_mounted_bg = "#00000000",
    tongue_line = "#ffa000ff",
    tongue_bg = "#00000060",
    
    cape = "#ffd700ff",
    cape_bg = "#ffd70060",
    
    block = "#00008bff",
    blank_tile = "#ffffff70",
    block_bg = "#22cc88a0",
    layer2_line = "#ff2060ff",
    layer2_bg = "#ff206040",
    static_camera_region = "#40002040",
}

-- Font settings
local BIZHAWK_FONT_HEIGHT = 14
local BIZHAWK_FONT_WIDTH = 10

-- Symbols
local LEFT_ARROW = "<-"
local RIGHT_ARROW = "->"

-- Others
local Y_CAMERA_OFF = 1 -- small adjustment to display the tiles according to their actual graphics

-- Input key names
local INPUT_KEYNAMES = {  -- BizHawk
    
    A=false, Add=false, Alt=false, Apps=false, Attn=false, B=false, Back=false, BrowserBack=false, BrowserFavorites=false,
    BrowserForward=false, BrowserHome=false, BrowserRefresh=false, BrowserSearch=false, BrowserStop=false, C=false,
    Cancel=false, Capital=false, CapsLock=false, Clear=false, Control=false, ControlKey=false, Crsel=false, D=false, D0=false,
    D1=false, D2=false, D3=false, D4=false, D5=false, D6=false, D7=false, D8=false, D9=false, Decimal=false, Delete=false,
    Divide=false, Down=false, E=false, End=false, Enter=false, EraseEof=false, Escape=false, Execute=false, Exsel=false,
    F=false, F1=false, F10=false, F11=false, F12=false, F13=false, F14=false, F15=false, F16=false, F17=false, F18=false,
    F19=false, F2=false, F20=false, F21=false, F22=false, F23=false, F24=false, F3=false, F4=false, F5=false, F6=false,
    F7=false, F8=false, F9=false, FinalMode=false, G=false, H=false, HanguelMode=false, HangulMode=false, HanjaMode=false,
    Help=false, Home=false, I=false, IMEAccept=false, IMEAceept=false, IMEConvert=false, IMEModeChange=false,
    IMENonconvert=false, Insert=false, J=false, JunjaMode=false, K=false, KanaMode=false, KanjiMode=false, KeyCode=false,
    L=false, LaunchApplication1=false, LaunchApplication2=false, LaunchMail=false, LButton=false, LControlKey=false,
    Left=false, LineFeed=false, LMenu=false, LShiftKey=false, LWin=false, M=false, MButton=false, MediaNextTrack=false,
    MediaPlayPause=false, MediaPreviousTrack=false, MediaStop=false, Menu=false, Modifiers=false, Multiply=false, N=false,
    Next=false, NoName=false, None=false, NumLock=false, NumPad0=false, NumPad1=false, NumPad2=false, NumPad3=false,
    NumPad4=false, NumPad5=false, NumPad6=false, NumPad7=false, NumPad8=false, NumPad9=false, O=false, Oem1=false,
    Oem102=false, Oem2=false, Oem3=false, Oem4=false, Oem5=false, Oem6=false, Oem7=false, Oem8=false, OemBackslash=false,
    OemClear=false, OemCloseBrackets=false, Oemcomma=false, OemMinus=false, OemOpenBrackets=false, OemPeriod=false,
    OemPipe=false, Oemplus=false, OemQuestion=false, OemQuotes=false, OemSemicolon=false, Oemtilde=false, P=false, Pa1=false,
    Packet=false, PageDown=false, PageUp=false, Pause=false, Play=false, Print=false, PrintScreen=false, Prior=false,
    ProcessKey=false, Q=false, R=false, RButton=false, RControlKey=false, Return=false, Right=false, RMenu=false, RShiftKey=false,
    RWin=false, S=false, Scroll=false, Select=false, SelectMedia=false, Separator=false, Shift=false, ShiftKey=false,
    Sleep=false, Snapshot=false, Space=false, Subtract=false, T=false, Tab=false, U=false, Up=false, V=false, VolumeDown=false,
    VolumeMute=false, VolumeUp=false, W=false, X=false, XButton1=false, XButton2=false, Y=false, Z=false, Zoom=false
}

-- END OF CONFIG < < < < < < <
--#############################################################################
-- INITIAL STATEMENTS:


-- Load environment
local gui, input, joypad, emu, movie, memory, mainmemory, bit = gui, input, joypad, emu, movie, memory, mainmemory, bit
local unpack = unpack or table.unpack
local string, math, table, next, ipairs, pairs, io, os, type = string, math, table, next, ipairs, pairs, io, os, type

-- Script tries to verify whether the emulator is indeed BizHawk
if tastudio == nil then
    gui.text(0, 0, "This script works with BizHawk emulator.")
    error("This script works with BizHawk emulator.")
elseif gui.drawAxis == nil then
    gui.text(0, 0, "This script works with BizHawk 1.11.0 or superior.")
    gui.text(0, 16, "Your version seems to be older.")
    gui.text(0, 32, "Visit http://tasvideos.org/Bizhawk.html to download the latest version.")
    error("This script works with BizHawk 1.11.0 or superior.")
else
    OLD_EMU_VERSION = client.SetGameExtraPadding == nil
end

print("\nStarting smw-bizhawk script.")

-- TEST: INI library for handling an ini configuration file
function file_exists(name)
   local f = io.open(name, "r")
   if f ~= nil then io.close(f) return true else return false end
end

function copytable(orig)
    local orig_type = type(orig)
    local copy
    if orig_type == 'table' then
        copy = {}
        for orig_key, orig_value in next, orig, nil do
            copy[copytable(orig_key)] = copytable(orig_value) -- possible stack overflow
        end
        setmetatable(copy, copytable(getmetatable(orig)))
    else -- number, string, boolean, etc
        copy = orig
    end
    return copy
end

function mergetable(source, t2)
    for key, value in pairs(t2) do
    	if type(value) == "table" then
    		if type(source[key] or false) == "table" then
    			mergetable(source[key] or {}, t2[key] or {}) -- possible stack overflow
    		else
    			source[key] = value
    		end
    	else
    		source[key] = value
    	end
    end
    return source
end

-- Creates a set from a list
local function make_set(list)
    local set = {}
    for _, l in ipairs(list) do set[l] = true end
    return set
end

local INI = {}

function INI.arg_to_string(value)
    local str
    if type(value) == "string" then
        str = "\"" .. value .. "\""
    elseif type(value) == "number" or type(value) == "boolean" or value == nil then
        str = tostring(value)
    elseif type(value) == "table" then
        local tmp = {"{"}  -- only arrays
        for a, b in ipairs(value) do
            table.insert(tmp, ("%s%s"):format(INI.arg_to_string(b), a ~= #value and ", " or "")) -- possible stack overflow
        end
        table.insert(tmp, "}")
        str = table.concat(tmp)
    else
        str = "#BAD_VALUE"
    end
    
    return str
end

-- creates the string for ini
function INI.data_to_string(data)
	local sections = {}
    
	for section, prop in pairs(data) do
        local properties = {}
		
        for key, value in pairs(prop) do
            table.insert(properties, ("%s = %s\n"):format(key, INI.arg_to_string(value)))  -- properties
		end
        
        table.sort(properties)
        table.insert(sections, ("[%s]\n"):format(section) .. table.concat(properties) .. "\n")
	end
    
    table.sort(sections)
    return table.concat(sections)
end

function INI.string_to_data(value)
    local data
    
    if tonumber(value) then
        data = tonumber(value)
    elseif value == "true" then
        data = true
    elseif value == "false" then
        data = false
    elseif value == "nil" then
        data = nil
    else
        local quote1, text, quote2 = value:match("(['\"{])(.+)(['\"}])")  -- value is surrounded by "", '' or {}?
        if quote1 and quote2 and text then
            if (quote1 == '"' or quote1 == "'") and quote1 == quote2 then
                data = text
            elseif quote1 == "{" and quote2 == "}" then
                local tmp = {} -- test
                for words in text:gmatch("[^,%s]+") do
                    tmp[#tmp + 1] = INI.string_to_data(words) -- possible stack overflow
                end
                
                data = tmp
            else
                data = value
            end
        else
            data = value
        end
    end
    
    return data
end

function INI.load(filename)
    local file = io.open(filename, "r")
    if not file then return false end
    
    local data, section = {}, nil
    
	for line in file:lines() do
        local new_section = line:match("^%[([^%[%]]+)%]$")
		
        if new_section then
            section = INI.string_to_data(new_section) and INI.string_to_data(new_section) or new_section
            if data[section] then print("Duplicated section") end
			data[section] = data[section] or {}
        else
            
            local prop, value = line:match("^([%w_%-%.]+)%s*=%s*(.+)%s*$")  -- prop = value
            
            if prop and value then
                value = INI.string_to_data(value)
                prop = INI.string_to_data(prop) and INI.string_to_data(prop) or prop
                
                if data[section] == nil then print(prop, value) ; error("Property outside section") end
                data[section][prop] = value
            else
                local ignore = line:match("^;") or line == ""
                if not ignore then
                    print("BAD LINE:", line, prop, value)
                end
            end
            
        end
        
	end
    
	file:close()
    return data
end

function INI.retrieve(filename, data)
    if type(data) ~= "table" then error"data must be a table" end
    local data, previous_data = copytable(data), nil
    
    -- Verifies if file already exists
    if file_exists(filename) then
        ini_data = INI.load(filename)
    else return data
    end
    
    -- Adds previous values to the new ini
    local union_data = mergetable(data, ini_data)
    return union_data
end

function INI.overwrite(filename, data)
    local file, err = assert(io.open(filename, "w"), "Error loading file :" .. filename)
    if not file then print(err) ; return end
    
	file:write(INI.data_to_string(data))
	file:close()
end

function INI.save(filename, data)
    if type(data) ~= "table" then error"data must be a table" end
    
    local tmp, previous_data
    if file_exists(filename) then
        previous_data = INI.load(filename)
        tmp = mergetable(previous_data, data)
    else
        tmp = data
    end
    
    INI.overwrite(filename, tmp)
end

local function color_number(str)
    local r, g, b, a = str:match("^#(%x+%x+)(%x+%x+)(%x+%x+)(%x+%x+)$")
    if not a then print(str) return gui.color(str) end -- lsnes specific
    
    r, g, b, a = tonumber(r, 16), tonumber(g, 16), tonumber(b, 16), tonumber(a, 16)
    return 0x1000000*a + 0x10000*r + 0x100*g + b  -- BizHawk specific
end

local OPTIONS = file_exists(INI_CONFIG_FILENAME) and
    INI.retrieve(INI_CONFIG_FILENAME, {["BIZHAWK OPTIONS"] = DEFAULT_OPTIONS})["BIZHAWK OPTIONS"] or DEFAULT_OPTIONS
local COLOUR = file_exists(INI_CONFIG_FILENAME) and
    INI.retrieve(INI_CONFIG_FILENAME, {["BIZHAWK COLOURS"] = DEFAULT_COLOUR})["BIZHAWK COLOURS"] or DEFAULT_COLOUR
INI.save(INI_CONFIG_FILENAME, {["BIZHAWK COLOURS"] = COLOUR})
INI.save(INI_CONFIG_FILENAME, {["BIZHAWK OPTIONS"] = OPTIONS})

function interpret_color(data)
    for k, v in pairs(data) do
        if type(v) == "string" then
            data[k] = type(v) == "string" and color_number(v) or v
        elseif type(v) == "table" then
            interpret_color(data[k]) -- possible stack overflow
        end
    end
end
interpret_color(COLOUR)

function INI.save_options()
    INI.save(INI_CONFIG_FILENAME, {["BIZHAWK OPTIONS"] = OPTIONS})
end

--######################## -- end of test

-- Text/Background_max_opacity is only changed by the player using the hotkeys
-- Text/Bg_opacity must be used locally inside the functions
local Text_max_opacity = COLOUR.default_text_opacity
local Background_max_opacity = COLOUR.default_bg_opacity
local Text_opacity = 1
local Bg_opacity = 1

local fmt = string.format
local floor = math.floor

-- unsigned to signed (based in <bits> bits)
local function signed(num, bits)
    local maxval = 2^(bits - 1)
    if num < maxval then return num else return num - 2*maxval end
end

-- Compatibility of the memory read/write functions
local u8 =  mainmemory.read_u8
local s8 =  mainmemory.read_s8
local w8 =  mainmemory.write_u8
local u16 = mainmemory.read_u16_le
local s16 = mainmemory.read_s16_le
local w16 = mainmemory.write_u16_le
local u24 = mainmemory.read_u24_le
local s24 = mainmemory.read_s24_le
local w24 = mainmemory.write_u32_le


--#############################################################################
-- GAME AND SNES SPECIFIC MACROS:


local NTSC_FRAMERATE = 60.0988138974405
local PAL_FRAMERATE = 50.0069789081886

local SMW = {
    -- Game Modes
    game_mode_overworld = 0x0e,
    game_mode_level = 0x14,
    
    -- Sprites
    sprite_max = 12,
    extended_sprite_max = 10,
    cluster_sprite_max = 20,
    minor_extended_sprite_max = 12,
    bounce_sprite_max = 4,
    null_sprite_id = 0xff,
    
    -- Blocks
    blank_tile_map16 = 0x25,
}

WRAM = {
    -- I/O
    ctrl_1_1 = 0x0015,
    ctrl_1_2 = 0x0017,
    firstctrl_1_1 = 0x0016,
    firstctrl_1_2 = 0x0018,
    
    -- General
    game_mode = 0x0100,
    real_frame = 0x0013,
    effective_frame = 0x0014,
    lag_indicator = 0x01fe,
    timer_frame_counter = 0x0f30,
    RNG = 0x148d,
    current_level = 0x00fe,  -- plus 1
    sprite_memory_header = 0x1692,
    lock_animation_flag = 0x009d, -- Most codes will still run if this is set, but almost nothing will move or animate.
    level_mode_settings = 0x1925,
    star_road_speed = 0x1df7,
    star_road_timer = 0x1df8,
    
    -- Cheats
    frozen = 0x13fb,
    level_paused = 0x13d4,
    level_index = 0x13bf,
    room_index = 0x00ce,
    level_flag_table = 0x1ea2,
    level_exit_type = 0x0dd5,
    midway_point = 0x13ce,
    
    -- Camera
    camera_x = 0x1462,
    camera_y = 0x1464,
    screens_number = 0x005d,
    hscreen_number = 0x005e,
    vscreen_number = 0x005f,
    vertical_scroll_flag_header = 0x1412,  -- #$00 = Disable; #$01 = Enable; #$02 = Enable if flying/climbing/etc.
    vertical_scroll_enabled = 0x13f1,
    camera_scroll_timer = 0x1401,
    
    -- Sprites
    sprite_status = 0x14c8,
    sprite_number = 0x009e,
    sprite_x_high = 0x14e0,
    sprite_x_low = 0x00e4,
    sprite_y_high = 0x14d4,
    sprite_y_low = 0x00d8,
    sprite_x_sub = 0x14f8,
    sprite_y_sub = 0x14ec,
    sprite_x_speed = 0x00b6,
    sprite_y_speed = 0x00aa,
    sprite_x_offscreen = 0x15a0, 
    sprite_y_offscreen = 0x186c,
    sprite_miscellaneous1 = 0x00c2,
	sprite_miscellaneous2 = 0x1504,
	sprite_miscellaneous3 = 0x1510,
	sprite_miscellaneous4 = 0x151c,
	sprite_miscellaneous5 = 0x1528,
	sprite_miscellaneous6 = 0x1534,
	sprite_miscellaneous7 = 0x1540,
	sprite_miscellaneous8 = 0x154c,
	sprite_miscellaneous9 = 0x1558,
	sprite_miscellaneous10 = 0x1564,
	sprite_miscellaneous11 = 0x1570,
	sprite_miscellaneous12 = 0x157c,
	sprite_miscellaneous13 = 0x1594,
	sprite_miscellaneous14 = 0x15ac,
	sprite_miscellaneous15 = 0x1602,
    sprite_miscellaneous16 = 0x160e,
    sprite_miscellaneous17 = 0x1626,
    sprite_miscellaneous18 = 0x163e,
    sprite_miscellaneous19 = 0x187b,
    sprite_underwater = 0x164a,
    sprite_disable_cape = 0x1fe2,
    sprite_1_tweaker = 0x1656,
    sprite_2_tweaker = 0x1662,
    sprite_3_tweaker = 0x166e,
    sprite_4_tweaker = 0x167a,
    sprite_5_tweaker = 0x1686,
    sprite_6_tweaker = 0x190f,
    sprite_tongue_wait = 0x14a3,
    sprite_yoshi_squatting = 0x18af,
    sprite_buoyancy = 0x190e,
    
    -- Extended sprites
    extspr_number = 0x170b,
    extspr_x_high = 0x1733,
    extspr_x_low = 0x171f,
    extspr_y_high = 0x1729,
    extspr_y_low = 0x1715,
    extspr_x_speed = 0x1747,
    extspr_y_speed = 0x173d,
    extspr_suby = 0x1751,
    extspr_subx = 0x175b,
    extspr_table = 0x1765,
    extspr_table2 = 0x176f,
    
    -- Cluster sprites
    cluspr_flag = 0x18b8,
    cluspr_number = 0x1892,
    cluspr_x_high = 0x1e3e,
    cluspr_x_low = 0x1e16,
    cluspr_y_high = 0x1e2a,
    cluspr_y_low = 0x1e02,
    cluspr_timer = 0x0f9a,
    cluspr_table_1 = 0x0f4a,
    cluspr_table_2 = 0x0f72,
    cluspr_table_3 = 0x0f86,
    reappearing_boo_counter = 0x190a,
    
    -- Minor extended sprites
    minorspr_number = 0x17f0,
    minorspr_x_high = 0x18ea,
    minorspr_x_low = 0x1808,
    minorspr_y_high = 0x1814,
    minorspr_y_low = 0x17fc,
    minorspr_xspeed = 0x182c,
    minorspr_yspeed = 0x1820,
    minorspr_x_sub = 0x1844,
    minorspr_y_sub = 0x1838,
    minorspr_timer = 0x1850,
    
    -- Bounce sprites
    bouncespr_number = 0x1699,
    bouncespr_x_high = 0x16ad,
    bouncespr_x_low = 0x16a5,
    bouncespr_y_high = 0x16a9,
    bouncespr_y_low = 0x16a1,
    bouncespr_timer = 0x16c5,
    bouncespr_last_id = 0x18cd,
    turn_block_timer = 0x18ce,
    
    -- Player
    x = 0x0094,
    y = 0x0096,
    previous_x = 0x00d1,
    previous_y = 0x00d3,
    x_sub = 0x13da,
    y_sub = 0x13dc,
    x_speed = 0x007b,
    x_subspeed = 0x007a,
    y_speed = 0x007d,
    direction = 0x0076,
    is_ducking = 0x0073,
    p_meter = 0x13e4,
    take_off = 0x149f,
    powerup = 0x0019,
    cape_spin = 0x14a6,
    cape_fall = 0x14a5,
    cape_interaction = 0x13e8,
    flight_animation = 0x1407,
    diving_status = 0x1409,
    player_animation_trigger = 0x0071,
    climbing_status = 0x0074,
    spinjump_flag = 0x140d,
    player_blocked_status = 0x0077, 
    player_item = 0x0dc2, --hex
    cape_x = 0x13e9,
    cape_y = 0x13eb,
    on_ground = 0x13ef,
    on_ground_delay = 0x008d,
    on_air = 0x0072,
    can_jump_from_water = 0x13fa,
    carrying_item = 0x148f,
    mario_score = 0x0f34,
    player_coin = 0x0dbf,
    player_looking_up = 0x13de,
    
    -- Yoshi
    yoshi_riding_flag = 0x187a,  -- #$00 = No, #$01 = Yes, #$02 = Yes, and turning around.
    yoshi_tile_pos = 0x0d8c,
    
    -- Timer
    --keep_mode_active = 0x0db1,
    pipe_entrance_timer = 0x0088,
    score_incrementing = 0x13d6,
    end_level_timer = 0x1493,
    multicoin_block_timer = 0x186b, 
    gray_pow_timer = 0x14ae,
    blue_pow_timer = 0x14ad,
    dircoin_timer = 0x190c,
    pballoon_timer = 0x1891,
    star_timer = 0x1490,
    animation_timer = 0x1496,--
    invisibility_timer = 0x1497,
    fireflower_timer = 0x149b,
    yoshi_timer = 0x18e8,
    swallow_timer = 0x18ac,
    lakitu_timer = 0x18e0,
    spinjump_fireball_timer = 0x13e2,
    
    -- Layers
    layer2_x_nextframe = 0x1466,
    layer2_y_nextframe = 0x1468,
}
local WRAM = WRAM

local X_INTERACTION_POINTS = {center = 0x8, left_side = 0x2 + 1, left_foot = 0x5, right_side = 0xe - 1, right_foot = 0xb}

local Y_INTERACTION_POINTS = {
    {head = 0x10, center = 0x18, shoulder = 0x16, side = 0x1a, foot = 0x20, sprite = 0x15},
    {head = 0x08, center = 0x12, shoulder = 0x0f, side = 0x1a, foot = 0x20, sprite = 0x07},
    {head = 0x13, center = 0x1d, shoulder = 0x19, side = 0x28, foot = 0x30, sprite = 0x19},
    {head = 0x10, center = 0x1a, shoulder = 0x16, side = 0x28, foot = 0x30, sprite = 0x11}
}

local HITBOX_SPRITE = {  -- sprites' hitbox against player and other sprites
    [0x00] = { xoff = 2, yoff = 3, width = 12, height = 10, oscillation = true },
    [0x01] = { xoff = 2, yoff = 3, width = 12, height = 21, oscillation = true },
    [0x02] = { xoff = 16, yoff = -2, width = 16, height = 18, oscillation = true },
    [0x03] = { xoff = 20, yoff = 8, width = 8, height = 8, oscillation = true },
    [0x04] = { xoff = 0, yoff = -2, width = 48, height = 14, oscillation = true },
    [0x05] = { xoff = 0, yoff = -2, width = 80, height = 14, oscillation = true },
    [0x06] = { xoff = 1, yoff = 2, width = 14, height = 24, oscillation = true },
    [0x07] = { xoff = 8, yoff = 8, width = 40, height = 48, oscillation = true },
    [0x08] = { xoff = -8, yoff = -2, width = 32, height = 16, oscillation = true },
    [0x09] = { xoff = -2, yoff = 8, width = 20, height = 30, oscillation = true },
    [0x0a] = { xoff = 3, yoff = 7, width = 1, height = 2, oscillation = true },
    [0x0b] = { xoff = 6, yoff = 6, width = 3, height = 3, oscillation = true },
    [0x0c] = { xoff = 1, yoff = -2, width = 13, height = 22, oscillation = true },
    [0x0d] = { xoff = 0, yoff = -4, width = 15, height = 16, oscillation = true },
    [0x0e] = { xoff = 6, yoff = 6, width = 20, height = 20, oscillation = true },
    [0x0f] = { xoff = 2, yoff = -2, width = 36, height = 18, oscillation = true },
    [0x10] = { xoff = 0, yoff = -2, width = 15, height = 32, oscillation = true },
    [0x11] = { xoff = -24, yoff = -24, width = 64, height = 64, oscillation = true },
    [0x12] = { xoff = -4, yoff = 16, width = 8, height = 52, oscillation = true },
    [0x13] = { xoff = -4, yoff = 16, width = 8, height = 116, oscillation = true },
    [0x14] = { xoff = 4, yoff = 2, width = 24, height = 12, oscillation = true },
    [0x15] = { xoff = 0, yoff = -2, width = 15, height = 14, oscillation = true },
    [0x16] = { xoff = -4, yoff = -12, width = 24, height = 24, oscillation = true },
    [0x17] = { xoff = 2, yoff = 8, width = 12, height = 69, oscillation = true },
    [0x18] = { xoff = 2, yoff = 19, width = 12, height = 58, oscillation = true },
    [0x19] = { xoff = 2, yoff = 35, width = 12, height = 42, oscillation = true },
    [0x1a] = { xoff = 2, yoff = 51, width = 12, height = 26, oscillation = true },
    [0x1b] = { xoff = 2, yoff = 67, width = 12, height = 10, oscillation = true },
    [0x1c] = { xoff = 0, yoff = 10, width = 10, height = 48, oscillation = true },
    [0x1d] = { xoff = 2, yoff = -3, width = 28, height = 27, oscillation = true },
    [0x1e] = { xoff = 6, yoff = -8, width = 3, height = 32, oscillation = true },  -- default: { xoff = -32, yoff = -8, width = 48, height = 32, oscillation = true },
    [0x1f] = { xoff = -16, yoff = -4, width = 48, height = 18, oscillation = true },
    [0x20] = { xoff = -4, yoff = -24, width = 8, height = 24, oscillation = true },
    [0x21] = { xoff = -4, yoff = 16, width = 8, height = 24, oscillation = true },
    [0x22] = { xoff = 0, yoff = 0, width = 16, height = 16, oscillation = true },
    [0x23] = { xoff = -8, yoff = -24, width = 32, height = 32, oscillation = true },
    [0x24] = { xoff = -12, yoff = 32, width = 56, height = 56, oscillation = true },
    [0x25] = { xoff = -14, yoff = 4, width = 60, height = 20, oscillation = true },
    [0x26] = { xoff = 0, yoff = 88, width = 32, height = 8, oscillation = true },
    [0x27] = { xoff = -4, yoff = -4, width = 24, height = 24, oscillation = true },
    [0x28] = { xoff = -14, yoff = -24, width = 28, height = 40, oscillation = true },
    [0x29] = { xoff = -16, yoff = -4, width = 32, height = 27, oscillation = true },
    [0x2a] = { xoff = 2, yoff = -8, width = 12, height = 19, oscillation = true },
    [0x2b] = { xoff = 0, yoff = 2, width = 16, height = 76, oscillation = true },
    [0x2c] = { xoff = -8, yoff = -8, width = 16, height = 16, oscillation = true },
    [0x2d] = { xoff = 4, yoff = 4, width = 8, height = 4, oscillation = true },
    [0x2e] = { xoff = 2, yoff = -2, width = 28, height = 34, oscillation = true },
    [0x2f] = { xoff = 2, yoff = -2, width = 28, height = 32, oscillation = true },
    [0x30] = { xoff = 8, yoff = -14, width = 16, height = 28, oscillation = true },
    [0x31] = { xoff = 0, yoff = -2, width = 48, height = 18, oscillation = true },
    [0x32] = { xoff = 0, yoff = -2, width = 48, height = 18, oscillation = true },
    [0x33] = { xoff = 0, yoff = -2, width = 64, height = 18, oscillation = true },
    [0x34] = { xoff = -4, yoff = -4, width = 8, height = 8, oscillation = true },
    [0x35] = { xoff = 3, yoff = 0, width = 18, height = 32, oscillation = true },
    [0x36] = { xoff = 8, yoff = 8, width = 52, height = 46, oscillation = true },
    [0x37] = { xoff = 0, yoff = -8, width = 15, height = 20, oscillation = true },
    [0x38] = { xoff = 8, yoff = 16, width = 32, height = 40, oscillation = true },
    [0x39] = { xoff = 4, yoff = 3, width = 8, height = 10, oscillation = true },
    [0x3a] = { xoff = -8, yoff = 16, width = 32, height = 16, oscillation = true },
    [0x3b] = { xoff = 0, yoff = 0, width = 16, height = 13, oscillation = true },
    [0x3c] = { xoff = 12, yoff = 10, width = 3, height = 6, oscillation = true },
    [0x3d] = { xoff = 12, yoff = 21, width = 3, height = 20, oscillation = true },
    [0x3e] = { xoff = 16, yoff = 18, width = 254, height = 16, oscillation = true },
    [0x3f] = { xoff = 8, yoff = 8, width = 8, height = 24, oscillation = true }
}

local OBJ_CLIPPING_SPRITE = {  -- sprites' interaction points against objects
    [0x0] = {xright = 14, xleft =  2, xdown =  8, xup =  8, yright =  8, yleft =  8, ydown = 16, yup =  2},
    [0x1] = {xright = 14, xleft =  2, xdown =  7, xup =  7, yright = 18, yleft = 18, ydown = 32, yup =  2},
    [0x2] = {xright =  7, xleft =  7, xdown =  7, xup =  7, yright =  7, yleft =  7, ydown =  7, yup =  7},
    [0x3] = {xright = 14, xleft =  2, xdown =  8, xup =  8, yright = 16, yleft = 16, ydown = 32, yup = 11},
    [0x4] = {xright = 16, xleft =  0, xdown =  8, xup =  8, yright = 18, yleft = 18, ydown = 32, yup =  2},
    [0x5] = {xright = 13, xleft =  2, xdown =  8, xup =  8, yright = 24, yleft = 24, ydown = 32, yup = 16},
    [0x6] = {xright =  7, xleft =  0, xdown =  4, xup =  4, yright =  4, yleft =  4, ydown =  8, yup =  0},
    [0x7] = {xright = 31, xleft =  1, xdown = 16, xup = 16, yright = 16, yleft = 16, ydown = 31, yup =  1},
    [0x8] = {xright = 15, xleft =  0, xdown =  8, xup =  8, yright =  8, yleft =  8, ydown = 15, yup =  0},
    [0x9] = {xright = 16, xleft =  0, xdown =  8, xup =  8, yright =  8, yleft =  8, ydown = 16, yup =  0},
    [0xa] = {xright = 13, xleft =  2, xdown =  8, xup =  8, yright = 72, yleft = 72, ydown = 80, yup = 66},
    [0xb] = {xright = 14, xleft =  2, xdown =  8, xup =  8, yright =  4, yleft =  4, ydown =  8, yup =  0},
    [0xc] = {xright = 13, xleft =  2, xdown =  8, xup =  8, yright =  0, yleft =  0, ydown =  0, yup =  0},
    [0xd] = {xright = 16, xleft =  0, xdown =  8, xup =  8, yright =  8, yleft =  8, ydown = 16, yup =  0},
    [0xe] = {xright = 31, xleft =  0, xdown = 16, xup = 16, yright =  8, yleft =  8, ydown = 16, yup =  0},
    [0xf] = {xright =  8, xleft =  8, xdown =  8, xup = 16, yright =  4, yleft =  1, ydown =  2, yup =  4}
}

local HITBOX_EXTENDED_SPRITE = {  -- extended sprites' hitbox
    -- To fill the slots...
    --[0] ={ xoff = 3, yoff = 3, width = 64, height = 64},  -- Free slot
    [0x01] ={ xoff = 3, yoff = 3, width =  0, height =  0},  -- Puff of smoke with various objects
    [0x0e] ={ xoff = 3, yoff = 3, width =  0, height =  0},  -- Wiggler's flower
    [0x0f] ={ xoff = 3, yoff = 3, width =  0, height =  0},  -- Trail of smoke -- TODO: add experimental hitbox
    [0x10] ={ xoff = 3, yoff = 3, width =  0, height =  0},  -- Spinjump stars
    [0x12] ={ xoff = 3, yoff = 3, width =  0, height =  0},  -- Water bubble
    -- extracted from ROM:
    [0x02] = { xoff = 3, yoff = 3, width = 1, height = 1, color_line = COLOUR.fireball },  -- Reznor fireball
    [0x03] = { xoff = 3, yoff = 3, width = 1, height = 1, color_line = COLOUR.fireball},  -- Flame left by hopping flame
    [0x04] = { xoff = 4, yoff = 4, width = 8, height = 8},  -- Hammer
    [0x05] = { xoff = 3, yoff = 3, width = 1, height = 1, color_line = COLOUR.fireball },  -- Player fireball
    [0x06] = { xoff = 4, yoff = 4, width = 8, height = 8},  -- Bone from Dry Bones
    [0x07] = { xoff = 0, yoff = 0, width = 0, height = 0},  -- Lava splash
    [0x08] = { xoff = 0, yoff = 0, width = 0, height = 0},  -- Torpedo Ted shooter's arm
    [0x09] = { xoff = 0, yoff = 0, width = 15, height = 15},  -- Unknown flickering object
    [0x0a] = { xoff = 4, yoff = 2, width = 8, height = 12},  -- Coin from coin cloud game
    [0x0b] = { xoff = 3, yoff = 3, width = 1, height = 1, color_line = COLOUR.fireball },  -- Piranha Plant fireball
    [0x0c] = { xoff = 3, yoff = 3, width = 1, height = 1, color_line = COLOUR.fireball },  -- Lava Lotus's fiery objects
    [0x0d] = { xoff = 3, yoff = 3, width = 1, height = 1, color_line = COLOUR.baseball },  -- Baseball
    -- got experimentally:
    [0x11] = { xoff = -0x1, yoff = -0x4, width = 11, height = 19},  -- Yoshi fireballs
}

local HITBOX_CLUSTER_SPRITE = {  -- got experimentally
    --[0] -- Free slot
    [0x01] = { xoff = 2, yoff = 0, width = 17, height = 21, oscillation = 2, phase = 1, color = COLOUR.awkward_hitbox, bg = COLOUR.awkward_hitbox_bg},  -- 1-Up from bonus game (glitched hitbox area)
    [0x02] = { xoff = 4, yoff = 7, width = 7, height = 7, oscillation = 4},  -- Unused
    [0x03] = { xoff = 4, yoff = 7, width = 7, height = 7, oscillation = 4},  -- Boo from Boo Ceiling
    [0x04] = { xoff = 4, yoff = 7, width = 7, height = 7, oscillation = 4},  -- Boo from Boo Ring
    [0x05] = { xoff = 4, yoff = 7, width = 7, height = 7, oscillation = 4},  -- Castle candle flame (meaningless hitbox)
    [0x06] = { xoff = 2, yoff = 2, width = 12, height = 20, oscillation = 4, color = COLOUR.sumo_brother_flame},  -- Sumo Brother lightning flames
    [0x07] = { xoff = 4, yoff = 7, width = 7, height = 7, oscillation = 4},  -- Reappearing Boo
    [0x08] = { xoff = 4, yoff = 7, width = 7, height = 7, oscillation = 4},  -- Swooper bat from Swooper Death Bat Ceiling (untested)
}

;                              -- 0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f  10 11 12
local SPRITE_MEMORY_MAX = {[0] = 10, 6, 7, 6, 7, 5, 8, 5, 7, 9, 9, 4, 8, 6, 8, 9, 10, 6, 6}  -- the max of sprites in a room

-- Creates a set from a list
local function make_set(list)
    local set = {}
    for _, l in ipairs(list) do set[l] = true end
    return set
end

-- from sprite number, returns oscillation flag
-- A sprite must be here iff it processes interaction with player every frame AND this bit is not working in the sprite_4_tweaker WRAM(0x167a)
local OSCILLATION_SPRITES = make_set{0x0e, 0x21, 0x29, 0x35, 0x54, 0x74, 0x75, 0x76, 0x77, 0x78, 0x81, 0x83, 0x87}

-- Sprites that have a custom hitbox drawing
local ABNORMAL_HITBOX_SPRITES = make_set{0x62, 0x63, 0x6b, 0x6c}

-- Sprites whose clipping interaction points usually matter
local GOOD_SPRITES_CLIPPING = make_set{
0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xf, 0x10, 0x11, 0x13, 0x14, 0x18,
0x1b, 0x1d, 0x1f, 0x20, 0x26, 0x27, 0x29, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30, 0x31,
0x32, 0x34, 0x35, 0x3d, 0x3e, 0x3f, 0x40, 0x46, 0x47, 0x48, 0x4d, 0x4e,
0x51, 0x53, 0x6e, 0x6f, 0x70, 0x80, 0x81, 0x86, 
0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9a, 0xa1, 0xa2, 0xa5, 0xa6, 0xa7, 0xab, 0xb2,
0xb4, 0xbb, 0xbc, 0xbd, 0xbf, 0xc3, 0xda, 0xdb, 0xdc, 0xdd, 0xdf
}

-- Extended sprites that don't interact with the player
local UNINTERESTING_EXTENDED_SPRITES = make_set{1, 7, 8, 0x0e, 0x10, 0x12}


--#############################################################################
-- SCRIPT UTILITIES:


-- Variables used in various functions
local Cheat = {}  -- family of cheat functions and variables
local Previous = {}
local User_input = INPUT_KEYNAMES -- BizHawk
local Joypad = {}
local Layer1_tiles = {}
local Layer2_tiles = {}
local Is_lagged = nil
local Mario_boost_indicator = nil
local Show_player_point_position = false
local Sprites_info = {}  -- keeps track of useful sprite info that might be used outside the main sprite function
local Sprite_hitbox = {}  -- keeps track of what sprite slots must display the hitbox
local Options_form = {}  -- BizHawk
local Bizhawk_loop_counter = 1  -- BizHawk specific, a hack for saving the ini regularly

-- Initialization of some tables
for i = 0, SMW.sprite_max -1 do
    Sprites_info[i] = {}
end
for key = 0, SMW.sprite_max - 1 do
    Sprite_hitbox[key] = {}
    for number = 0, 0xff do
        Sprite_hitbox[key][number] = {["sprite"] = true, ["block"] = GOOD_SPRITES_CLIPPING[number]}
    end
end


local function copytable(orig)
    local orig_type = type(orig)
    local copy
    if orig_type == 'table' then
        copy = {}
        for orig_key, orig_value in pairs(orig) do
            copy[orig_key] = orig_value
        end
    else -- number, string, boolean, etc
        copy = orig
    end
    return copy
end


-- Sum of the digits of a integer
local function sum_digits(number)
    local sum = 0
    while number > 0 do
        sum = sum + number%10
        number = floor(number*0.1)
    end
    
    return sum
end


-- Transform the binary representation of base into a string
-- For instance, if each bit of a number represents a char of base, then this function verifies what chars are on
local function decode_bits(data, base)
    local i = 1
    local size = base:len()
    local direct_concatenation = size <= 45  -- Performance: I found out that the .. operator is faster for 45 operations or less
    local result
    
    if direct_concatenation then
        result = ""
        for ch in base:gmatch(".") do
            if bit.test(data, size - i) then
                result = result .. ch
            else
                result = result .. " "
            end
            i = i + 1
        end
    else
        result = {}
        for ch in base:gmatch(".") do
            if bit.test(data, size-i) then
                result[i] = ch
            else
                result[i] = " "
            end
            i = i + 1
        end
        result = table.concat(result)
    end
    
    return result
end


bit.test = bit.check  -- BizHawk


-- Register a function to be executed on key press or release
-- execution happens in the main loop
local Keys = {}
Keys.press = {}
Keys.release = {}
Keys.down, Keys.up, Keys.pressed, Keys.released = {}, {}, {}, {}
function Keys.registerkeypress(key, fn)
    Keys.press[key] = fn
end
function Keys.registerkeyrelease(key, fn)
    Keys.release[key] = fn
end


-- A cross sign with pos and size
gui.crosshair = gui.drawAxis


local Movie_active, Readonly, Framecount, Lagcount, Rerecords, Game_region
local Lastframe_emulated, Starting_subframe_last_frame, Size_last_frame, Final_subframe_last_frame
local Nextframe, Starting_subframe_next_frame, Starting_subframe_next_frame, Final_subframe_next_frame
local function bizhawk_status()
    Movie_active = movie.isloaded()  -- BizHawk
    Readonly = movie.getreadonly()  -- BizHawk
    Framecount = movie.length()  -- BizHawk
    Lagcount = emu.lagcount()  -- BizHawk
    Rerecords = movie.getrerecordcount()  -- BizHawk
    Is_lagged = emu.islagged()  -- BizHawk
    Game_region = emu.getdisplaytype()  -- BizHawk
    
    -- Last frame info
    Lastframe_emulated = emu.framecount()
    
    -- Next frame info (only relevant in readonly mode)
    Nextframe = Lastframe_emulated + 1
end


-- Get screen values of the game and emulator areas
local Left_gap, Right_gap, Top_gap, Bottom_gap
local Border_left, Border_right, Border_top, Border_bottom
local Buffer_width, Buffer_height, Buffer_middle_x, Buffer_middle_y
local Screen_width, Screen_height, AR_x, AR_y
local function bizhawk_screen_info()
    -- zero gaps in old versions
    if OLD_EMU_VERSION then
        Left_gap = 0
        Top_gap = 0
        Right_gap = 0
        Bottom_gap = 0
    else
        Left_gap = OPTIONS.left_gap
        Top_gap = OPTIONS.top_gap
        Right_gap = OPTIONS.right_gap
        Bottom_gap = OPTIONS.bottom_gap
    end
    
	Screen_width = client.screenwidth()  -- Screen area
	Screen_height = client.screenheight()
    Buffer_width = client.bufferwidth()  -- Game area
    Buffer_height = client.bufferheight()
    Border_left = client.borderwidth()  -- Borders' dimensions
    Border_top = client.borderheight()
    
    -- BizHawk bug: buffer dimensions go crazy when emu is minimized
    if Buffer_width == 0 then
        Buffer_width, Screen_width = 256, 256
        Buffer_height, Screen_height = 224, 224
        Border_left, Border_top = 0, 0
    end
    
    -- Derived dimensions
    Buffer_middle_x = floor(Buffer_width/2)
    Buffer_middle_y = floor(Buffer_height/2)
    Border_right = Screen_width - Buffer_width - Border_left
    Border_bottom = Screen_height - Buffer_height - Border_top
    AR_x = Buffer_width/256
	AR_y = Buffer_height/224
end


local function mouse_onregion(x1, y1, x2, y2)
    -- Reads external mouse coordinates
    local mouse_x = User_input.xmouse*AR_x
    local mouse_y = User_input.ymouse*AR_y
    
    -- From top-left to bottom-right
    if x2 < x1 then
        x1, x2 = x2, x1
    end
    if y2 < y1 then
        y1, y2 = y2, y1
    end
    
    if mouse_x >= x1 and mouse_x <= x2 and  mouse_y >= y1 and mouse_y <= y2 then
        return true
    else
        return false
    end
end


-- draw a pixel given (x,y) with SNES' pixel sizes
local draw_pixel = function(x, y, color) gui.drawPixel(x + Left_gap, y + Top_gap, color) end


-- draws a line given (x,y) and (x',y') with given scale and SNES' pixel thickness (whose scale is 2)
local function draw_line(x1, y1, x2, y2, scale, color)
    -- Draw from top-left to bottom-right
    if x2 < x1 then
        x1, x2 = x2, x1
    end
    if y2 < y1 then
        y1, y2 = y2, y1
    end
    
    x1, y1, x2, y2 = scale*x1, scale*y1, scale*x2, scale*y2
    gui.drawLine(x1 + Left_gap, y1 + Top_gap, x2 + Left_gap, y2 + Top_gap, color)
end


-- draws a box given (x,y) and (x',y') with SNES' pixel sizes
local draw_box = function(x1, y1, x2, y2, line, bg) gui.drawBox(x1 + Left_gap, y1 + Top_gap, x2 + Left_gap, y2 + Top_gap, line, bg) end


-- draws a rectangle given (x,y) and dimensions, with SNES' pixel sizes
local draw_rectangle = function(x, y, w, h, line, bg) gui.drawRectangle(x + Left_gap, y + Top_gap, w, h, line, bg) end


-- Changes transparency of a color: result is opaque original * transparency level (0.0 to 1.0).
local function change_transparency(color, transparency)
    -- Sane transparency
    if transparency >= 1 then return color end  -- no transparency
    if transparency <= 0 then return 0 end    -- total transparency
    
    -- Sane colour
    if color == 0 then return 0 end
    if type(color) ~= "number" then
        print(color)
        error"Wrong color"
    end
    
    local a = floor(color/0x1000000)
    local rgb = color - a*0x1000000
    local new_a = floor(a*transparency)
    return new_a*0x1000000 + rgb
end


-- returns the (x, y) position to start the text and its length:
-- number, number, number text_position(x, y, text, font_width, font_height[[[[, always_on_client], always_on_game], ref_x], ref_y])
-- x, y: the coordinates that the refereed point of the text must have
-- text: a string, don't make it bigger than the buffer area width and don't include escape characters
-- font_width, font_height: the sizes of the font
-- always_on_client, always_on_game: boolean
-- ref_x and ref_y: refer to the relative point of the text that must occupy the origin (x,y), from 0% to 100%
--                  for instance, if you want to display the middle of the text in (x, y), then use 0.5, 0.5
local function text_position(x, y, text, font_width, font_height, always_on_client, always_on_game, ref_x, ref_y)
    -- Reads external variables
    local border_left     = Border_left
    local border_right    = Border_right
    local border_top      = Border_top
    local border_bottom   = Border_bottom
    local buffer_width    = Buffer_width
    local buffer_height   = Buffer_height
    
    -- text processing
    local text_length = text and string.len(text)*font_width or font_width  -- considering another objects, like bitmaps
    
    -- actual position, relative to game area origin
    x = (not ref_x and x) or (ref_x == 0 and x) or x - floor(text_length*ref_x)
    y = (not ref_y and y) or (ref_y == 0 and y) or y - floor(font_height*ref_y)
    
    -- adjustment needed if text is supposed to be on screen area
    local x_end = x + text_length
    local y_end = y + font_height
    
    if always_on_game then
        if x < 0 then x = 0 end
        if y < 0 then y = 0 end
        
        if x_end > buffer_width  then x = buffer_width  - text_length end
        if y_end > buffer_height then y = buffer_height - font_height end
        
    elseif always_on_client then
        if x < -border_left then x = -border_left end
        if y < -border_top  then y = -border_top  end
        
        if x_end > buffer_width  + border_right  then x = buffer_width  + border_right  - text_length end
        if y_end > buffer_height + border_bottom then y = buffer_height + border_bottom - font_height end
    end
    
    return x, y, text_length
end


-- Complex function for drawing, that uses text_position
local function draw_text(x, y, text, ...)
    -- Reads external variables
    local font_name = Font or false
    local font_width  = BIZHAWK_FONT_WIDTH
    local font_height = BIZHAWK_FONT_HEIGHT
    local bg_default_color = font_name and COLOUR.outline or COLOUR.background
    local text_color, bg_color, always_on_client, always_on_game, ref_x, ref_y
    local arg1, arg2, arg3, arg4, arg5, arg6 = ...
    
    if not arg1 or arg1 == true then
        
        text_color = COLOUR.text
        bg_color = bg_default_color
        always_on_client, always_on_game, ref_x, ref_y = arg1, arg2, arg3, arg4
        
    elseif not arg2 or arg2 == true then
        
        text_color = arg1
        bg_color = bg_default_color
        always_on_client, always_on_game, ref_x, ref_y = arg2, arg3, arg4, arg5
        
    else
        
        text_color, bg_color = arg1, arg2
        always_on_client, always_on_game, ref_x, ref_y = arg3, arg4, arg5, arg6
        
    end
    
    local x_pos, y_pos, length = text_position(x, y, text, font_width, font_height,
                                    always_on_client, always_on_game, ref_x, ref_y)
    ;
    
    text_color = change_transparency(text_color, Text_max_opacity * Text_opacity)
    bg_color = change_transparency(bg_color, Text_max_opacity * Text_opacity)
    gui.text(x_pos + Border_left, y_pos + Border_top, text, OLD_EMU_VERSION and bg_color or text_color, OLD_EMU_VERSION and text_color or bg_color)
    
    return x_pos + length, y_pos + font_height, length
end


local function alert_text(x, y, text, text_color, bg_color, always_on_game, ref_x, ref_y)
    -- Reads external variables
    local font_width  = BIZHAWK_FONT_WIDTH
    local font_height = BIZHAWK_FONT_HEIGHT
    
    local x_pos, y_pos, text_length = text_position(x, y, text, font_width, font_height, false, always_on_game, ref_x, ref_y)
    
    if not bg_color then bg_color = BACKGROUND_COLOR end
    text_color = change_transparency(text_color, Text_max_opacity * Text_opacity)
    bg_color = change_transparency(bg_color, Background_max_opacity * Bg_opacity)
    
    draw_box(x_pos/AR_x, y_pos/AR_y, (x_pos + text_length)/AR_x + 2, (y_pos + font_height)/AR_y + 1, 0, bg_color)
    gui.text(x_pos + Border_left, y_pos + Border_top, text, OLD_EMU_VERSION and 0 or text_color, OLD_EMU_VERSION and text_color or 0)
end


local function draw_over_text(x, y, value, base, color_base, color_value, color_bg, always_on_client, always_on_game, ref_x, ref_y)
    value = decode_bits(value, base)
    local x_end, y_end, length = draw_text(x, y, base,  color_base, color_bg, always_on_client, always_on_game, ref_x, ref_y)
    
    change_transparency(color_value or COLOUR.text, Text_max_opacity * Text_opacity)
    gui.text(x_end + Border_left - length, y_end + Border_top - BIZHAWK_FONT_HEIGHT, value,
        OLD_EMU_VERSION and 0 or color_value, OLD_EMU_VERSION and color_value or 0)  -- BizHawk
    ;
    
    return x_end, y_end, length
end


-- Returns frames-time conversion
local function frame_time(frame)
    local total_seconds = frame/(Game_region == "NTSC" and NTSC_FRAMERATE or PAL_FRAMERATE)
    local hours = floor(total_seconds/3600)
    local tmp = total_seconds - 3600*hours
    local minutes = floor(tmp/60)
    tmp = tmp - 60*minutes
    local seconds = floor(tmp)
    
    local miliseconds = 1000* (total_seconds%1)
    if hours == 0 then hours = "" else hours = string.format("%d:", hours) end
    local str = string.format("%s%.2d:%.2d.%03.0f", hours, minutes, seconds, miliseconds)
    return str
end


-- Background opacity functions
local function increase_opacity()
    if Text_max_opacity <= 0.9 then Text_max_opacity = Text_max_opacity + 0.1
    else
        if Background_max_opacity <= 0.9 then Background_max_opacity = Background_max_opacity + 0.1 end
    end
end


local function decrease_opacity()
    if  Background_max_opacity >= 0.1 then Background_max_opacity = Background_max_opacity - 0.1
    else
        if Text_max_opacity >= 0.1 then Text_max_opacity = Text_max_opacity - 0.1 end
    end
end



--#############################################################################
-- SMW FUNCTIONS:


-- Returns the id of Yoshi; if more than one, the lowest sprite slot
local function get_yoshi_id()
    for i = 0, SMW.sprite_max - 1 do
        local id = u8(WRAM.sprite_number + i)
        local status = u8(WRAM.sprite_status + i)
        if id == 0x35 and status ~= 0 then return i end
    end
    
    return nil
end


local Real_frame, Previous_real_frame, Effective_frame, Game_mode
local Level_index, Room_index, Level_flag, Current_level
local Is_paused, Lock_animation_flag, Player_powerup, Player_animation_trigger
local Camera_x, Camera_y
local function scan_smw()
    Previous_real_frame = Real_frame or u8(WRAM.real_frame)
    Real_frame = u8(WRAM.real_frame)
    Effective_frame = u8(WRAM.effective_frame)
    Game_mode = u8(WRAM.game_mode)
    Level_index = u8(WRAM.level_index)
    Level_flag = u8(WRAM.level_flag_table + Level_index)
    Is_paused = u8(WRAM.level_paused) == 1
    Lock_animation_flag = u8(WRAM.lock_animation_flag)
    Room_index = u24(WRAM.room_index)
    
    -- In level frequently used info
    Player_animation_trigger = u8(WRAM.player_animation_trigger)
    Player_powerup = u8(WRAM.powerup)
    Camera_x = s16(WRAM.camera_x)
    Camera_y = s16(WRAM.camera_y)
    Yoshi_riding_flag = u8(WRAM.yoshi_riding_flag) ~= 0
    Yoshi_id = get_yoshi_id()
end


-- Converts the in-game (x, y) to SNES-screen coordinates
local function screen_coordinates(x, y, camera_x, camera_y)
    -- Sane values
    camera_x = camera_x or Camera_x or u8(WRAM.camera_x)
    camera_y = camera_y or Camera_y or u8(WRAM.camera_y)
    
    local x_screen = (x - camera_x)
    local y_screen = (y - camera_y) - Y_CAMERA_OFF
    
    return x_screen, y_screen
end


-- Converts BizHawk/emu-screen coordinates to in-game (x, y)
local function game_coordinates(x_emu, y_emu, camera_x, camera_y)
    -- Sane values
    camera_x = camera_x or Camera_x or u8(WRAM.camera_x)
    camera_y = camera_y or Camera_y or u8(WRAM.camera_y)
    
    local x_game = x_emu + camera_x
    local y_game = y_emu + Y_CAMERA_OFF + camera_y
    
    return x_game, y_game
end


-- Returns the extreme values that Mario needs to have in order to NOT touch a rectangular object
local function display_boundaries(x_game, y_game, width, height, camera_x, camera_y)
    -- Font
    Text_opacity = 0.6
    Bg_opacity = 0.4
    
    -- Coordinates around the rectangle
    local left = width*floor(x_game/width)
    local top = height*floor(y_game/height)
    left, top = screen_coordinates(left, top, camera_x, camera_y)
    local right = left + width - 1
    local bottom = top + height - 1
    
    -- Reads WRAM values of the player
    local is_ducking = u8(WRAM.is_ducking)
    local powerup = Player_powerup
    local is_small = is_ducking ~= 0 or powerup == 0
    
    -- Left
    local left_text = string.format("%4d.0", width*floor(x_game/width) - 13)
    draw_text(AR_x*left, AR_y*(top+bottom)/2, left_text, false, false, 1.0, 0.5)
    
    -- Right
    local right_text = string.format("%d.f", width*floor(x_game/width) + 12)
    draw_text(AR_x*right, AR_y*(top+bottom)/2, right_text, false, false, 0.0, 0.5)
    
    -- Top
    local value = (Yoshi_riding_flag and y_game - 16) or y_game
    local top_text = fmt("%d.0", width*floor(value/width) - 32)
    draw_text(AR_x*(left+right)/2, AR_y*top, top_text, false, false, 0.5, 1.0)
    
    -- Bottom
    value = height*floor(y_game/height)
    if not is_small and not Yoshi_riding_flag then
        value = value + 0x07
    elseif is_small and Yoshi_riding_flag then
        value = value - 4
    else
        value = value - 1  -- the 2 remaining cases are equal
    end
    
    local bottom_text = fmt("%d.f", value)
    draw_text(AR_x*(left+right)/2, AR_y*bottom, bottom_text, false, false, 0.5, 0.0)
    
    return left, top
end


local function read_screens()
	local screens_number = u8(WRAM.screens_number)
    local vscreen_number = u8(WRAM.vscreen_number)
    local hscreen_number = u8(WRAM.hscreen_number) - 1
    local vscreen_current = s8(WRAM.y + 1)
    local hscreen_current = s8(WRAM.x + 1)
    local level_mode_settings = u8(WRAM.level_mode_settings)
    --local b1, b2, b3, b4, b5, b6, b7, b8 = bit.multidiv(level_mode_settings, 128, 64, 32, 16, 8, 4, 2)
    --draw_text(Buffer_middle_x, Buffer_middle_y, {"%x: %x%x%x%x%x%x%x%x", level_mode_settings, b1, b2, b3, b4, b5, b6, b7, b8}, COLOUR.text, COLOUR.background)
    
    local level_type
    if (level_mode_settings ~= 0) and (level_mode_settings == 0x3 or level_mode_settings == 0x4 or level_mode_settings == 0x7
        or level_mode_settings == 0x8 or level_mode_settings == 0xa or level_mode_settings == 0xd) then
            level_type = "Vertical"
        ;
    else
        level_type = "Horizontal"
    end
    
    return level_type, screens_number, hscreen_current, hscreen_number, vscreen_current, vscreen_number
end


local function get_map16_value(x_game, y_game)
    local num_x = floor(x_game/16)
    local num_y = floor(y_game/16)
    if num_x < 0 or num_y < 0 then return end  -- 1st breakpoint
    
    local level_type, screens, _, hscreen_number, _, vscreen_number = read_screens()
    local max_x, max_y
    if level_type == "Horizontal" then
        max_x = 16*(hscreen_number + 1)
        max_y = 27
    else
        max_x = 32
        max_y = 16*(vscreen_number + 1)
    end
    
    if num_x > max_x or num_y > max_y then return end  -- 2nd breakpoint
    
    local num_id, kind, address
    if level_type == "Horizontal" then
        num_id = 16*27*floor(num_x/16) + 16*num_y + num_x%16
    else
        local nx = floor(num_x/16)
        local ny = floor(num_y/16)
        local n = 2*ny + nx
        num_id = 16*16*n + 16*(num_y%16) + num_x%16
    end
    if (num_id >= 0 and num_id <= 0x37ff) then
        address = fmt(" $%4.x", 0xc800 + num_id)
        kind = 256*u8(0x1c800 + num_id) + u8(0xc800 + num_id)
    end
    
    if kind then return  num_x, num_y, kind, address end
end


local function draw_layer1_tiles(camera_x, camera_y)
    local x_origin, y_origin = screen_coordinates(0, 0, camera_x, camera_y)
    local x_mouse, y_mouse = game_coordinates(User_input.xmouse, User_input.ymouse, camera_x, camera_y)
    x_mouse = 16*floor(x_mouse/16)
    y_mouse = 16*floor(y_mouse/16)
    local push_direction = Real_frame%2 == 0 and 0 or 7  -- block pushes sprites to left or right?
    
    for number, positions in ipairs(Layer1_tiles) do
        -- Calculate the Lsnes coordinates
        local left = positions[1] + x_origin
        local top = positions[2] + y_origin
        local right = left + 15
        local bottom = top + 15
        local x_game, y_game = game_coordinates(left, top, camera_x, camera_y)
        
        -- Returns if block is way too outside the screen
        if left > - Border_left - 32 and top  > - Border_top - 32 and
        right < Screen_width  + Border_right + 32 and bottom < Screen_height + Border_bottom + 32 then
            
            -- Drawings
            Text_opacity = 1.0
            local num_x, num_y, kind, address = get_map16_value(x_game, y_game)
            if kind then
                if kind >= 0x111 and kind <= 0x16d or kind == 0x2b then
                    -- default solid blocks, don't know how to include custom blocks
                    draw_rectangle(left + push_direction, top, 8, 15, 0, COLOUR.block_bg)
                end
                draw_rectangle(left, top, 15, 15, kind == SMW.blank_tile_map16 and COLOUR.blank_tile or COLOUR.block, 0)
                
                if Layer1_tiles[number][3] then
                    display_boundaries(x_game, y_game, 16, 16, camera_x, camera_y)  -- the text around it
                end
                
                -- Draw Map16 id
                Text_opacity = 1.0
                if kind and x_mouse == positions[1] and y_mouse == positions[2] then
                    draw_text(AR_x*(left + 4), AR_y*top - BIZHAWK_FONT_HEIGHT, fmt("Map16 (%d, %d), %x%s", num_x, num_y, kind, address),
                    false, false, 0.5, 1.0)
                end
            end
            
        end
        
    end
    
end


local function draw_layer2_tiles()
    local layer2x = s16(WRAM.layer2_x_nextframe)
    local layer2y = s16(WRAM.layer2_y_nextframe)
    
    for number, positions in ipairs(Layer2_tiles) do
        draw_rectangle(-layer2x + positions[1], -layer2y + positions[2], 15, 15, COLOUR.layer2_line, COLOUR.layer2_bg)
    end
end


-- if the user clicks in a tile, it will be be drawn
-- if click is onto drawn region, it'll be erased
-- there's a max of possible tiles
-- layer_table[n] is an array {x, y, [draw info?]}
local function select_tile(x, y, layer_table)
    if not OPTIONS.draw_tiles_with_click then return end
    if Game_mode ~= SMW.game_mode_level then return end
    
    for number, positions in ipairs(layer_table) do  -- if mouse points a drawn tile, erase it
        if x == positions[1] and y == positions[2] then
            -- Layer 1
            if layer_table == Layer1_tiles then
                if layer_table[number][3] == false then
                    layer_table[number][3] = true
                else
                    table.remove(layer_table, number)
                end
            -- Layer 2
            elseif layer_table == Layer2_tiles then
                table.remove(layer_table, number)
            end
            
            return
        end
    end
    
    -- otherwise, draw a new tile
    if #layer_table == OPTIONS.max_tiles_drawn then
        table.remove(layer_table, 1)
        layer_table[OPTIONS.max_tiles_drawn] = {x, y, false}
    else
        table.insert(layer_table, {x, y, false})
    end
    
end


-- uses the mouse to select an object
local function select_object(mouse_x, mouse_y, camera_x, camera_y)
    -- Font
    Text_opacity = 1.0
    Bg_opacity = 0.5
    
    local x_game, y_game = game_coordinates(mouse_x, mouse_y, camera_x, camera_y)
    local obj_id
    
    -- Checks if the mouse is over Mario
    local x_player = s16(WRAM.x)
    local y_player = s16(WRAM.y)
    if x_player + 0xe >= x_game and x_player + 0x2 <= x_game and y_player + 0x30 >= y_game and y_player + 0x8 <= y_game then
        obj_id = "Mario"
    end
    
    if not obj_id and OPTIONS.display_sprite_info then
        for id = 0, SMW.sprite_max - 1 do
            local sprite_status = u8(WRAM.sprite_status + id)
            if sprite_status ~= 0 and Sprites_info[id].x then  -- TODO: see why the script gets here without exporting Sprites_info
                -- Import some values
                local x_sprite, y_sprite = Sprites_info[id].x, Sprites_info[id].y
                local x_screen, y_screen = Sprites_info[id].x_screen, Sprites_info[id].y_screen
                local boxid = Sprites_info[id].boxid
                local xoff, yoff = Sprites_info[id].xoff, Sprites_info[id].yoff
                local width, height = Sprites_info[id].width, Sprites_info[id].height
                
                if x_sprite + xoff + width >= x_game and x_sprite + xoff <= x_game and
                y_sprite + yoff + height >= y_game and y_sprite + yoff <= y_game then
                    obj_id = id
                    break
                end
            end
        end
    end
    
    if not obj_id then return end
    
    draw_text(AR_x*User_input.xmouse, AR_y*(User_input.ymouse - 8), obj_id, true, false, 0.5, 1.0)
    return obj_id, x_game, y_game
end


-- This function sees if the mouse if over some object, to change its hitbox mode
-- The order is: 1) player, 2) sprite.
local function right_click()
    local id = select_object(User_input.xmouse, User_input.ymouse, Camera_x, Camera_y)
    
    if tostring(id) == "Mario" then
        
        if OPTIONS.display_player_hitbox and OPTIONS.display_interaction_points then
            OPTIONS.display_interaction_points = false
            OPTIONS.display_player_hitbox = false
        elseif OPTIONS.display_player_hitbox then
            OPTIONS.display_interaction_points = true
            OPTIONS.display_player_hitbox = false
        elseif OPTIONS.display_interaction_points then
            OPTIONS.display_player_hitbox = true
        else
            OPTIONS.display_player_hitbox = true
        end
        
    end
    if id then return end
    
    local spr_id = tonumber(id)
    if spr_id and spr_id >= 0 and spr_id <= SMW.sprite_max - 1 then
        
        local number = Sprites_info[spr_id].number
        if Sprite_hitbox[spr_id][number].sprite and Sprite_hitbox[spr_id][number].block then
            Sprite_hitbox[spr_id][number].sprite = false
            Sprite_hitbox[spr_id][number].block = false
        elseif Sprite_hitbox[spr_id][number].sprite then
            Sprite_hitbox[spr_id][number].block = true
            Sprite_hitbox[spr_id][number].sprite = false
        elseif Sprite_hitbox[spr_id][number].block then
            Sprite_hitbox[spr_id][number].sprite = true
        else
            Sprite_hitbox[spr_id][number].sprite = true
        end
        
    end
    if id then return end
    
    -- Select layer 2 tiles
    local layer2x = s16(WRAM.layer2_x_nextframe)
    local layer2y = s16(WRAM.layer2_y_nextframe)
    local x_mouse, y_mouse = User_input.xmouse + layer2x, User_input.ymouse + layer2y
    select_tile(16*floor(x_mouse/16), 16*floor(y_mouse/16) - Y_CAMERA_OFF, Layer2_tiles)
end


local function show_movie_info()
    if not OPTIONS.display_movie_info then
        return
    end
    
    -- Font
    Text_opacity = 1.0
    Bg_opacity = 1.0
    local y_text = - Border_top
    local x_text = 0
    local width = BIZHAWK_FONT_WIDTH
    
    local rec_color = (Readonly or not Movie_active) and COLOUR.text or COLOUR.warning
    local recording_bg = (Readonly or not Movie_active) and COLOUR.background or COLOUR.warning_bg 
    
    -- Read-only or read-write?
    local movie_type = (not Movie_active and "No movie ") or (Readonly and "Movie " or "REC ")
    alert_text(x_text, y_text, movie_type, rec_color, recording_bg)
    
    if Movie_active then
        -- Frame count
        x_text = x_text + width*string.len(movie_type)
        local movie_info
        if Readonly then
            movie_info = string.format("%d/%d", Lastframe_emulated, Framecount)
        else
            movie_info = string.format("%d", Lastframe_emulated)
        end
        draw_text(x_text, y_text, movie_info)  -- Shows the latest frame emulated, not the frame being run now
        
        -- Rerecord count
        x_text = x_text + width*string.len(movie_info)
        local rr_info = string.format(" %d ", Rerecords)
        draw_text(x_text, y_text, rr_info, COLOUR.weak)
        
        -- Lag count
        x_text = x_text + width*string.len(rr_info)
        draw_text(x_text, y_text, Lagcount, COLOUR.warning)
    end
    
    local str = frame_time(Lastframe_emulated)    -- Shows the latest frame emulated, not the frame being run now
    alert_text(Buffer_width, Buffer_height, str, COLOUR.text, recording_bg, false, 1.0, 1.0)
    
end


local function show_misc_info()
    if not OPTIONS.display_misc_info then
        return
    end
    
    -- Font
    Text_opacity = 1.0
    Bg_opacity = 1.0
    
    -- Display
    local RNG = u16(WRAM.RNG)
    local main_info = string.format("Frame(%02x, %02x) RNG(%04x) Mode(%02x)",
                                    Real_frame, Effective_frame, RNG, Game_mode)
    ;
    
    draw_text(Buffer_width + Border_right, -Border_top, main_info, true, false)
    
    if Game_mode == SMW.game_mode_level then
        -- Time frame counter of the clock
        Text_opacity = 1.0
        local timer_frame_counter = u8(WRAM.timer_frame_counter)
        draw_text(AR_x*161, AR_y*15, fmt("%.2d", timer_frame_counter))
        
        -- Score: sum of digits, useful for avoiding lag
        Text_opacity = 0.5
        local score = u24(WRAM.mario_score)
        draw_text(AR_x*240, AR_y*24, fmt("=%d", sum_digits(score)), COLOUR.weak)
    end
end


-- Shows the controller input as the RAM and SNES registers store it
local function show_controller_data()
    if not (OPTIONS.display_debug_info and OPTIONS.display_debug_controller_data) then return end
    
    -- Font
    Text_opacity = 0.9
    local height = BIZHAWK_FONT_HEIGHT
    local x_pos, y_pos, x, y, _ = 0, 0, 0, BIZHAWK_FONT_HEIGHT
    
    x = draw_over_text(x_pos, y_pos, 256*u8(WRAM.ctrl_1_1) + u8(WRAM.ctrl_1_2), "BYsS^v<>AXLR0123", COLOUR.weak)
    _, y = draw_text(x, y_pos, " (RAM data)", COLOUR.weak, false, true)
    
    x = x_pos
    draw_over_text(x, y, 256*u8(WRAM.firstctrl_1_1) + u8(WRAM.firstctrl_1_2), "BYsS^v<>AXLR0123", 0, 0xff0000ff, 0)
end


local function level_info()
    if not OPTIONS.display_level_info then
        return
    end
    -- Font
    local x_pos = Buffer_width + Border_right
    local y_pos = - Border_top + BIZHAWK_FONT_HEIGHT
    local color = COLOUR.text
    Text_opacity = 1.0
    Bg_opacity = 1.0
    
    local sprite_buoyancy = floor(u8(WRAM.sprite_buoyancy)/64)
    if sprite_buoyancy == 0 then sprite_buoyancy = "" else
        sprite_buoyancy = fmt(" %.2x", sprite_buoyancy)
        color = COLOUR.warning
    end
    
    -- converts the level number to the Lunar Magic number; should not be used outside here
    local lm_level_number = Level_index
    if Level_index > 0x24 then lm_level_number = Level_index + 0xdc end
    
    -- Number of screens within the level
    local level_type, screens_number, hscreen_current, hscreen_number, vscreen_current, vscreen_number = read_screens()
    
    draw_text(x_pos, y_pos, fmt("%.1sLevel(%.2x)%s", level_type, lm_level_number, sprite_buoyancy),
                    color, true, false)
	;
    
    draw_text(x_pos, y_pos + BIZHAWK_FONT_HEIGHT, fmt("Screens(%d):", screens_number), true)
    
    draw_text(x_pos, y_pos + 2*BIZHAWK_FONT_HEIGHT, fmt("(%d/%d, %d/%d)", hscreen_current, hscreen_number,
                vscreen_current, vscreen_number), true)
    ;
end


function draw_blocked_status(x_text, y_text, player_blocked_status, x_speed, y_speed)
    local block_width  = 9 -- BizHawk
    local block_height = 9 -- BizHawk
    local block_str = "Block:"
    local str_len = string.len(block_str)
    local xoffset = (x_text + str_len*BIZHAWK_FONT_WIDTH)/AR_x
    local yoffset = y_text/AR_y
    local color_line = COLOUR.warning
    
    gui.drawRectangle(xoffset + Left_gap, yoffset + Top_gap, block_width - 1, block_height - 1, 0x40000000, 0x40ff0000)
    
    local blocked_status = {}
    local was_boosted = false
    
    if bit.test(player_blocked_status, 0) then  -- Right
        draw_line(xoffset + block_width - 1, yoffset, xoffset + block_width - 1, yoffset + block_height - 1, 1, color_line)
        if x_speed < 0 then was_boosted = true end
    end
    
    if bit.test(player_blocked_status, 1) then  -- Left
        draw_line(xoffset, yoffset, xoffset, yoffset + block_height - 1, 1, color_line)
        if x_speed > 0 then was_boosted = true end
    end
    
    if bit.test(player_blocked_status, 2) then  -- Down
        draw_line(xoffset, yoffset + block_height - 1, xoffset + block_width - 1, yoffset + block_height - 1, 1, color_line)
    end
    
    if bit.test(player_blocked_status, 3) then  -- Up
        draw_line(xoffset, yoffset, xoffset + block_width - 1, yoffset, 1, color_line)
        if y_speed > 6 then was_boosted = true end
    end
    
    if bit.test(player_blocked_status, 4) then  -- Middle
        gui.crosshair(xoffset + floor(block_width/2) + Left_gap, yoffset + floor(block_height/2) + Top_gap,  -- BizHawk
        math.min(block_width/3, block_height/3), color_line)
    end
    
    draw_text(x_text, y_text, block_str, COLOUR.text, was_boosted and COLOUR.warning_bg or nil)
    
end


-- displays player's hitbox
local function player_hitbox(x, y, is_ducking, powerup, transparency_level)
    local x_screen, y_screen = screen_coordinates(x, y, Camera_x, Camera_y)
    local is_small = is_ducking ~= 0 or powerup == 0
    
    -- Colors BizHawk
    local is_transparent = transparency_level == 1
    local interaction_bg = is_transparent and COLOUR.interaction_bg or 0
    local mario_bg = is_transparent and COLOUR.mario_bg or 0
    local mario_mounted_bg = is_transparent and COLOUR.mario_mounted_bg or 0
    local mario = is_transparent and COLOUR.mario or change_transparency(COLOUR.mario, transparency_level)
    local interaction_nohitbox = is_transparent and COLOUR.interaction_nohitbox or change_transparency(COLOUR.interaction_nohitbox, transparency_level)
    local interaction_nohitbox_bg = is_transparent and COLOUR.interaction_nohitbox_bg or 0
    local interaction = is_transparent and COLOUR.interaction or change_transparency(COLOUR.interaction, transparency_level)
    
    local x_points = X_INTERACTION_POINTS
    local y_points
    if is_small and not Yoshi_riding_flag then
        y_points = Y_INTERACTION_POINTS[1]
    elseif not is_small and not Yoshi_riding_flag then
        y_points = Y_INTERACTION_POINTS[2]
    elseif is_small and Yoshi_riding_flag then
        y_points = Y_INTERACTION_POINTS[3]
    else
        y_points = Y_INTERACTION_POINTS[4]
    end
    
    draw_box(x_screen + x_points.left_side, y_screen + y_points.head, x_screen + x_points.right_side, y_screen + y_points.foot,
            interaction_bg, interaction_bg)  -- background for block interaction
    ;
    
    if OPTIONS.display_player_hitbox then
        
        -- Collision with sprites
        local mario_bg = (not Yoshi_riding_flag and mario_bg) or mario_mounted_bg
        
        draw_box(x_screen + x_points.left_side  - 1, y_screen + y_points.sprite,
                 x_screen + x_points.right_side + 1, y_screen + y_points.foot + 1, mario, mario_bg)
        ;
        
    end
    
    -- interaction points (collision with blocks)
    if OPTIONS.display_interaction_points then
        
        if not SHOW_PLAYER_HITBOX then
            draw_box(x_screen + x_points.left_side , y_screen + y_points.head,
                     x_screen + x_points.right_side, y_screen + y_points.foot, interaction_nohitbox, interaction_nohitbox_bg)
        end
        
        draw_line(x_screen + x_points.left_side, y_screen + y_points.side, x_screen + x_points.left_foot, y_screen + y_points.side, 1, interaction)  -- left side
        draw_line(x_screen + x_points.right_side, y_screen + y_points.side, x_screen + x_points.right_foot, y_screen + y_points.side, 1, interaction)  -- right side
        draw_line(x_screen + x_points.left_foot, y_screen + y_points.foot - 2, x_screen + x_points.left_foot, y_screen + y_points.foot, 1, interaction)  -- left foot bottom
        draw_line(x_screen + x_points.right_foot, y_screen + y_points.foot - 2, x_screen + x_points.right_foot, y_screen + y_points.foot, 1, interaction)  -- right foot bottom
        draw_line(x_screen + x_points.left_side, y_screen + y_points.shoulder, x_screen + x_points.left_side + 2, y_screen + y_points.shoulder, 1, interaction)  -- head left point
        draw_line(x_screen + x_points.right_side - 2, y_screen + y_points.shoulder, x_screen + x_points.right_side, y_screen + y_points.shoulder, 1, interaction)  -- head right point
        draw_line(x_screen + x_points.center, y_screen + y_points.head, x_screen + x_points.center, y_screen + y_points.head + 2, 1, interaction)  -- head point
        draw_line(x_screen + x_points.center - 1, y_screen + y_points.center, x_screen + x_points.center + 1, y_screen + y_points.center, 1, interaction)  -- center point
        draw_line(x_screen + x_points.center, y_screen + y_points.center - 1, x_screen + x_points.center, y_screen + y_points.center + 1, 1, interaction)  -- center point
    end
    
    -- That's the pixel that appears when Mario dies in the pit
    Show_player_point_position = Show_player_point_position or y_screen >= 200 or
    (OPTIONS.display_debug_info and OPTIONS.display_debug_player_extra)
    if Show_player_point_position then
        draw_rectangle(x_screen - 1, y_screen - 1, 2, 2, interaction_bg, interaction)
        Show_player_point_position = false
    end
    
    return x_points, y_points
end


-- displays the hitbox of the cape while spinning
local function cape_hitbox(spin_direction)
    local cape_interaction = u8(WRAM.cape_interaction)
    if cape_interaction == 0 then return end
    
    local cape_x = u16(WRAM.cape_x)
    local cape_y = u16(WRAM.cape_y)
    
    local cape_x_screen, cape_y_screen = screen_coordinates(cape_x, cape_y, Camera_x, Camera_y)
    local cape_left = -2
    local cape_right = 0x12
    local cape_up = 0x01
    local cape_down = 0x11
    local cape_middle = 0x08
    local block_interaction_cape = (spin_direction < 0 and cape_left + 4) or cape_right - 4
    local active_frame_sprites = Real_frame%2 == 1  -- active iff the cape can hit a sprite
    local active_frame_blocks  = Real_frame%2 == (spin_direction < 0 and 0 or 1)  -- active iff the cape can hit a block
    
    if active_frame_sprites then bg_color = COLOUR.cape_bg else bg_color = 0 end
    draw_box(cape_x_screen + cape_left, cape_y_screen + cape_up, cape_x_screen + cape_right, cape_y_screen + cape_down, COLOUR.cape, bg_color)
    
    if active_frame_blocks then
        draw_pixel(cape_x_screen + block_interaction_cape, cape_y_screen + cape_middle, COLOUR.warning)
    else
        draw_pixel(cape_x_screen + block_interaction_cape, cape_y_screen + cape_middle, COLOUR.text)
    end
end


local function player()
    if not OPTIONS.display_player_info then
        return
    end
    
    -- Font
    Text_opacity = 1.0
    
    -- Reads WRAM
    local x = s16(WRAM.x)
    local y = s16(WRAM.y)
    local previous_x = s16(WRAM.previous_x)
    local previous_y = s16(WRAM.previous_y)
    local x_sub = u8(WRAM.x_sub)
    local y_sub = u8(WRAM.y_sub)
    local x_speed = s8(WRAM.x_speed)
    local x_subspeed = u8(WRAM.x_subspeed)
    local y_speed = s8(WRAM.y_speed)
    local p_meter = u8(WRAM.p_meter)
    local take_off = u8(WRAM.take_off)
    local powerup = Player_powerup
    local direction = u8(WRAM.direction)
    local cape_spin = u8(WRAM.cape_spin)
    local cape_fall = u8(WRAM.cape_fall)
    local flight_animation = u8(WRAM.flight_animation)
    local diving_status = s8(WRAM.diving_status)
    local player_blocked_status = u8(WRAM.player_blocked_status)
    local player_item = u8(WRAM.player_item)
    local is_ducking = u8(WRAM.is_ducking)
    local on_ground = u8(WRAM.on_ground)
    local spinjump_flag = u8(WRAM.spinjump_flag)
    local can_jump_from_water = u8(WRAM.can_jump_from_water)
    local carrying_item = u8(WRAM.carrying_item)
    local scroll_timer = u8(WRAM.camera_scroll_timer)
    local vertical_scroll_flag_header = u8(WRAM.vertical_scroll_flag_header)
    local vertical_scroll_enabled = u8(WRAM.vertical_scroll_enabled)
    
    -- Transformations
    if direction == 0 then direction = LEFT_ARROW else direction = RIGHT_ARROW end
    local x_sub_simple, y_sub_simple-- = x_sub, y_sub
    if x_sub%0x10 == 0 then x_sub_simple = fmt("%x", x_sub/0x10) else x_sub_simple = fmt("%.2x", x_sub) end
    if y_sub%0x10 == 0 then y_sub_simple = fmt("%x", y_sub/0x10) else y_sub_simple = fmt("%.2x", y_sub) end
    
    local x_speed_int, x_speed_frac = math.modf(x_speed + x_subspeed/0x100)
    x_speed_frac = math.abs(x_speed_frac*100)
    
    local spin_direction = (Effective_frame)%8
    if spin_direction < 4 then
        spin_direction = spin_direction + 1
    else
        spin_direction = 3 - spin_direction
    end
    
    local is_caped = powerup == 0x2
    local is_spinning = cape_spin ~= 0 or spinjump_flag ~= 0
    
    -- Display info
    local i = 0
    local delta_x = BIZHAWK_FONT_WIDTH
    local delta_y = BIZHAWK_FONT_HEIGHT
    local table_x = - Border_left
    local table_y = AR_y*32
    
    draw_text(table_x, table_y + i*delta_y, fmt("Meter (%03d, %02d) %s", p_meter, take_off, direction))
    draw_text(table_x + 18*delta_x, table_y + i*delta_y, fmt(" %+d", spin_direction),
    (is_spinning and COLOUR.text) or COLOUR.weak)
    i = i + 1
    
    draw_text(table_x, table_y + i*delta_y, fmt("Pos (%+d.%s, %+d.%s)", x, x_sub_simple, y, y_sub_simple))
    i = i + 1
    
    draw_text(table_x, table_y + i*delta_y, fmt("Speed (%+d(%d.%02.0f), %+d)", x_speed, x_speed_int, x_speed_frac, y_speed))
    i = i + 1
    
    if is_caped then
        draw_text(table_x, table_y + i*delta_y, fmt("Cape (%.2d, %.2d)/(%d, %d)", cape_spin, cape_fall, flight_animation, diving_status), COLOUR.cape)
        i = i + 1
    end
    
    local x_txt = draw_text(table_x, table_y + i*delta_y, fmt("Camera (%d, %d)", Camera_x, Camera_y))
    if scroll_timer ~= 0 then x_txt = draw_text(x_txt, table_y + i*delta_y, 16 - scroll_timer, COLOUR.warning) end
    if vertical_scroll_flag_header ~=0 and vertical_scroll_enabled ~= 0 then
        draw_text(x_txt, table_y + i*delta_y, vertical_scroll_enabled, COLOUR.warning2)
    end
    i = i + 1
    
    if OPTIONS.display_static_camera_region then
        Show_player_point_position = true
        local left_cam, right_cam = u16(0x142c), u16(0x142e)  -- unlisted WRAM
        draw_box(left_cam, 0, right_cam, 224, COLOUR.static_camera_region, COLOUR.static_camera_region)
    end
    
    draw_blocked_status(table_x, table_y + i*delta_y, player_blocked_status, x_speed, y_speed)
    
    -- Mario boost indicator (experimental)
    -- This looks for differences between the expected x position and the actual x position, after a frame advance
    -- Fails during a loadstate and has false positives if the game is paused or lagged
    Previous.player_x = 256*x + x_sub  -- the total amount of 256-based subpixels
    Previous.x_speed = 16*x_speed  -- the speed in 256-based subpixels
    
    if Mario_boost_indicator and not Cheat.under_free_move then
        local x_screen, y_screen = screen_coordinates(x, y, Camera_x, Camera_y)
        draw_text(AR_x*(x_screen + 4), AR_y*(y_screen + 60), Mario_boost_indicator, COLOUR.warning, 0x20000000)  -- unlisted color
    end
    
    -- shows hitbox and interaction points for player
    if not (OPTIONS.display_player_hitbox or OPTIONS.display_interaction_points) then return end
    
    cape_hitbox(spin_direction)
    player_hitbox(x, y, is_ducking, powerup, 1.0)
    
    -- Shows where Mario is expected to be in the next frame, if he's not boosted or stopped (DEBUG)
    if OPTIONS.display_debug_info and OPTIONS.display_debug_player_extra then
        player_hitbox( floor((256*x + x_sub + 16*x_speed)/256),
            floor((256*y + y_sub + 16*y_speed)/256), is_ducking, powerup, 0.3)  -- BizHawk
    end
    
end


local function extended_sprites()
    if not OPTIONS.display_extended_sprite_info then
        return
    end
    
    -- Font
    Text_opacity = 1.0
    local height = BIZHAWK_FONT_HEIGHT
    
    local y_pos = AR_y*144
    local counter = 0
    for id = 0, SMW.extended_sprite_max - 1 do
        local extspr_number = u8(WRAM.extspr_number + id)
        
        if extspr_number ~= 0 then
            -- Reads WRAM addresses
            local x = 256*u8(WRAM.extspr_x_high + id) + u8(WRAM.extspr_x_low + id)
            local y = 256*u8(WRAM.extspr_y_high + id) + u8(WRAM.extspr_y_low + id)
            local sub_x = bit.rshift(u8(WRAM.extspr_subx + id), 4)
            local sub_y = bit.rshift(u8(WRAM.extspr_suby + id), 4)
            local x_speed = s8(WRAM.extspr_x_speed + id)
            local y_speed = s8(WRAM.extspr_y_speed + id)
            local extspr_table = u8(WRAM.extspr_table + id)
            local extspr_table2 = u8(WRAM.extspr_table2 + id)
            
            -- Reduction of useless info
            local special_info = ""
            if OPTIONS.display_debug_info and OPTIONS.display_debug_extended_sprite and (extspr_table ~= 0 or extspr_table2 ~= 0) then
                special_info = fmt("(%x, %x) ", extspr_table, extspr_table2)
            end
            
            -- x speed for Fireballs
            if extspr_number == 5 then x_speed = 16*x_speed end
            
            if OPTIONS.display_extended_sprite_info then
                draw_text(Buffer_width + Border_right, y_pos + counter*height, fmt("#%.2d %.2x %s(%d.%x(%+.2d), %d.%x(%+.2d))",
                                                    id, extspr_number, special_info, x, sub_x, x_speed, y, sub_y, y_speed),
                                                    COLOUR.extended_sprites, true, false)
            end
            
            if (OPTIONS.display_debug_info and OPTIONS.display_debug_extended_sprite) or not UNINTERESTING_EXTENDED_SPRITES[extspr_number]
                or (extspr_number == 1 and extspr_table2 == 0xf)
            then
                local x_screen, y_screen = screen_coordinates(x, y, Camera_x, Camera_y)
                
                local t = HITBOX_EXTENDED_SPRITE[extspr_number] or
                    {xoff = 0, yoff = 0, width = 16, height = 16, color_line = COLOUR.awkward_hitbox, color_bg = COLOUR.awkward_hitbox_bg}
                local xoff = t.xoff
                local yoff = t.yoff + Y_CAMERA_OFF
                local xrad = t.width
                local yrad = t.height
                
                local color_line = t.color_line or COLOUR.extended_sprites
                local color_bg = t.color_bg or COLOUR.extended_sprites_bg
                if extspr_number == 0x5 or extspr_number == 0x11 then
                    color_bg = (Real_frame - id)%4 == 0 and COLOUR.special_extended_sprite_bg or 0
                end
                draw_rectangle(x_screen+xoff, y_screen+yoff, xrad, yrad, color_line, color_bg) -- regular hitbox
                
                -- Experimental: attempt to show Mario's fireball vs sprites
                -- this is likely wrong in some situation, but I can't solve this yet
                if extspr_number == 5 or extspr_number == 1 then
                    local xoff_spr = x_speed >= 0 and -5 or  1
                    local yoff_spr = - floor(y_speed/16) - 4 + (y_speed >= -40 and 1 or 0)
                    local yrad_spr = y_speed >= -40 and 19 or 20
                    draw_rectangle(x_screen + xoff_spr, y_screen + yoff_spr, 12, yrad_spr, color_line, color_bg)
                end
            end
            
            counter = counter + 1
        end
    end
    
    Text_opacity = 0.5
    local x_pos, y_pos, length = draw_text(Buffer_width + Border_right, y_pos, fmt("Ext. spr:%2d ", counter), COLOUR.weak, true, false, 0.0, 1.0)
    
    if u8(WRAM.spinjump_flag) ~= 0 and u8(WRAM.powerup) == 3 then
        local fireball_timer = u8(WRAM.spinjump_fireball_timer)
        draw_text(x_pos - length - BIZHAWK_FONT_WIDTH, y_pos, fmt("%d %s",
        fireball_timer%16, bit.test(fireball_timer, 4) and RIGHT_ARROW or LEFT_ARROW), COLOUR.extended_sprites, true, false, 1.0, 1.0)
    end
    
end


local function cluster_sprites()
    if not OPTIONS.display_cluster_sprite_info or u8(WRAM.cluspr_flag) == 0 then return end
    
    -- Font
    Text_opacity = 1.0
    local height = BIZHAWK_FONT_HEIGHT
    local x_pos, y_pos = AR_x*90, AR_y*77  -- BizHawk
    local counter = 0
    
    if OPTIONS.display_debug_info and OPTIONS.display_debug_cluster_sprite then
        draw_text(x_pos, y_pos, "Cluster Spr.", COLOUR.weak)
        counter = counter + 1
    end
    
    local reappearing_boo_counter
    
    for id = 0, SMW.cluster_sprite_max - 1 do
        local clusterspr_number = u8(WRAM.cluspr_number + id)
        
        if clusterspr_number ~= 0 then
            if not HITBOX_CLUSTER_SPRITE[clusterspr_number] then
                print("Warning: wrong cluster sprite number:", clusterspr_number)  -- should not happen without cheats
                return
            end
            
            -- Reads WRAM addresses
            local x = signed(256*u8(WRAM.cluspr_x_high + id) + u8(WRAM.cluspr_x_low + id), 16)
            local y = signed(256*u8(WRAM.cluspr_y_high + id) + u8(WRAM.cluspr_y_low + id), 16)
            local clusterspr_timer, special_info, table_1, table_2, table_3
            
            -- Reads cluster's table
            local x_screen, y_screen = screen_coordinates(x, y, Camera_x, Camera_y)
            local t = HITBOX_CLUSTER_SPRITE[clusterspr_number] or
                {xoff = 0, yoff = 0, width = 16, height = 16, color_line = COLOUR.awkward_hitbox, color_bg = COLOUR.awkward_hitbox_bg, oscillation = 1}
            local xoff = t.xoff
            local yoff = t.yoff + Y_CAMERA_OFF
            local xrad = t.width
            local yrad = t.height
            local phase = t.phase or 0
            local oscillation = (Real_frame - id)%t.oscillation == phase
            local color = t.color or COLOUR.cluster_sprites
            local color_bg = t.bg or COLOUR.sprites_bg
            local invencibility_hitbox = nil
            
            if OPTIONS.display_debug_info and OPTIONS.display_debug_cluster_sprite then
                table_1 = u8(WRAM.cluspr_table_1 + id)
                table_2 = u8(WRAM.cluspr_table_2 + id)
                table_3 = u8(WRAM.cluspr_table_3 + id)
                draw_text(x_pos, y_pos + counter*height, ("#%d(%d): (%d, %d) %d, %d, %d")
                :format(id, clusterspr_number, x, y, table_1, table_2, table_3), color)
                counter = counter + 1
            end
            
            -- Case analysis
            if clusterspr_number == 3 or clusterspr_number == 8 then
                clusterspr_timer = u8(WRAM.cluspr_timer + id)
                if clusterspr_timer ~= 0 then special_info = " " .. clusterspr_timer end
            elseif clusterspr_number == 6 then
                table_1 = table_1 or u8(WRAM.cluspr_table_1 + id)
                if table_1 >= 111 or (table_1 < 31 and table_1 >= 16) then
                    yoff = yoff + 17
                elseif table_1 >= 103 or table_1 < 16 then
                    invencibility_hitbox = true
                elseif table_1 >= 95 or (table_1 < 47 and table_1 >= 31) then
                    yoff = yoff + 16
                end
            elseif clusterspr_number == 7 then
                reappearing_boo_counter = reappearing_boo_counter or u8(WRAM.reappearing_boo_counter)
                invencibility_hitbox = (reappearing_boo_counter > 0xde) or (reappearing_boo_counter < 0x3f)
                special_info = " " .. reappearing_boo_counter
            end
            
            -- Hitbox and sprite id
            color = invencibility_hitbox and COLOUR.weak or color
            color_bg = (invencibility_hitbox and 0) or (oscillation and color_bg) or 0
            draw_rectangle(x_screen + xoff, y_screen + yoff, xrad, yrad, color, color_bg)
            draw_text(AR_x*(x_screen + xoff) + xrad, AR_y*(y_screen + yoff), special_info and id .. special_info or id,
            color, false, false, 0.5, 1.0)
        end
    end
end


local function minor_extended_sprites()
    if not OPTIONS.display_minor_extended_sprite_info then return end
    
    -- Font
    Text_opacity = 1.0
    local height = BIZHAWK_FONT_HEIGHT
    local x_pos, y_pos = 0, Buffer_height - height*SMW.minor_extended_sprite_max
    local counter = 0
    
    for id = 0, SMW.minor_extended_sprite_max - 1 do
        local minorspr_number = u8(WRAM.minorspr_number + id)
        
        if minorspr_number ~= 0 then
            -- Reads WRAM addresses
            local x = signed(256*u8(WRAM.minorspr_x_high + id) + u8(WRAM.minorspr_x_low + id), 16)
            local y = signed(256*u8(WRAM.minorspr_y_high + id) + u8(WRAM.minorspr_y_low + id), 16)
            local xspeed, yspeed = s8(WRAM.minorspr_xspeed + id), s8(WRAM.minorspr_yspeed + id)
            local x_sub, y_sub = u8(WRAM.minorspr_x_sub + id), u8(WRAM.minorspr_y_sub + id)
            local timer = u8(WRAM.minorspr_timer + id)
            
            -- Only sprites 1 and 10 use the higher byte
            local x_screen, y_screen = screen_coordinates(x, y, Camera_x, Camera_y)
            if minorspr_number ~= 1 and minorspr_number ~= 10 then  -- Boo stream and Piece of brick block
                x_screen = x_screen%0x100
                y_screen = y_screen%0x100
            end
            
            -- Draw next to the sprite
            local text = "#" .. id .. (timer ~= 0 and (" " .. timer) or "")
            draw_text(AR_x*(x_screen + 8), AR_y*(y_screen + 4), text, COLOUR.minor_extended_sprites, false, false, 0.5, 1.0)
            if minorspr_number == 10 then  -- Boo stream
                draw_rectangle(x_screen + 4, y_screen + 4 + Y_CAMERA_OFF, 8, 8, COLOUR.minor_extended_sprites, COLOUR.sprites_bg)
            end
            
            -- Draw in the table
            if OPTIONS.display_debug_info and OPTIONS.display_debug_minor_extended_sprite then
                draw_text(x_pos, y_pos + counter*height, ("#%d(%d): %d.%x(%d), %d.%x(%d)")
                :format(id, minorspr_number, x, floor(x_sub/16), xspeed, y, floor(y_sub/16), yspeed), COLOUR.minor_extended_sprites)
            end
            counter = counter + 1
        end
    end
    
    if OPTIONS.display_debug_info and OPTIONS.display_debug_minor_extended_sprite then
        draw_text(x_pos, y_pos - height, "Minor Ext Spr:" .. counter, COLOUR.weak)
    end
end


local function bounce_sprite_info()
    if not OPTIONS.display_bounce_sprite_info then return end
    
    -- Debug info
    local x_txt, y_txt = AR_x*90, AR_y*37
    if OPTIONS.display_debug_info and OPTIONS.display_debug_bounce_sprite then
        Text_opacity = 0.5
        draw_text(x_txt, y_txt, "Bounce Spr.", COLOUR.weak)
    end
    
    -- Font
    Text_opacity = 1.0
    local height = BIZHAWK_FONT_HEIGHT
    
    local stop_id = (u8(WRAM.bouncespr_last_id) - 1)%SMW.bounce_sprite_max
    for id = 0, SMW.bounce_sprite_max - 1 do
        local bounce_sprite_number = u8(WRAM.bouncespr_number + id)
        if bounce_sprite_number ~= 0 then
            local x = 256*u8(WRAM.bouncespr_x_high + id) + u8(WRAM.bouncespr_x_low + id)
            local y = 256*u8(WRAM.bouncespr_y_high + id) + u8(WRAM.bouncespr_y_low + id)
            local bounce_timer = u8(WRAM.bouncespr_timer + id)
            
            if OPTIONS.display_debug_info and OPTIONS.display_debug_bounce_sprite then
                draw_text(x_txt, y_txt + height*(id + 1), fmt("#%d:%d (%d, %d)", id, bounce_sprite_number, x, y))
            end
            
            local x_screen, y_screen = screen_coordinates(x, y, Camera_x, Camera_y)
            x_screen, y_screen = AR_x*(x_screen + 8), AR_y*y_screen
            local color = id == stop_id and COLOUR.warning or COLOUR.text
            draw_text(x_screen , y_screen, fmt("#%d:%d", id, bounce_timer), color, false, false, 0.5)  -- timer
            
            -- Turn blocks
            if bounce_sprite_number == 7 then
                turn_block_timer = u8(WRAM.turn_block_timer + id)
                draw_text(x_screen, y_screen + height, turn_block_timer, color, false, false, 0.5)
            end
        end
    end
end


local function sprite_info(id, counter, table_position)
    Text_opacity = 1.0
    
    local sprite_status = u8(WRAM.sprite_status + id)
    if sprite_status == 0 then return 0 end  -- returns if the slot is empty
    
    local x = 256*u8(WRAM.sprite_x_high + id) + u8(WRAM.sprite_x_low + id)
    local y = 256*u8(WRAM.sprite_y_high + id) + u8(WRAM.sprite_y_low + id)
    local x_sub = u8(WRAM.sprite_x_sub + id)
    local y_sub = u8(WRAM.sprite_y_sub + id)
    local number = u8(WRAM.sprite_number + id)
    local stun = u8(WRAM.sprite_miscellaneous7 + id)
    local x_speed = s8(WRAM.sprite_x_speed + id)
    local y_speed = s8(WRAM.sprite_y_speed + id)
    local contact_mario = u8(WRAM.sprite_miscellaneous8 + id)
    local underwater = u8(WRAM.sprite_underwater + id)
    local x_offscreen = s8(WRAM.sprite_x_offscreen + id)
    local y_offscreen = s8(WRAM.sprite_y_offscreen + id)
    
    local special = ""
    if (OPTIONS.display_debug_info and OPTIONS.display_debug_sprite_extra) or
    ((sprite_status ~= 0x8 and sprite_status ~= 0x9 and sprite_status ~= 0xa and sprite_status ~= 0xb) or stun ~= 0) then
        special = string.format("(%d %d) ", sprite_status, stun)
    end
    
    -- Let x and y be 16-bit signed
    x = signed(x, 16)
    y = signed(y, 16)
    
    ---**********************************************
    -- Calculates the sprites dimensions and screen positions
    
    local x_screen, y_screen = screen_coordinates(x, y, Camera_x, Camera_y)
    
    -- Sprite clipping vs mario and sprites
    local boxid = bit.band(u8(WRAM.sprite_2_tweaker + id), 0x3f)  -- This is the type of box of the sprite
    local xoff = HITBOX_SPRITE[boxid].xoff
    local yoff = HITBOX_SPRITE[boxid].yoff + Y_CAMERA_OFF
    local sprite_width = HITBOX_SPRITE[boxid].width
    local sprite_height = HITBOX_SPRITE[boxid].height
    
    -- Sprite clipping vs objects
    local clip_obj = bit.band(u8(WRAM.sprite_1_tweaker + id), 0xf)  -- type of hitbox for blocks
    local xpt_right = OBJ_CLIPPING_SPRITE[clip_obj].xright
    local ypt_right = OBJ_CLIPPING_SPRITE[clip_obj].yright
    local xpt_left = OBJ_CLIPPING_SPRITE[clip_obj].xleft 
    local ypt_left = OBJ_CLIPPING_SPRITE[clip_obj].yleft
    local xpt_down = OBJ_CLIPPING_SPRITE[clip_obj].xdown
    local ypt_down = OBJ_CLIPPING_SPRITE[clip_obj].ydown
    local xpt_up = OBJ_CLIPPING_SPRITE[clip_obj].xup
    local ypt_up = OBJ_CLIPPING_SPRITE[clip_obj].yup
    
    -- Process interaction with player every frame?
    -- Format: dpmksPiS. This 'm' bit seems odd, since it has false negatives
    local oscillation_flag = bit.test(u8(WRAM.sprite_4_tweaker + id), 5) or OSCILLATION_SPRITES[number]
    
    -- calculates the correct color to use, according to id
    local info_color
    local color_background
    if number == 0x35 then
        info_color = COLOUR.yoshi
        color_background = COLOUR.yoshi_bg
    else
        info_color = COLOUR.sprites[id%(#COLOUR.sprites) + 1]
        color_background = COLOUR.sprites_bg
    end
    
    
    if (not oscillation_flag) and (Real_frame - id)%2 == 1 then color_background = 0 end     -- due to sprite oscillation every other frame
                                                                                    -- notice that some sprites interact with Mario every frame
    ;
    
    
    ---**********************************************
    -- Displays sprites hitboxes
    if OPTIONS.display_sprite_hitbox then
        -- That's the pixel that appears when the sprite vanishes in the pit
        if y_screen >= 224 or (OPTIONS.display_debug_info and OPTIONS.display_debug_sprite_extra) then
            draw_pixel(x_screen, y_screen, info_color)
        end
        
        if Sprite_hitbox[id][number].block then
            draw_box(x_screen + xpt_left, y_screen + ypt_down, x_screen + xpt_right, y_screen + ypt_up,
                COLOUR.sprites_clipping_bg, Sprite_hitbox[id][number].sprite and 0 or COLOUR.sprites_clipping_bg)
        end
        
        if Sprite_hitbox[id][number].sprite and not ABNORMAL_HITBOX_SPRITES[number] then  -- show sprite/sprite clipping
            draw_rectangle(x_screen + xoff, y_screen + yoff, sprite_width, sprite_height, info_color, color_background)
        end
        
        if Sprite_hitbox[id][number].block then  -- show sprite/object clipping
            local size, color = 1, COLOUR.sprites_interaction_pts
            draw_line(x_screen + xpt_right, y_screen + ypt_right, x_screen + xpt_right - size, y_screen + ypt_right, 1, color) -- right
            draw_line(x_screen + xpt_left, y_screen + ypt_left, x_screen + xpt_left + size, y_screen + ypt_left, 1, color)  -- left
            draw_line(x_screen + xpt_down, y_screen + ypt_down, x_screen + xpt_down, y_screen + ypt_down - size, 1, color) -- down
            draw_line(x_screen + xpt_up, y_screen + ypt_up, x_screen + xpt_up, y_screen + ypt_up + size, 1, color)  -- up
        end
    end
    
    
    ---**********************************************
    -- Special sprites analysis:
    
    --[[
    PROBLEMATIC ONES
        29	Koopa Kid
        54  Revolving door for climbing net, wrong hitbox area, not urgent
        5a  Turn block bridge, horizontal, hitbox only applies to central block and wrongly
        86	Wiggler, the second part of the sprite, that hurts Mario even if he's on Yoshi, doesn't appear
        89	Layer 3 Smash, hitbox of generator outside
        9e	Ball 'n' Chain, hitbox only applies to central block, rotating ball
        a3	Rotating gray platform, wrong hitbox, rotating plataforms
    ]]
    
    if number == 0x5f then  -- Swinging brown platform (fix it)
        
        -- Powerup Incrementation helper
        local yoshi_right = 256*floor(x/256) - 58
        local yoshi_left  = yoshi_right + 32
        local x_text, y_text, height = AR_x*(x_screen + xoff), AR_y*(y_screen + yoff), BIZHAWK_FONT_HEIGHT
        
        if mouse_onregion(x_text, y_text, x_text + AR_x*sprite_width, y_text + AR_y*sprite_height) then
            local x_text, y_text = 0, height
            draw_text(x_text, y_text, "Powerup Incrementation help", info_color, COLOUR.background)
            draw_text(x_text, y_text + height, "Yoshi must have: id = #4;", info_color, COLOUR.background)
            draw_text(x_text, y_text + 2*height, ("Yoshi x pos: (%s %d) or (%s %d)")
            :format(LEFT_ARROW, yoshi_left, RIGHT_ARROW, yoshi_right), info_color, COLOUR.background)
        end
        --The status change happens when yoshi's id number is #4 and when (yoshi's x position) + Z mod 256 = 214,
        --where Z is 16 if yoshi is facing right, and -16 if facing left. More precisely, when (yoshi's x position + Z) mod 256 = 214,
        --the address 0x7E0015 + (yoshi's id number) will be added by 1.
        -- therefore: X_yoshi = 256*floor(x/256) + 32*yoshi_direction - 58
    end
    
    if number == 0x35 then  -- Yoshi
        if not Yoshi_riding_flag and OPTIONS.display_sprite_hitbox and Sprite_hitbox[id][number].sprite then
            draw_rectangle(x_screen + 4, y_screen + 20, 8, 8, COLOUR.yoshi)
        end
    end
    
    if number == 0x62 or number == 0x63 then  -- Brown line-guided platform & Brown/checkered line-guided platform
            xoff = xoff - 24
            yoff = yoff - 8
            -- for some reason, the actual base is 1 pixel below when Mario is small
            if OPTIONS.display_sprite_hitbox then
                draw_rectangle(x_screen + xoff, y_screen + yoff, sprite_width, sprite_height, info_color, color_background)
            end
    end
    
    if number == 0x6b then  -- Wall springboard (left wall)
        xoff = xoff - 8
        sprite_height = sprite_height + 1  -- for some reason, small Mario gets a bigger hitbox
        
        if OPTIONS.display_sprite_hitbox then
            draw_rectangle(x_screen + xoff, y_screen + yoff, sprite_width, sprite_height, info_color, color_background)
            draw_line(x_screen + xoff, y_screen + yoff + 3, x_screen + xoff + sprite_width, y_screen + yoff + 3, 1, info_color)
        end
    end
    
    if number == 0x6c then  -- Wall springboard (right wall)
        xoff = xoff - 31
        sprite_height = sprite_height + 1
        
        if OPTIONS.display_sprite_hitbox then
            draw_rectangle(x_screen + xoff, y_screen + yoff, sprite_width, sprite_height, info_color, color_background)
            draw_line(x_screen + xoff, y_screen + yoff + 3, x_screen + xoff + sprite_width, y_screen + yoff + 3, 1, info_color)
        end
    end
    
    if number == 0x7b then  -- Goal Tape
    
        Text_opacity = 0.8
        
        -- This draws the effective area of a goal tape
        local x_effective = 256*u8(WRAM.sprite_miscellaneous4 + id) + u8(0xc2 + id)  -- unlisted WRAM
        local y_low = 256*u8(0x1534 + id) + u8(WRAM.sprite_miscellaneous5 + id)  -- unlisted WRAM
        local _, y_high = screen_coordinates(0, 0, Camera_x, Camera_y)
        local x_s, y_s = screen_coordinates(x_effective, y_low, Camera_x, Camera_y)
        
        if OPTIONS.display_sprite_hitbox then
            draw_box(x_s, y_high, x_s + 15, y_s, info_color, COLOUR.goal_tape_bg)
        end
        draw_text(AR_x*x_s, AR_y*y_screen, fmt("Touch=%4d.0->%4d.f", x_effective, x_effective + 15), info_color, false, false)
        
        Text_opacity = 1.0
        Bg_opacity = 1.0
    
    elseif number == 0xa9 then  -- Reznor
    
        local reznor
        local color
        for index = 0, SMW.sprite_max - 1 do
            reznor = u8(WRAM.sprite_miscellaneous4 + index)
            if index >= 4 and index <= 7 then
                color = COLOUR.warning
            else
                color = color_weak
            end
            draw_text(3*BIZHAWK_FONT_WIDTH*index, Buffer_height, fmt("%.2x", reznor), color, true, false, 0.0, 1.0)
        end
    
    elseif number == 0xa0 then  -- Bowser
    
        local height = BIZHAWK_FONT_HEIGHT
        local y_text = Screen_height - 10*height
        local address = 0x14b0  -- unlisted WRAM
        for index = 0, 9 do
            local value = u8(address + index)
            draw_text(Buffer_width + Border_right, y_text + index*height, fmt("%2x = %3d", value, value), info_color, true)
        end
    
    end
    
    
    ---**********************************************
    -- Prints those informations next to the sprite
    Text_opacity = 1.0
    Bg_opacity = 1.0
    
    if x_offscreen ~= 0 or y_offscreen ~= 0 then
        Text_opacity = 0.6
    end
    
    local contact_str = contact_mario == 0 and "" or " " .. contact_mario
    
    local sprite_middle = x_screen + xoff + floor(sprite_width/2)
    local sprite_top = y_screen + math.min(yoff, ypt_up)
    draw_text(AR_x*sprite_middle, AR_y*sprite_top, fmt("#%.2d%s", id, contact_str), info_color, true, false, 0.5, 1.0)
    if Player_powerup == 2 then
        local contact_cape = u8(WRAM.sprite_disable_cape + id)
        if contact_cape ~= 0 then
            draw_text(AR_x*sprite_middle, AR_y*sprite_top - 2*BIZHAWK_FONT_HEIGHT, contact_cape, COLOUR.cape, true)
        end
    end
    
    
    ---**********************************************
    -- Sprite tweakers info
    if OPTIONS.display_debug_info and OPTIONS.display_debug_sprite_tweakers then
        Text_opacity = 0.8 -- BizHawk
        local height = BIZHAWK_FONT_HEIGHT
        local x_txt, y_txt = AR_x*sprite_middle - 4*BIZHAWK_FONT_WIDTH, AR_y*(y_screen + yoff) - 7*height
        
        local tweaker_1 = u8(WRAM.sprite_1_tweaker + id)
        draw_over_text(x_txt, y_txt, tweaker_1, "sSjJcccc", COLOUR.weak, info_color)
        y_txt = y_txt + height
        
        local tweaker_2 = u8(WRAM.sprite_2_tweaker + id)
        draw_over_text(x_txt, y_txt, tweaker_2, "dscccccc", COLOUR.weak, info_color)
        y_txt = y_txt + height
        
        local tweaker_3 = u8(WRAM.sprite_3_tweaker + id)
        draw_over_text(x_txt, y_txt, tweaker_3, "lwcfpppg", COLOUR.weak, info_color)
        y_txt = y_txt + height
        
        local tweaker_4 = u8(WRAM.sprite_4_tweaker + id)
        draw_over_text(x_txt, y_txt, tweaker_4, "dpmksPiS", COLOUR.weak, info_color)
        y_txt = y_txt + height
        
        local tweaker_5 = u8(WRAM.sprite_5_tweaker + id)
        draw_over_text(x_txt, y_txt, tweaker_5, "dnctswye", COLOUR.weak, info_color)
        y_txt = y_txt + height
        
        local tweaker_6 = u8(WRAM.sprite_6_tweaker + id)
        draw_over_text(x_txt, y_txt, tweaker_6, "wcdj5sDp", COLOUR.weak, info_color)
        Text_opacity = 1.0
    end
    
    
    ---**********************************************
    -- The sprite table:
    local x_speed_water = ""
    if underwater ~= 0 then  -- if sprite is underwater
        local correction = floor(3*floor(x_speed/2)/2)
        x_speed_water = string.format("%+.2d=%+.2d", correction - x_speed, correction)
    end
    local sprite_str = fmt("#%02d %02x %s%d.%1x(%+.2d%s) %d.%1x(%+.2d)", 
                    id, number, special, x, floor(x_sub/16), x_speed, x_speed_water, y, floor(y_sub/16), y_speed)
                    
    Text_opacity = 1.0
    Bg_opacity = 1.0
    if x_offscreen ~= 0 or y_offscreen ~= 0 then
        Text_opacity = 0.6
    end
    draw_text(Buffer_width + Border_right, table_position + counter*BIZHAWK_FONT_HEIGHT, sprite_str, info_color, true)
    
    -- Miscellaneous sprite table
    if OPTIONS.display_miscellaneous_sprite_table then
        local x_mis, y_mis = - Border_left, AR_y*144 + counter*BIZHAWK_FONT_HEIGHT
        
        local t = OPTIONS.miscellaneous_sprite_table_number
        local misc, text = nil, fmt("#%.2d", id)
        for num = 1, 19 do
            misc = t[num] and u8(WRAM["sprite_miscellaneous" .. num] + id) or false
            text = misc and fmt("%s %3d", text, misc) or text
        end
        
        draw_text(x_mis, y_mis, text, info_color)
    end
    
    -- Exporting some values
    Sprites_info[id].number = number
    Sprites_info[id].x, Sprites_info[id].y = x, y
    Sprites_info[id].x_screen, Sprites_info[id].y_screen = x_screen, y_screen
    Sprites_info[id].boxid = boxid
    Sprites_info[id].xoff, Sprites_info[id].yoff = xoff, yoff
    Sprites_info[id].width, Sprites_info[id].height = sprite_width, sprite_height
    
    return 1
end


local function sprites()
    if not OPTIONS.display_sprite_info then return end
    
    local counter = 0
    local table_position = AR_y*48
    for id = 0, SMW.sprite_max - 1 do
        counter = counter + sprite_info(id, counter, table_position)
    end
    
    -- Font
    Text_opacity = 0.6
    
    local swap_slot = u8(0x1861) -- unlisted WRAM
    local smh = u8(WRAM.sprite_memory_header)
    draw_text(Buffer_width + Border_right, table_position - 2*BIZHAWK_FONT_HEIGHT, fmt("spr:%.2d", counter), COLOUR.weak, true)
    draw_text(Buffer_width + Border_right, table_position - BIZHAWK_FONT_HEIGHT, fmt("1st div: %d. Swap: %d",
                                                            SPRITE_MEMORY_MAX[smh] or 0, swap_slot), COLOUR.weak, true)
    --
    -- Miscellaneous sprite table: index
    if OPTIONS.display_miscellaneous_sprite_table then
        local t = OPTIONS.miscellaneous_sprite_table_number
        local text = "Tab"
        for num = 1, 19 do
            text = t[num] and fmt("%s %3d", text, num) or text
        end
        
        draw_text(- Border_left, AR_y*144 - BIZHAWK_FONT_HEIGHT, text, info_color)
    end
end


local function yoshi()
    if not OPTIONS.display_yoshi_info then
        return
    end
    
    -- Font
    Text_opacity = 1.0
    Bg_opacity = 1.0
    local x_text = - Border_left
    local y_text = AR_y*88
    
    local yoshi_id = Yoshi_id
    if yoshi_id ~= nil then
        local eat_id = u8(WRAM.sprite_miscellaneous16+ yoshi_id)
        local eat_type = u8(WRAM.sprite_number + eat_id)
        local tongue_len = u8(WRAM.sprite_miscellaneous4 + yoshi_id)
        local tongue_timer = u8(WRAM.sprite_miscellaneous9 + yoshi_id)
        local tongue_wait = u8(WRAM.sprite_tongue_wait)
        local tongue_height = u8(WRAM.yoshi_tile_pos)
        local tongue_out = u8(WRAM.sprite_miscellaneous13 + yoshi_id)
        
        local eat_type_str = eat_id == SMW.null_sprite_id and "-" or string.format("%02x", eat_type)
        local eat_id_str = eat_id == SMW.null_sprite_id and "-" or string.format("#%02d", eat_id)
        
        -- Yoshi's direction and turn around
        local turn_around = u8(WRAM.sprite_miscellaneous14 + yoshi_id)
        local yoshi_direction = u8(WRAM.sprite_miscellaneous12 + yoshi_id)
        local direction_symbol
        if yoshi_direction == 0 then direction_symbol = RIGHT_ARROW else direction_symbol = LEFT_ARROW end
        
        draw_text(x_text, y_text, fmt("Yoshi %s %d", direction_symbol, turn_around), COLOUR.yoshi)
        local h = BIZHAWK_FONT_HEIGHT
        
        if eat_id == SMW.null_sprite_id and tongue_len == 0 and tongue_timer == 0 and tongue_wait == 0 then
            Text_opacity = 0.2
        end
        draw_text(x_text, y_text + h, fmt("(%0s, %0s) %02d, %d, %d",
                            eat_id_str, eat_type_str, tongue_len, tongue_wait, tongue_timer), COLOUR.yoshi)
        ;
        
        -- more WRAM values
        local yoshi_x = 256*u8(WRAM.sprite_x_high + yoshi_id) + u8(WRAM.sprite_x_low + yoshi_id)
        local yoshi_y = 256*u8(WRAM.sprite_y_high + yoshi_id) + u8(WRAM.sprite_y_low + yoshi_id)
        local x_screen, y_screen = screen_coordinates(yoshi_x, yoshi_y, Camera_x, Camera_y)
        
        -- invisibility timer
        local mount_invisibility = u8(WRAM.sprite_miscellaneous18 + yoshi_id)
        if mount_invisibility ~= 0 then
            Text_opacity = 0.5
            draw_text(AR_x*(x_screen + 4), AR_y*(y_screen - 12), mount_invisibility, COLOUR.yoshi)
        end
        
        -- Tongue hitbox and timer
        if tongue_wait ~= 0 or tongue_out ~=0 or tongue_height == 0x89 then  -- if tongue is out or appearing
            -- the position of the hitbox pixel
            local tongue_direction = yoshi_direction == 0 and 1 or -1
            local tongue_high = tongue_height ~= 0x89
            local x_tongue = x_screen + 24 - 40*yoshi_direction + tongue_len*tongue_direction
            x_tongue = not tongue_high and x_tongue or x_tongue - 5*tongue_direction
            local y_tongue = y_screen + 10 + 11*(tongue_high and 0 or 1)
            
            -- the drawing
            local tongue_line
            if tongue_wait <= 9  then  -- hitbox point vs berry tile
                draw_rectangle(x_tongue - 1, y_tongue - 1, 2, 2, COLOUR.tongue_bg, COLOUR.text)
                tongue_line = COLOUR.tongue_line
            else tongue_line = COLOUR.tongue_bg
            end
            
            -- tongue out: time predictor
            local tinfo, tcolor
            if tongue_wait > 9 then tinfo = tongue_wait - 9; tcolor = COLOUR.tongue_line  -- not ready yet
            
            elseif tongue_out == 1 then tinfo = 17 + tongue_wait; tcolor = COLOUR.text  -- tongue going out
            
            elseif tongue_out == 2 then  -- at the max or tongue going back
                tinfo = math.max(tongue_wait, tongue_timer) + floor((tongue_len + 7)/4) - (tongue_len ~= 0 and 1 or 0)
                tcolor = eat_id == SMW.null_sprite_id and COLOUR.text or COLOUR.warning
            
            elseif tongue_out == 0 then tinfo = 0; tcolor = COLOUR.text  -- tongue in
            
            else tinfo = tongue_timer + 1; tcolor = COLOUR.tongue_line -- item was just spat out
            end
            
            Text_opacity = 0.5
            draw_text(AR_x*(x_tongue + 4), AR_y*(y_tongue + 5), tinfo, tcolor, false, false, 0.5)
            Text_opacity = 1.0
            draw_rectangle(x_tongue, y_tongue + 1, 8, 4, tongue_line, COLOUR.tongue_bg)
        end
        
    end
end


local function show_counters()
    if not OPTIONS.display_counters then
        return
    end
    
    -- Font
    Text_opacity = 1.0
    Bg_opacity = 1.0
    local height = BIZHAWK_FONT_HEIGHT
    local text_counter = 0
    
    local pipe_entrance_timer = u8(WRAM.pipe_entrance_timer)
    local multicoin_block_timer = u8(WRAM.multicoin_block_timer)
    local gray_pow_timer = u8(WRAM.gray_pow_timer)
    local blue_pow_timer = u8(WRAM.blue_pow_timer)
    local dircoin_timer = u8(WRAM.dircoin_timer)
    local pballoon_timer = u8(WRAM.pballoon_timer)
    local star_timer = u8(WRAM.star_timer)
    local invisibility_timer = u8(WRAM.invisibility_timer)
    local animation_timer = u8(WRAM.animation_timer)
    local fireflower_timer = u8(WRAM.fireflower_timer)
    local yoshi_timer = u8(WRAM.yoshi_timer)
    local swallow_timer = u8(WRAM.swallow_timer)
    local lakitu_timer = u8(WRAM.lakitu_timer)
    local score_incrementing = u8(WRAM.score_incrementing)
    local end_level_timer = u8(WRAM.end_level_timer)
    
    local display_counter = function(label, value, default, mult, frame, color)
        if value == default then return end
        text_counter = text_counter + 1
        local color = color or COLOUR.text
        
        draw_text(- Border_left, AR_y*102 + (text_counter * height), fmt("%s: %d", label, (value * mult) - frame), color)
    end
    
    if Player_animation_trigger == 5 or Player_animation_trigger == 6 then
        display_counter("Pipe", pipe_entrance_timer, -1, 1, 0, COLOUR.counter_pipe)
    end
    display_counter("Multi Coin", multicoin_block_timer, 0, 1, 0, COLOUR.counter_multicoin)
    display_counter("Pow", gray_pow_timer, 0, 4, Effective_frame % 4, COLOUR.counter_gray_pow)
    display_counter("Pow", blue_pow_timer, 0, 4, Effective_frame % 4, COLOUR.counter_blue_pow)
    display_counter("Dir Coin", dircoin_timer, 0, 4, Real_frame % 4, COLOUR.counter_dircoin)
    display_counter("P-Balloon", pballoon_timer, 0, 4, Real_frame % 4, COLOUR.counter_pballoon)
    display_counter("Star", star_timer, 0, 4, (Effective_frame - 3) % 4, COLOUR.counter_star)
    display_counter("Invisibility", invisibility_timer, 0, 1, 0)
    display_counter("Fireflower", fireflower_timer, 0, 1, 0, COLOUR.counter_fireflower)
    display_counter("Yoshi", yoshi_timer, 0, 1, 0, COLOUR.yoshi)
    display_counter("Swallow", swallow_timer, 0, 4, (Effective_frame - 1) % 4, COLOUR.yoshi)
    display_counter("Lakitu", lakitu_timer, 0, 4, Effective_frame % 4)
    display_counter("End Level", end_level_timer, 0, 2, (Real_frame - 1) % 2)
    display_counter("Score Incrementing", score_incrementing, 0x50, 1, 0)
    
    if Lock_animation_flag ~= 0 then display_counter("Animation", animation_timer, 0, 1, 0) end  -- shows when player is getting hurt or dying
    
end


-- Main function to run inside a level
local function level_mode()
    if Game_mode == SMW.game_mode_level then
        
        -- Draws/Erases the tiles if user clicked
        draw_layer1_tiles(Camera_x, Camera_y)
        
        draw_layer2_tiles()
        
        sprites()
        
        extended_sprites()
        
        cluster_sprites()
        
        minor_extended_sprites()
        
        bounce_sprite_info()
        
        level_info()
        
        player()
        
        yoshi()
        
        show_counters()
        
        -- Draws/Erases the hitbox for objects
        if User_input.mouse_inwindow then
            select_object(User_input.xmouse, User_input.ymouse, Camera_x, Camera_y)
        end
        
    end
end


local function overworld_mode()
    if Game_mode ~= SMW.game_mode_overworld then return end
    
    -- Font
    Text_opacity = 1.0
    Bg_opacity = 1.0
    
    local height = BIZHAWK_FONT_HEIGHT
    local y_text = BIZHAWK_FONT_HEIGHT
    
    -- Real frame modulo 8
    local real_frame_8 = Real_frame%8
    draw_text(Buffer_width + Border_right, y_text, fmt("Real Frame = %3d = %d(mod 8)", Real_frame, real_frame_8), true)
    
    -- Star Road info
    local star_speed = u8(WRAM.star_road_speed)
    local star_timer = u8(WRAM.star_road_timer)
    y_text = y_text + height
    draw_text(Buffer_width + Border_right, y_text, fmt("Star Road(%x %x)", star_speed, star_timer), COLOUR.cape, true)
end


local function left_click()
    -- Call options menu if the form is closed
    if Options_form.is_form_closed and mouse_onregion(120*AR_x, 0, 120*AR_x + 4*BIZHAWK_FONT_WIDTH, BIZHAWK_FONT_HEIGHT) then -- bizhawk
        Options_form.create_window()
        return
    end
    
    -- Drag and drop sprites
    if Cheat.allow_cheats then
        local id = select_object(User_input.xmouse, User_input.ymouse, Camera_x, Camera_y)
        if type(id) == "number" and id >= 0 and id < SMW.sprite_max then
            Cheat.dragging_sprite_id = id
            Cheat.is_dragging_sprite = true
            return
        end
    end
    
    -- Layer 1 tiles
    local x_mouse, y_mouse = game_coordinates(User_input.xmouse, User_input.ymouse, Camera_x, Camera_y)
    x_mouse = 16*floor(x_mouse/16)
    y_mouse = 16*floor(y_mouse/16)
    if User_input.mouse_inwindow then
        select_tile(x_mouse, y_mouse, Layer1_tiles)
    end
end


-- This function runs at the end of paint callback
-- Specific for info that changes if the emulator is paused and idle callback is called
local function mouse_actions()
    -- Font
    Text_opacity = 1.0
    
    if Cheat.allow_cheats then  -- show cheat status anyway
        alert_text(-Border_left, Buffer_height + Border_bottom, "Cheats: allowed", COLOUR.warning, COLOUR.warning_bg,
        true, false, 0.0, 1.0)
    end
    
    -- Drag and drop sprites with the mouse
    if Cheat.is_dragging_sprite then
        Cheat.drag_sprite(Cheat.dragging_sprite_id)
        Cheat.is_cheating = true
    end
    
end


local function read_raw_input()
    -- User input data
    Previous.User_input = copytable(User_input)
    local tmp = input.get()
    for entry, value in pairs(User_input) do
        User_input[entry] = tmp[entry] or false
    end
    -- Mouse input
    tmp = input.getmouse()
    User_input.xmouse = tmp.X
    User_input.ymouse = tmp.Y
    User_input.leftclick = tmp.Left
    User_input.rightclick = tmp.Right
    -- BizHawk, custom field
    User_input.mouse_inwindow = mouse_onregion(-Border_left, -Border_top, Buffer_width + Border_right, Buffer_height + Border_bottom)
    
    -- Detect if a key was just pressed or released
    for entry, value in pairs(User_input) do
        if (value ~= false) and (Previous.User_input[entry] == false) then Keys.pressed[entry] = true
            else Keys.pressed[entry] = false
        end
        if (value == false) and (Previous.User_input[entry] ~= false) then Keys.released[entry] = true
            else Keys.released[entry] = false
        end
    end
    
    -- Key presses/releases execution:
    for entry, value in pairs(Keys.press) do
        if Keys.pressed[entry] then
            value()
        end
    end
    for entry, value in pairs(Keys.release) do
        if Keys.released[entry] then
            value()
        end
    end
    
end



--#############################################################################
-- CHEATS

-- This signals that some cheat is activated, or was some short time ago
Cheat.allow_cheats = false
Cheat.is_cheating = false
function Cheat.is_cheat_active()
    if Cheat.is_cheating then
        Text_opacity = 1.0
        Bg_opacity = 1.0
        alert_text(Buffer_middle_x - 3*BIZHAWK_FONT_WIDTH, BIZHAWK_FONT_HEIGHT, " CHEAT ", COLOUR.warning, COLOUR.warning_bg)
        Previous.is_cheating = true
    else
        if Previous.is_cheating then
            gui.addmessage("Script applied cheat") -- BizHawk
            Previous.is_cheating = false
        end
    end
end


-- Called from Cheat.beat_level()
function Cheat.activate_next_level(secret_exit)
    if u8(WRAM.level_exit_type) == 0x80 and u8(WRAM.midway_point) == 1 then
        if secret_exit then
            w8(WRAM.level_exit_type, 0x2)
        else
            w8(WRAM.level_exit_type, 1)
        end
    end
    
    Cheat.is_cheating = true
end


-- allows start + select + X to activate the normal exit
--        start + select + A to activate the secret exit 
--        start + select + B to exit the level without activating any exits
function Cheat.beat_level()
    if Is_paused and Joypad["Select"] and (Joypad["X"] or Joypad["A"] or Joypad["B"]) then
        w8(WRAM.level_flag_table + Level_index, bit.bor(Level_flag, 0x80))
        
        local secret_exit = Joypad["A"]
        if not Joypad["B"] then
            w8(WRAM.midway_point, 1)
        else
            w8(WRAM.midway_point, 0)
        end
        
        Cheat.activate_next_level(secret_exit)
    end
end


-- This function makes Mario's position free
-- Press L+R+up to activate and L+R+down to turn it off.
-- While active, press directionals to fly free and Y or X to boost him up
Cheat.under_free_move = false
function Cheat.free_movement()
    if (Joypad["L"] and Joypad["R"] and Joypad["Up"]) then Cheat.under_free_move = true end
    if (Joypad["L"] and Joypad["R"] and Joypad["Down"]) then Cheat.under_free_move = false end
    if not Cheat.under_free_move then
        if Previous.under_free_move then w8(WRAM.frozen, 0) end
        return
    end
    
    local x_pos, y_pos = u16(WRAM.x), u16(WRAM.y)
    local movement_mode = u8(WRAM.player_animation_trigger)
    local pixels = (Joypad["Y"] and 7) or (Joypad["X"] and 4) or 1  -- how many pixels per frame
    
    if Joypad["Left"] then x_pos = x_pos - pixels end
    if Joypad["Right"] then x_pos = x_pos + pixels end
    if Joypad["Up"] then y_pos = y_pos - pixels end
    if Joypad["Down"] then y_pos = y_pos + pixels end
    
    -- freeze player to avoid deaths
    if movement_mode == 0 then
        w8(WRAM.frozen, 1)
        w8(WRAM.x_speed, 0)
        w8(WRAM.y_speed, 0)
        
        -- animate sprites by incrementing the effective frame
        w8(WRAM.effective_frame, (u8(WRAM.effective_frame) + 1) % 256)
    else
        w8(WRAM.frozen, 0)
    end
    
    -- manipulate some values
    w16(WRAM.x, x_pos)
    w16(WRAM.y, y_pos)
    w8(WRAM.invisibility_timer, 127)
    w8(WRAM.vertical_scroll_flag_header, 1)  -- free vertical scrolling
    w8(WRAM.vertical_scroll_enabled, 1)
    
    Cheat.is_cheating = true
    Previous.under_free_move = true
end


-- Drag and drop sprites with the mouse, if the cheats are activated and mouse is over the sprite
-- Right clicking and holding: drags the sprite
-- Releasing: drops it over the latest spot
function Cheat.drag_sprite(id)
    if Game_mode ~= SMW.game_mode_level then Cheat.is_dragging_sprite = false ; return end
    
    local xoff, yoff = Sprites_info[id].xoff, Sprites_info[id].yoff
    local xgame, ygame = game_coordinates(User_input.xmouse - xoff, User_input.ymouse - yoff, Camera_x, Camera_y)
    
    local sprite_xhigh = floor(xgame/256)
    local sprite_xlow = xgame - 256*sprite_xhigh
    local sprite_yhigh = floor(ygame/256)
    local sprite_ylow = ygame - 256*sprite_yhigh
    
    w8(WRAM.sprite_x_high + id, sprite_xhigh)
    w8(WRAM.sprite_x_low + id, sprite_xlow)
    w8(WRAM.sprite_y_high + id, sprite_yhigh)
    w8(WRAM.sprite_y_low + id, sprite_ylow)
end


function Cheat.score()
    if not Cheat.allow_cheats then
        print("Cheats not allowed.")
        return
    end
    
    local num = forms.gettext(Options_form.score_number)
    local is_hex = num:sub(1,2):lower() == "0x"
    num = tonumber(num)
    
    if not num or num%1 ~= 0 or num < 0
    or num > 9999990 or (not is_hex and num%10 ~= 0) then
        print("Enter a valid score: hexadecimal representation or decimal ending in 0.")
        return
    end
    
    num = is_hex and num or num/10
    w24(WRAM.mario_score, num)
    
    print(fmt("Cheat: score set to %d0.", num))
    Cheat.is_cheating = true
end


-- BizHawk: modifies address <address> value from <current> to <current + modification>
-- [size] is the optional size in bytes of the address
-- TODO: [is_signed] is untrue if the value is unsigned, true otherwise
function Cheat.change_address(address, value_form, size, criterion, error_message, success_message)
    if not Cheat.allow_cheats then
        print("Cheats not allowed.")
        return
    end
    
    size = size or 1
    local max_value = 256^size - 1
    local value = Options_form[value_form] and forms.gettext(Options_form[value_form]) or value_form
    local default_criterion = function(value)
        value = tonumber(value)
        if not value or value%1 ~= 0 or value < 0 or value > max_value then
            return false
        else
            return value
        end
    end
    
    local new = default_criterion(value)
    if criterion and new then
        new = criterion(new) and new or false
    end
    if not new then
        print(error_message or "Enter a valid value.")
        return
    end
    
    local memoryf = (size == 1 and w8) or (size == 2 and w16) or (size == 3 and w24) or error"size is too big"
    memoryf(address, new)
    print(fmt("Cheat: %s set to %d.", success_message, new) or fmt("Cheat: set WRAM 0x%X to %d.", address, new))
    Cheat.is_cheating = true
end


--#############################################################################
-- MAIN --


-- Key presses:
Keys.registerkeypress("rightclick", right_click)
Keys.registerkeypress("leftclick", left_click)

-- Key releases:
Keys.registerkeyrelease("mouse_inwindow", function() Cheat.is_dragging_sprite = false end)
Keys.registerkeyrelease("leftclick", function() Cheat.is_dragging_sprite = false end)

-- Lateral gaps:
if not OLD_EMU_VERSION then
    client.SetGameExtraPadding(OPTIONS.left_gap, OPTIONS.top_gap, OPTIONS.right_gap, OPTIONS.bottom_gap)
    client.SetClientExtraPadding(0, 0, 0, 0)
end

function Options_form.create_window()
    Options_form.form = forms.newform(220, 554, "SMW Options")
    local xform, yform, delta_y = 2, 0, 20
    
    -- Top label
    Options_form.label_cheats = forms.label(Options_form.form, "You can close this form at any time", xform, yform, 200, 20)
    
    yform = yform + delta_y
    Options_form.allow_cheats = forms.checkbox(Options_form.form, "Allow cheats", xform, yform)
    forms.setproperty(Options_form.allow_cheats, "Checked", Cheat.allow_cheats)
    
    xform = xform + 105
    forms.button(Options_form.form, "Powerup", function() Cheat.change_address(WRAM.powerup, "powerup_number", 1, 
        nil, "Enter a valid integer (0-255).", "powerup")
    end, xform, yform, 58, 24)
    
    yform = yform + 2
    xform = xform + 59
    Options_form.powerup_number = forms.textbox(Options_form.form, "", 24, 16, "UNSIGNED", xform, yform, false, false)
    
    xform = 2
    yform = yform + 28
    forms.button(Options_form.form, "Score", Cheat.score, xform, yform, 43, 24)
    
    yform = yform + 2
    xform = xform + 45
    Options_form.score_number = forms.textbox(Options_form.form, fmt("0x%X", u24(WRAM.mario_score)), 48, 16, nil, xform, yform, false, false)
    
    xform = xform + 59
    forms.button(Options_form.form, "Coin", function() Cheat.change_address(WRAM.player_coin, "coin_number", 1,
        function(num) return num < 100 end, "Enter an integer between 0 and 99.", "coin")
    end, xform, yform, 43, 24)
    
    yform = yform + 2
    xform = xform + 45
    Options_form.coin_number = forms.textbox(Options_form.form, "", 24, 16, "UNSIGNED", xform, yform, false, false)
    
    xform = 2
    yform = yform + 28
    forms.button(Options_form.form, "Box", function() Cheat.change_address(0x0dc2, "item_box_number", 1,  -- unlisted WRAM
        nil, "Enter a valid integer (0-255).", "Item box")
    end, xform, yform, 43, 24)
    
    yform = yform + 2
    xform = xform + 45
    Options_form.item_box_number = forms.textbox(Options_form.form, "", 24, 16, "UNSIGNED", xform, yform, false, false)
    
    -- SHOW/HIDE
    xform = 2
    yform = yform + 28
    Options_form.label1 = forms.label(Options_form.form, "Show/hide options:", xform, yform)
    
    yform = yform + delta_y
    local y_begin_showhide = yform  -- 1st column
    Options_form.debug_info = forms.checkbox(Options_form.form, "Debug info", xform, yform)
    forms.setproperty(Options_form.debug_info, "Checked", OPTIONS.display_debug_info)
    
    yform = yform + delta_y
    Options_form.movie_info = forms.checkbox(Options_form.form, "Movie info", xform, yform)
    forms.setproperty(Options_form.movie_info, "Checked", OPTIONS.display_movie_info)
    
    yform = yform + delta_y
    Options_form.misc_info = forms.checkbox(Options_form.form, "Miscellaneous", xform, yform)
    forms.setproperty(Options_form.misc_info, "Checked", OPTIONS.display_misc_info)
    
    yform = yform + delta_y
    Options_form.player_info = forms.checkbox(Options_form.form, "Player info", xform, yform)
    forms.setproperty(Options_form.player_info, "Checked", OPTIONS.display_player_info)
    
    yform = yform + delta_y
    Options_form.sprite_info = forms.checkbox(Options_form.form, "Sprite info", xform, yform)
    forms.setproperty(Options_form.sprite_info, "Checked", OPTIONS.display_sprite_info)
    
    yform = yform + delta_y
    Options_form.sprite_hitbox = forms.checkbox(Options_form.form, "Sprite hitbox", xform, yform)
    forms.setproperty(Options_form.sprite_hitbox, "Checked", OPTIONS.display_sprite_hitbox)
    
    yform = yform + delta_y
    Options_form.sprite_tables = forms.checkbox(Options_form.form, "Sprite tables", xform, yform)
    forms.setproperty(Options_form.sprite_tables, "Checked", OPTIONS.display_miscellaneous_sprite_table)
    
    yform = yform + delta_y
    Options_form.extended_sprite_info = forms.checkbox(Options_form.form, "Extended sprites", xform, yform)
    forms.setproperty(Options_form.extended_sprite_info, "Checked", OPTIONS.display_extended_sprite_info)
    
    xform = xform + 105  -- 2nd column
    yform = y_begin_showhide
    Options_form.cluster_sprite_info = forms.checkbox(Options_form.form, "Cluster sprites", xform, yform)
    forms.setproperty(Options_form.cluster_sprite_info, "Checked", OPTIONS.display_cluster_sprite_info)
    
    yform = yform + delta_y
    Options_form.minor_extended_sprite_info = forms.checkbox(Options_form.form, "Minor ext. spr.", xform, yform)
    forms.setproperty(Options_form.minor_extended_sprite_info, "Checked", OPTIONS.display_minor_extended_sprite_info)
    
    yform = yform + delta_y
    Options_form.bounce_sprite_info = forms.checkbox(Options_form.form, "Bounce sprites", xform, yform)
    forms.setproperty(Options_form.bounce_sprite_info, "Checked", OPTIONS.display_bounce_sprite_info)
    
    yform = yform + delta_y
    Options_form.level_info = forms.checkbox(Options_form.form, "Level info", xform, yform)
    forms.setproperty(Options_form.level_info, "Checked", OPTIONS.display_level_info)
    
    yform = yform + delta_y
    Options_form.yoshi_info = forms.checkbox(Options_form.form, "Yoshi info", xform, yform)
    forms.setproperty(Options_form.yoshi_info, "Checked", OPTIONS.display_yoshi_info)
    
    yform = yform + delta_y
    Options_form.counters_info = forms.checkbox(Options_form.form, "Counters info", xform, yform)
    forms.setproperty(Options_form.counters_info, "Checked", OPTIONS.display_counters)
    
    yform = yform + delta_y
    Options_form.static_camera_region = forms.checkbox(Options_form.form, "Static camera", xform, yform)
    forms.setproperty(Options_form.static_camera_region, "Checked", OPTIONS.display_static_camera_region)
    yform = yform + delta_y  -- if odd number of show/hide checkboxes
    
    xform, yform = 2, yform + 30
    forms.label(Options_form.form, "Player hitbox:", xform, yform + 2, 70, 25)
    xform = xform + 70
    Options_form.player_hitbox = forms.dropdown(Options_form.form, {"Hitbox", "Interaction points", "Both", "None"}, xform, yform)
    xform, yform = 2, yform + 30
    
    -- DEBUG/EXTRA
    forms.label(Options_form.form, "Debug info:", xform, yform, 62, 22)
    yform = yform + delta_y
    
    local y_begin_debug = yform  -- 1st column
    Options_form.debug_player_extra = forms.checkbox(Options_form.form, "Player extra", xform, yform)
    forms.setproperty(Options_form.debug_player_extra, "Checked", OPTIONS.display_debug_player_extra)
    yform = yform  + delta_y
    
    Options_form.debug_sprite_extra = forms.checkbox(Options_form.form, "Sprite extra", xform, yform)
    forms.setproperty(Options_form.debug_sprite_extra, "Checked", OPTIONS.display_debug_sprite_extra)
    yform = yform + delta_y
    
    Options_form.debug_sprite_tweakers = forms.checkbox(Options_form.form, "Sprite tweakers", xform, yform)
    forms.setproperty(Options_form.debug_sprite_tweakers, "Checked", OPTIONS.display_debug_sprite_tweakers)
    yform = yform + delta_y
    
    Options_form.debug_extended_sprite = forms.checkbox(Options_form.form, "Extended sprites", xform, yform)
    forms.setproperty(Options_form.debug_extended_sprite, "Checked", OPTIONS.display_debug_extended_sprite)
    yform = yform + delta_y
    
    xform, yform = xform + 105, y_begin_debug
    Options_form.debug_cluster_sprite = forms.checkbox(Options_form.form, "Cluster sprites", xform, yform)
    forms.setproperty(Options_form.debug_cluster_sprite, "Checked", OPTIONS.display_debug_cluster_sprite)
    yform = yform + delta_y
    
    Options_form.debug_minor_extended_sprite = forms.checkbox(Options_form.form, "Minor ext. spr.", xform, yform)
    forms.setproperty(Options_form.debug_minor_extended_sprite, "Checked", OPTIONS.display_debug_minor_extended_sprite)
    yform = yform + delta_y
    
    Options_form.debug_bounce_sprite = forms.checkbox(Options_form.form, "Bounce sprites", xform, yform)
    forms.setproperty(Options_form.debug_bounce_sprite, "Checked", OPTIONS.display_debug_bounce_sprite)
    yform = yform + delta_y
    
    Options_form.debug_controller_data = forms.checkbox(Options_form.form, "Controller data", xform, yform)
    forms.setproperty(Options_form.debug_controller_data, "Checked", OPTIONS.display_debug_controller_data)
    --yform = yform + delta_y
    
    -- HELP:
    xform, yform = 4, yform + 30
    forms.label(Options_form.form, "Miscellaneous:", xform, yform, 78, 22)
    xform, yform = xform + 78, yform - 2
    
    Options_form.draw_tiles_with_click = forms.checkbox(Options_form.form, "Draw/erase tiles", xform, yform)
    forms.setproperty(Options_form.draw_tiles_with_click, "Checked", OPTIONS.draw_tiles_with_click)
    xform, yform = 4, yform + 30
    
    -- OPACITY
    Options_form.text_opacity = forms.label(Options_form.form, ("Text opacity: (%.0f%%, %.0f%%)"):
            format(100*Text_max_opacity, 100*Background_max_opacity), xform, yform, 135, 22)
    ;
    xform, yform = xform + 135, yform - 4
    forms.button(Options_form.form, "-", function() decrease_opacity()
        forms.settext(Options_form.text_opacity, ("Text opacity: (%.0f%%, %.0f%%)"):format(100*Text_max_opacity, 100*Background_max_opacity))
    end, xform, yform, 14, 24)
    xform = xform + 14
    forms.button(Options_form.form, "+", function() increase_opacity()
        forms.settext(Options_form.text_opacity, ("Text opacity: (%.0f%%, %.0f%%)"):format(100*Text_max_opacity, 100*Background_max_opacity))
    end, xform, yform, 14, 24)
    xform, yform = 4, yform + 25
    
    -- HELP
    Options_form.erase_tiles = forms.button(Options_form.form, "Erase tiles", function() Layer1_tiles = {}; Layer2_tiles = {} end, xform, yform)
    xform = xform + 105
    Options_form.write_help_handle = forms.button(Options_form.form, "Help", Options_form.write_help, xform, yform)
end


function Options_form.evaluate_form()
    -- Option form's buttons
    Cheat.allow_cheats = forms.ischecked(Options_form.allow_cheats) or false
    OPTIONS.display_debug_info = forms.ischecked(Options_form.debug_info) or false
    -- Show/hide
    OPTIONS.display_movie_info = forms.ischecked(Options_form.movie_info) or false
    OPTIONS.display_misc_info = forms.ischecked(Options_form.misc_info) or false
    OPTIONS.display_player_info = forms.ischecked(Options_form.player_info) or false
    OPTIONS.display_sprite_info = forms.ischecked(Options_form.sprite_info) or false
    OPTIONS.display_sprite_hitbox = forms.ischecked(Options_form.sprite_hitbox) or false
    OPTIONS.display_miscellaneous_sprite_table =  forms.ischecked(Options_form.sprite_tables) or false
    OPTIONS.display_extended_sprite_info = forms.ischecked(Options_form.extended_sprite_info) or false
    OPTIONS.display_cluster_sprite_info = forms.ischecked(Options_form.cluster_sprite_info) or false
    OPTIONS.display_minor_extended_sprite_info = forms.ischecked(Options_form.minor_extended_sprite_info) or false
    OPTIONS.display_bounce_sprite_info = forms.ischecked(Options_form.bounce_sprite_info) or false
    OPTIONS.display_level_info = forms.ischecked(Options_form.level_info) or false
    OPTIONS.display_yoshi_info = forms.ischecked(Options_form.yoshi_info) or false
    OPTIONS.display_counters = forms.ischecked(Options_form.counters_info) or false
    OPTIONS.display_static_camera_region = forms.ischecked(Options_form.static_camera_region) or false
    -- Debug/Extra
    OPTIONS.display_debug_player_extra = forms.ischecked(Options_form.debug_player_extra) or false
    OPTIONS.display_debug_sprite_extra = forms.ischecked(Options_form.debug_sprite_extra) or false
    OPTIONS.display_debug_sprite_tweakers = forms.ischecked(Options_form.debug_sprite_tweakers) or false
    OPTIONS.display_debug_extended_sprite = forms.ischecked(Options_form.debug_extended_sprite) or false
    OPTIONS.display_debug_cluster_sprite = forms.ischecked(Options_form.debug_cluster_sprite) or false
    OPTIONS.display_debug_minor_extended_sprite = forms.ischecked(Options_form.debug_minor_extended_sprite) or false
    OPTIONS.display_debug_bounce_sprite = forms.ischecked(Options_form.debug_bounce_sprite) or false
    OPTIONS.display_debug_controller_data = forms.ischecked(Options_form.debug_controller_data) or false
    -- Other buttons
    OPTIONS.draw_tiles_with_click = forms.ischecked(Options_form.draw_tiles_with_click) or false
    local button_text = forms.gettext(Options_form.player_hitbox)
    OPTIONS.display_player_hitbox = button_text == "Both" or button_text == "Hitbox"
    OPTIONS.display_interaction_points = button_text == "Both" or button_text == "Interaction points"
    
    -- Save the configurations
    if Bizhawk_loop_counter == 0 then INI.save_options() end
end


function Options_form.write_help()
        print(" - - - TIPS - - - ")
        print("MOUSE:")
        print("Use the left click to draw blocks and to see the Map16 properties.")
        print("Use the right click to toogle the hitbox mode of Mario and sprites.")
        print("\n")
        
        print("CHEATS(better turn off while recording a movie):")
        print("L+R+up: stop gravity for Mario fly / L+R+down to cancel")
        print("Use the mouse to drag and drop sprites")
        print("While paused: B+select to get out of the level")
        print("              X+select to beat the level (main exit)")
        print("              A+select to get the secret exit (don't use it if there isn't one)")
        
        print("\n")
        print("OTHERS:")
        print("If performance suffers, disable some options that are not needed at the moment.")
        print(" - - - end of tips - - - ")
end
Options_form.create_window()
Options_form.is_form_closed = false


event.unregisterbyname("smw-tas-bizhawk-onexit")
event.onexit(function()
    local destroyed = forms.destroy(Options_form.form)
    if not OLD_EMU_VERSION then
        client.SetGameExtraPadding(0, 0, 0, 0)
        client.SetClientExtraPadding(0, 0, 0, 0)
    end
    
    print("Finishing smw-bizhawk script.")
    client.paint()
end, "smw-tas-bizhawk-onexit")


while true do
    if emu.getsystemid() ~= "SNES" then
        gui.text(0, 0, "WRONG CORE: " .. emu.getsystemid(), "black", "red", "bottomright")
        
    else
        
        Options_form.is_form_closed = forms.gettext(Options_form.player_hitbox) == ""
        if not Options_form.is_form_closed then Options_form.evaluate_form() end
        
        bizhawk_status()
        bizhawk_screen_info()
        read_raw_input()
        
        -- Drawings are allowed now
        scan_smw()
        level_mode()
        overworld_mode()
        show_movie_info()
        if Is_lagged then  -- BizHawk: outside show_movie_info
            alert_text(Buffer_middle_x - 3*BIZHAWK_FONT_WIDTH, 2*BIZHAWK_FONT_HEIGHT, " LAG ", COLOUR.warning, COLOUR.warning_bg)
        end
        show_misc_info()
        show_controller_data()
        
        Cheat.is_cheat_active()
        
        mouse_actions()
        
        -- Checks if options form exits and create a button in case it doesn't
        if Options_form.is_form_closed then
            if User_input.mouse_inwindow then
                draw_rectangle(120 - 1, 0, 4*BIZHAWK_FONT_WIDTH/AR_x + 1, BIZHAWK_FONT_HEIGHT/AR_y + 1, 0xff000000, 0xff808080)  -- bizhawk
                gui.text(120*AR_x + Border_left, 0 + Border_top, "Menu")  -- unlisted color
            end
        end
        
        -- INPUT MANIPULATION
        Joypad = joypad.get(1)
        if Cheat.allow_cheats then
            Cheat.is_cheating = false
            
            Cheat.beat_level()
            Cheat.free_movement()
        else
            -- Cancel any continuous cheat
            Cheat.under_free_move = false
            
            Cheat.is_cheating = false
        end
    end
    
    -- Frame advance: hack for performance
    Bizhawk_loop_counter = (Bizhawk_loop_counter + 1)%300  -- save options each 5 seconds
    if client.ispaused() then
        emu.yield()
        gui.clearGraphics()
        gui.cleartext()
    else
        emu.frameadvance()
    end
    
end