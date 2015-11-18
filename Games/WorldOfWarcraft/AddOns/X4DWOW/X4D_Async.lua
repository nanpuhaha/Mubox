local X4D_Async = LibStub:NewLibrary("X4D_Async", 2000)
if (not X4D_Async) then
	return
end
local X4D = LibStub("X4D")
X4D.Async = X4D_Async

local _nextTimerId = 0
local _timers = {}
X4D_Async.ActiveTimers = X4D.DB:Create(_timers)

local X4D_Timer = {}

--- <summary>
--- <para>Create a new instance of X4D_Timer</para>
--- <para>Timer continues to elapse until stopped.</para>
--- </summary>
--- <params>
--- <param name="callback">the callback to execute when the timer elapses, receives a reference to Timer instance and user state object</param>
--- <param name="interval">the interval at which the timer elapses</param>
--- </params>
function X4D_Timer:New(callback, interval, state)
    local timerId = _nextTimerId
    _nextTimerId = _nextTimerId + 1
	local proto = {
        _id = timerId,
        _timestamp = 0,
--        _memory = 0, -- for diagnostic purposes only, requires retuning of GC to be of any value, and so commented out for Release
		_enabled = false,
		_callback = callback or (function(L_timer) self:Stop() end),
		_interval = interval or 1000,
		_state = state or {},
	}
	setmetatable(proto, { __index = X4D_Timer })
	return proto
end

function X4D_Timer:IsEnabled()
	return self._enabled
end

function X4D_Timer:IsElapsed()
	local timeNow = debugprofilestop()
	local elapsed = (timeNow - self._timestamp)
	return elapsed >= self._interval
end

function X4D_Timer:Elapsed()
--    local memory = collectgarbage("count") -- TODO: only perform this count when debug mode has been set
	if (not self._callback) then
		self:Stop()
		return
	end
	local success, err = pcall(self._callback, self, self._state)
	if (not success) then
		X4D.Log:Error(err, self.Name)
		return
	end
--    memory = (collectgarbage("count") - memory)
--    if (memory >= 100 or memory <= -100) then
--        X4D.Log:Debug(self.Name .. " memory delta: " .. (memory * 1024))
--    end
--    self._memory = memory
end

-- "state" is passed into timer callback
-- "interval" is optional, and can be used to change the timer interval during execution
function X4D_Timer:Start(interval, state, name)
    if (name ~= nil) then
        self.Name = name
    elseif (self.Name == nil) then
        self.Name = "$" .. tostring(math.floor(debugprofilestop()))
    end
	if (state ~= nil) then
		self._state = state
	elseif (self._state == nil) then
        self._state = {}
    end
	if (interval ~= nil) then
		self._interval = interval
    elseif (self._interval == nil) then
        self._interval = 1000
	end
	if (self._enabled) then
		return
	end
	self._enabled = true
    X4D_Async.ActiveTimers:Add(self._id, self)
    return self
end

function X4D_Timer:Stop()
	self._enabled = false
    X4D_Async.ActiveTimers:Remove(self._id)
    return self
end

setmetatable(X4D_Timer, { __call = X4D_Timer.New })

function X4D_Async:CreateTimer(callback, interval, state)
    return X4D_Timer:New(callback, interval, state)
end

local function DoTimerElapsed(timer, key)
	if (timer:IsEnabled() and timer:IsElapsed()) then
	    timer._timestamp = debugprofilestop()
        timer:Elapsed(key)
	end
end

local _frame = CreateFrame("FRAME", "X4D_Async$FRAME")
local _lastUpdate = 0
local function OnUpdate(self, elapsed)
	local startTime = math.floor(debugprofilestop())
	if ((startTime - _lastUpdate) > 100) then
		_lastUpdate = startTime
		--X4D.Log:Debug("X4D_Async$FRAME "..startTime)
		X4D_Async.ActiveTimers
			:ForEach(DoTimerElapsed)
	end
end
_frame:SetScript("OnUpdate", OnUpdate)
