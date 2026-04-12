local id = nil
local function frameEnd()
	event.unregisterbyid(id)
end

id = event.onframeend(frameEnd)
