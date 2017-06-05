local X4D_Player = LibStub:NewLibrary("X4D_Player", 2000)
if (not X4D_Player) then
	return
end
local X4D = LibStub("X4D")
X4D.Player = X4D_Player
X4D_Player.NPC =
	{
		Name = "",
		ActiveCount = 0,
		ActiveIndex = 0,
		AvailableCount = 0,
		AvailableIndex = 0,
		TrainerCount = 0,
		TrainerIndex = 0,
		OptionCount = 0,
		OptionIndex = 0
	}
X4D:RegisterForEvent("X4D_Player", "ADDON_LOADED", function (...)
	local arg0, arg1, event, name = ...
	if (name ~= "X4DWOW") then
		return
	end
	X4D_Player.Name = UnitName("player")
	X4D_Player.Class = UnitClass("player")
	X4D_Player.FocusTargetName = nil
	X4D_Player.IsFollowing = false
end)



function X4D_Player:Write(message, from)
	if (message ~= nil) then
		X4D.Log:Information(message, from)
	end
end

function X4D_Player:WriteHelpText()
	X4D.Player:Write("X4DWOW "..X4D.Version)
	X4D.Player:Write("  Usage: /xinvite [unitName]")
	X4D.Player:Write("       adds or removes unit from /XINVITE Macro")
	X4D.Player:Write("       prefix with a '-' character to remove the Unit")
	X4D.Player:Write("  Usage: /xfollow")
	X4D.Player:Write("       instructs all peers to follow you")
	X4D.Player:Write("       also sets you as party/raid leader")
	X4D.Player:Write("  Usage: /xgroup [name]")
	X4D.Player:Write("       sets the group name for the current peer")
	X4D.Player:Write("       a peer can only belong to one group")
	X4D.Player:Write("  Usage: /x4d vendor [quality]")
	X4D.Player:Write("       where 'quality' is numeric, one of:")
	for i = 0, X4D.Colors.NUM_ITEM_QUALITIES do
		local r, g, b, hex = GetItemQualityColor(i)
		local itemQualityColor = X4D.Colors:Create(r, g, b)
		local itemQualityName = X4D.Colors.ItemQualityNames[i]
		X4D.Player:Write("       "..i.." - "..itemQualityColor..itemQualityName)
	end
	X4D.Player:Write("  Usage: /x4d [command]")
	X4D.Player:Write("       where 'command' is one of:")
	X4D.Player:Write("       'on' - Turns X4D WoW AddOn features 'ON'")
	X4D.Player:Write("       'off' - Turns X4D WoW AddOn features 'OFF'")
	local enablementMessage = X4D.Colors.Text..X4D.Name.." AddOn ("..X4D.Version..")"
	if (X4D.Persistence.IsEnabled) then
		enablementMessage = enablementMessage.." is ENABLED, using Group \""..X4D.Colors.TextHighlight..X4D.Persistence.Group.Name..X4D.Colors.Text.."\""
	else
		enablementMessage = enablementMessage.." is DISABLED."
	end
	X4D.HUD:Write(enablementMessage)
	X4D.Player:Write("")
	X4D.Player:Write(enablementMessage)
end

function X4D_Player:X4DCommandHandler(parm)
	local cmd_start, cmd_stop, cmd_text, cmd_parm=string.find(string.lower(parm), "(%w+) (%w+)")
	if (cmd_text == nil) then
		cmd_start, cmd_stop, cmd_text, cmd_parm=string.find(string.lower(parm), "(%w+)")
	end
      
	if ((cmd_text == nil) or (cmd_text == "help")) then
		X4D.Player:WriteHelpText()
		return
	end

	-- enable/disable X4D
	if (cmd_text == "on") then
		X4D.Persistence.IsEnabled = true
		X4D.HUD:Write(X4D.Colors.Text..X4D.Name.." "..X4D.Version.." is using Group \""..X4D.Colors.TextHighlight..X4D.Persistence.Group.Name..X4D.Colors.Text.."\"")
		X4D.Player:Write(X4D.Colors.Text..X4D.Name.." "..X4D.Version.." is using Group \""..X4D.Colors.TextHighlight..X4D.Persistence.Group.Name..X4D.Colors.Text.."\"")
	elseif (cmd_text == "off") then
		X4D.Persistence.IsEnabled = false
		X4D.Player:X4DCommandHandler("")
		X4D.HUD:Write(X4D.Name.." "..X4D.Version.." Disabled.")
		X4D.Player:Write(X4D.Name.." "..X4D.Version.." Disabled.")
	elseif (cmd_text == "vendor") then
		if (cmd_parm ~= nil) then
			local qualityLevel = tonumber(cmd_parm)
			if (qualityLevel ~= nil) then
				if ((qualityLevel >= 0) and (qualityLevel <= 5)) then
					X4D.Persistence.MaxQualityAutoVendor = qualityLevel
					X4D.Player:Write("AutoVendor Quality Set to \""..X4D.Colors.TextHighlight..qualityLevel..X4D.Colors.Text.."\"")
					return
				end
			end
		end
		X4D.Player:Write("  Usage: /x4d vendor [quality]")
		X4D.Player:Write("       where 'quality' is numeric, one of:")
		for i = 0, X4D.Colors.NUM_ITEM_QUALITIES do
			local r, g, b, hex = GetItemQualityColor(i)
			local itemQualityColor = X4D.Colors:Create(r, g, b)
			local itemQualityName = X4D.Colors.ItemQualityNames[i]
			if (X4D.Persistence.MaxQualityAutoVendor == i) then
				X4D.Player:Write("==> "..i.." - "..itemQualityColor..itemQualityName..X4D.Colors.Text.." (current)")
			else
				X4D.Player:Write("       "..i.." - "..itemQualityColor..itemQualityName)
			end
		end
	elseif (cmd_text == "reload") then
		ReloadUI()
	elseif (cmd_text == "sort") then
		X4D.Inventory:Sort()
	elseif (cmd_text == "test") or (cmd_text == "debug") then
		X4D.Log:SetTraceLevel(X4D.Log.TRACE_LEVELS.DEBUG)
		X4D.Log:Debug("Debug Logging Enabled", "X4D");
		-- TODO: implement some tests
	end
end
