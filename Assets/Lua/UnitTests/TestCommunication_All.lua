function round(num, numDecimalPlaces)
  local mult = 10^(numDecimalPlaces or 0)
  return math.floor(num * mult + 0.5) / mult
end

function get_baseline()
	i = 100
	client.reboot_core()
	t = os.clock()

	while i > 0 do
		emu.frameadvance()
		i = i - 1
	end
	baseline = os.clock() - t
	print('Baseline:            ' .. round(baseline, 3) .. " secs")
	return baseline
end

function test_mmf()
	i = 100
	client.reboot_core()
	t = os.clock()
	while i > 0 do
		emu.frameadvance()
		comm.mmfScreenshot()
		i = i - 1
	end
	print('Memory mapped files: ' .. round((os.clock() - t - baseline), 3) .. " secs")
end

function test_http()
	print("Testing HTTP server")
	client.reboot_core()
	i = 100
	t = os.clock()
	
	while i > 0 do
		emu.frameadvance()
		comm.httpTestGet()
		i = i - 1
	end
	print('HTTP get:            ' ..  round((os.clock() - t - baseline), 3) .. " secs")
	
	client.reboot_core()
	i = 100
	t = os.clock()
	
	while i > 0 do
		emu.frameadvance()
		comm.httpPostScreenshot()
		i = i - 1
	end
	print('HTTP post:           ' ..  round((os.clock() - t - baseline), 3) .. " secs")

end

function test_socket()

	i = 100
	client.reboot_core()
	t = os.clock()
	while i > 0 do
		emu.frameadvance()
		comm.socketServerScreenShot()
		i = i - 1
	end
	print('Socket server:       ' ..  round((os.clock() - t - baseline), 3) .. " secs")
end

function test_socketresponse()
	best_time = -100
	timeouts = {1, 2, 3, 4, 5, 10, 20, 25, 50, 100, 250, 500, 1000}
	comm.socketServerSetTimeout(1000)
	resp = comm.socketServerScreenShotResponse()
	for t, timeout in ipairs(timeouts) do
		comm.socketServerSetTimeout(timeout)
		client.reboot_core()
		print("Trying to find minimal timeout for Socket server")
		i = 100
		t = os.clock()
		while i > 0 do
			emu.frameadvance()
			resp = comm.socketServerScreenShotResponse()
			if resp ~= 'ack' then
				i = -100
				print(resp)
				print("Failed to a get a proper response")
			end
			i = i - 1
		end
		if i > -100 then
			print("Best timeout: " .. timeout .. " msecs")
			print("Best time:    " .. round((os.clock() - t - baseline), 3) .. " secs")
			break
		end
	end
	
end

function test_http_response()
	err = false
	print("Testing HTTP server response")
	client.reboot_core()
	i = 100
	
	while i > 0 do
		emu.frameadvance()
		resp = comm.httpTestGet()
		if resp ~= "<html><body><h1>hi!</h1></body></html>" then
			print("Failed to get correct HTTP get response")
			print(resp)
			i = 0
			err = true
		end
		i = i - 1
	end
	if not err then
		print("HTTP GET looks fine: No errors occurred")
	end
	
	client.reboot_core()
	i = 100
	err = false
	while i > 0 do
		emu.frameadvance()
		resp = comm.httpPostScreenshot()
		if resp ~= "<html><body>OK</body></html>" then
			print("Failed to get correct HTTP post response")
			print(resp)
			i = 0
			err = true
		end
		i = i - 1
	end
	if not err then
		print("HTTP POST looks fine: No errors occurred")
	end
end

baseline = get_baseline()
test_socket()
test_mmf()
test_http()
print("#####################")
test_http_response()
test_socketresponse()
print()

