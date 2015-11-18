local X4D_PVP = LibStub:NewLibrary("X4D_PVP", 2000)
if (not X4D_PVP) then
	return
end
local X4D = LibStub("X4D")
X4D.PvP = X4D_PVP


function X4D_PVP:EndJoinBattleground(event)
	local joinAsGroup = CanJoinBattlefieldAsGroup()
	if ((X4D.Group.Leader == nil) and joinAsGroup) then
		local rated = false
		JoinBattlefield(0, joinAsGroup, rated)
	end
	self:UnregisterEvent("PVPQUEUE_ANYWHERE_SHOW")
	self.Frame = nil
end

function X4D_PVP:BeginJoinBattleground(id)
	if (not id) then
		id = 1 -- Alterac
	end
	local maxBattlegroundTypes = GetNumBattlegroundTypes()
	for index = 1, maxBattlegroundTypes do
		local name, canEnter, isHoliday, isRandom, battlegroundId = GetBattlegroundInfo(index)
		if (canEnter and (battlegroundId == id)) then
			local status = GetBattlefieldStatus(battlegroundId)
			if (status == nil) then
				status = "none"
			end
			if ((not canEnter) or (status == "active") or (status == "queued") or (status == "confirm")) then
				return
			end
			local frame = self.Frame
			if (frame == nil) then
				frame = CreateFrame("FRAME")
				frame:SetScript("OnEvent", function(...)
					X4D_PVP:EndJoinBattleground(...)
				end)
				self.Frame = frame
			end
			if (frame ~= nil) then
				if (not frame:IsEventRegistered("PVPQUEUE_ANYWHERE_SHOW")) then
					frame:RegisterEvent("PVPQUEUE_ANYWHERE_SHOW")
					RequestBattlegroundInstanceInfo(index)
				end
			end
			return
		end
	end
end

function X4D_PVP:TryQueuePvP()
	-- defer
	if ((self.NextCheckTime ~= nil) and (self.NextCheckTime > time())) then
		return
	end
	self.NextCheckTime = time() + 20 -- TODO: config, allow another check in 20 seconds

	-- constrain
	if (not X4D.Persistence.IsEnabled) then
		return
	end
	if (X4D.Persistence.PvP.Battleground.AutoQueue == nil) then
		return
	end
	if (UnitInBattleground("player")) then
		return
	end

	-- execute
	self:BeginJoinBattleground(X4D.Persistence.PvP.Battleground.AutoQueue)
end

function X4D_PVP:DumpBattlegroundInfo(parm)
	local cmd_start, cmd_stop, cmd_text, cmd_parm=string.find(string.lower(parm), "(%w+) (%w+)")
	if (cmd_text == nil) then
		cmd_start, cmd_stop, cmd_text, cmd_parm=string.find(string.lower(parm), "(%w+)")
	end
	
	if (cmd_text ~= nil) then
		if (cmd_text == "off") then
			X4D.Persistence.PvP.Battleground.AutoQueue = nil
		else
			X4D.Persistence.PvP.Battleground.AutoQueue = tonumber(cmd_text)
		end
	end

	local maxBattlegroundTypes = GetNumBattlegroundTypes()
	for battlegroundTypeId = 1, maxBattlegroundTypes do
		local name, canEnter, isHoliday, isRandom, battlegroundId = GetBattlegroundInfo(battlegroundTypeId)
		if (name ~= nil) then
			local status = GetBattlefieldStatus(battlegroundTypeId)
			if (status == nil) then
				status = "none"
			end
			if (battlegroundId == nil) then
				battlegroundId = 0
			end
			local logString = "BG ["..battlegroundTypeId.."] "..battlegroundId.."/"..name.." ("..status..")"
			if (isHoliday) then
				logString = logString.." (holiday)"
			end
			if (not canEnter) then
				logString = logString.." (locked)"
			end
			if (battlegroundId == X4D.Persistence.PvP.Battleground.AutoQueue) then
				logString = logString.." (AUTO)"
				X4D.HUD:Write("BG Auto Queue Enabled for '"..name.."'")
			end
			X4D.Player:Write(logString)
		end
	end

	if (not X4D.Persistence.PvP.Battleground.AutoQueue) then
		X4D.HUD:Write("BG Auto Queueing Disabled")
	end
end
