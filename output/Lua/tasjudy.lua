--
-- TASJudy - Judges so you don't have to.
-- Author: Raiscan
-- Timestamp: 201508041957
-- Version: 0.1
-- To change this template use File | Settings | File Templates.
--

local resultsFilePath = "F:\\competition\\results\\results.txt"
local gracePeriod = 10000
local gracePeriodCounter = 0
local separator = ","
local exitOnResults = true

local FAILURE_DOES_NOT_FINISH = "DNF"
local FAILURE_DISQUALIFIED = "DQ"

-- Main Function --
function judge()
	if movie.startsfromsavestate() then
		console.log("Movie starts from savestate, so disqualifying")

		writeFailureToResults(FAILURE_DISQUALIFIED)

		endScript()
		return
	end

	while not hasCompletedGame() do

		if movieHasFinished() then
			if gracePeriodLimiter() then
				console.log("Movie does not finish the game :(")
				writeFailureToResults(FAILURE_DOES_NOT_FINISH)

				endScript()
				return
			end
		end

		--The show must go on
		emu.frameadvance()
	end

	--We must have finished!
	console.log("Movie finished game.")
	writeSuccessToResults(emu.framecount(), getInGameTime())

	endScript()
end


-------- HELPER FUNCTIONS BELOW ---------

-- Edit this with game completion criteria as necessary --
function hasCompletedGame()
    return mainmemory.read_s8(0x002C) == 1
end


-- Edit this with functionality for in game time --
function getInGameTime()
    local millis = bizstring.hex(mainmemory.read_u8(0x0050)):reverse()
    local seconds = bizstring.hex(mainmemory.read_u8(0x004F)):reverse()
    local minutes = bizstring.hex(mainmemory.read_u8(0x004E)):reverse()

    return minutes .. ":" .. seconds .. "." .. millis
end


-- Ends the script. If exitOnResults set, exits emulator.
function endScript()
    client.pause()
    if exitOnResults then
        client.closerom()
        client.exitCode(emu.framecount())
    end
end

-- Parses the hash of the movie file calculated by RGamma's uploader.
function parseHash()
    local moviePath = movie.filename()
    local movieFile = moviePath:match("([^\\]+)$")
    local hash = movieFile:match("^([^.]+)")

    return hash
end


-- Alias for writing results; blanks out times with reason.
function writeFailureToResults(reason)

    writeResults(parseHash(), movie.getheader()["Author"], reason, reason, reason)
end

-- Alias for writing results; takes in endFrame and in game time --
function writeSuccessToResults(endFrame, inGameTime)

    writeResults(parseHash(),  movie.getheader()["Author"], endFrame, movie.length(), inGameTime)
end

function movieHasFinished() 
	return movie.mode() == "FINISHED" or not movie.isloaded()
end

-- Opens results file and writes a single line of all information gathered --
function writeResults(hash, author, endFrame, length, inGameTime)
    local resultsFile, err = io.open(resultsFilePath, "a")

    if err then
        console.log("Could not write results " .. err)
    else
        local hash = parseHash()
        local resultsLine = hash ..
                separator ..
                author ..
                separator ..
                endFrame ..
                separator ..
                length ..
                separator ..
                inGameTime


        resultsFile:write(resultsLine .. "\n")
        resultsFile:close()
    end
end

function gracePeriodLimiter() 
	if gracePeriodCounter < gracePeriod then
		gracePeriodCounter = gracePeriodCounter + 1
	end
	
	return gracePeriodCounter >= gracePeriod
end

judge()
