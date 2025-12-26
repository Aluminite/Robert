--- Robert interface for Mesen.

local function argb_to_luma(argb)
    --- Follows Rec. 709 luma function.
    local r = (argb & 0x00ff0000) >> 16
    local g = (argb & 0x0000ff00) >> 8
    local b = argb & 0x000000ff
    return r * 0.2126 + g * 0.7152 + b * 0.0722
end

local function average_luma(pixels)
    local total_luma = 0.0
    for _, pixel in ipairs(pixels) do
        total_luma = total_luma + argb_to_luma(pixel)
    end
    return total_luma / #pixels
end

local function screen_brightness()
    --- Samples color values from a 4x4 grid, then averages their luminances and returns the result
    local xvals = {53, 103, 153, 203}
    local yvals = {60, 100, 140, 180}
    local pixels = {}

    for _, x in ipairs(xvals) do
        for _, y in ipairs(yvals) do
            table.insert(pixels, emu.getPixel(x, y))
        end
    end
    return average_luma(pixels)
end

local function is_bright(brightness)
    --- 0x2a (light green used for commands) is ~163
    --- 0x19 (dark green used for test) is ~96
    local threshold = 80
    if brightness >= threshold then
        return true
    else
        return false
    end
end

local function bits_follow_pattern(bits)
	--- Special case for test mode
	if bits == 0x1555 or bits == 0xaaa then --- 0b1010101010101 and 0b0101010101010
		return true
	end

    local bits_mask = 0x1faa      --- 0b1111110101010
    local correct_pattern = 0x2aa --- 0b0001010101010
    if (bits & bits_mask) == correct_pattern then 
        return true
    else 
        return false 
    end
end

local function extract_command(bits)
	--- Special case for test mode
	if bits == 0x1555 or bits == 0xaaa then --- 0b1010101010101 and 0b0101010101010
		return 0
	end
	
    local w = (bits & 0x40) >> 3 --- 0b0000001000000
    local x = (bits & 0x10) >> 2 --- 0b0000000010000
    local y = (bits & 0x04) >> 1 --- 0b0000000000100
    local z = (bits & 0x01)      --- 0b0000000000001
    return w + x + y + z
end

local p2_inputs = {["a"] = false, ["b"] = false}
local rob_bits = 0x0
local rob_commands = {"Blink LED", "Reset", "Down one", "Invalid", "Left", "Up two", "Close arms", "Invalid", "Right", 
                      "LED on", "Open arms", "Invalid", "Up one", "Down two", "Invalid", "Invalid"}

--- Set up the server
local socket = require("socket.core")
local server = socket.tcp()
server:bind("127.0.0.1", 8012)
server:listen(0)
server:settimeout(0)
local ip, port = server:getsockname()
emu.log(string.format("Server is listing on %s:%s", ip, port))

local client = nil

local function frame_callback()
    --- Attempt to make a connection if there currently isn't one
    if client == nil then
        local err
        client, err = server:accept()
        if client ~= nil and err ~= "timeout" then
            client:settimeout(0)
            emu.log("Connected")
        end
    end
    
    --- Get data if there is a connection
    local response = nil
    if client ~= nil then
        local err
        response, err = client:receive(1)
        if err == "closed" then
            emu.log("Disconnected")
            client = nil
        end
    end
    
    --- Press buttons if there is incoming data
    if response ~= nil then
        if response == "A" then
            p2_inputs["a"] = true
            emu.log("Pressing A")
        elseif response == "a" then
            p2_inputs["a"] = false
            emu.log("Releasing A")
        elseif response == "B" then
            p2_inputs["b"] = true
            emu.log("Pressing B")
        elseif response == "b" then
            p2_inputs["b"] = false
            emu.log("Releasing B")
        end
    end
    
    --- Process screen flashes
    rob_bits = (rob_bits << 1) & 0x1fff --- 0b1111111111111
    if is_bright(screen_brightness()) then
        rob_bits = rob_bits | 0x1
    end
    
    --- Check the current command in the buffer
    if bits_follow_pattern(rob_bits) then
        local cmd = extract_command(rob_bits)
        rob_bits = 0x1fff
        emu.log(rob_commands[cmd + 1])
        
        if client ~= nil then 
            client:send(string.format("%x", cmd)) 
        end
    end
end

local function controller_callback()
    emu.setInput(p2_inputs, 0, 1)
end

local function disconnect()
    if client ~= nil then
        client:close()
    end
end

emu.addEventCallback(frame_callback, emu.eventType.endFrame)
emu.addEventCallback(controller_callback, emu.eventType.inputPolled)
emu.addEventCallback(disconnect, emu.eventType.scriptEnded)