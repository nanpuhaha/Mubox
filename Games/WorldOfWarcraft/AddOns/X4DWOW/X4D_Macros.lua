local X4D_Macros = LibStub:NewLibrary("X4D_Macros", 2000)
if (not X4D_Macros) then
	return
end
local X4D = LibStub("X4D")
X4D.Macros = X4D_Macros

function X4D_Macros:CreateOrEditMacro(macro_name, macro_body)
	if (InCombatLockdown()) then
		return
	end
	local index = GetMacroIndexByName(macro_name)
	if (index ~= 0) then
		EditMacro(index, macro_name, nil, macro_body)
	else
		CreateMacro(macro_name, 1, macro_body, 1)
	end
end

function X4D_Macros:MacroizeFollow()
	X4D.Macros:CreateOrEditMacro("X4DFOLLOW", "/xfollow")
end

function X4D_Macros:MacroizeFocus(unitName)
	if (unitName ~= nil) then
		X4D.Macros:CreateOrEditMacro("X4DFOCUS", "/focus "..unitName.."\r\n/assist focus")
	else
		X4D.Macros:CreateOrEditMacro("X4DFOCUS", "/clearfocus")
	end
end

function X4D_Macros:MacroizeTarget(unitName)
	if (unitName ~= nil) then
		X4D.Macros:CreateOrEditMacro("X4DTARGET", "/target "..unitName.."-target")
	else
		X4D.Macros:CreateOrEditMacro("X4DTARGET", "/script SetRaidTarget(\"target\", 8)")
	end
end

function X4D_Macros:MacroizeInvite(unitName)
	-- manage invite list if unit name was supplied
	if ((unitName ~= nil) and (string.len(unitName) > 0)) then
		if (string.sub(unitName, 1, 1) ~= "-") then
			-- CreateOrUpdate
			if (X4D.Persistence.InviteList == nil) then
				-- first-time use, initialize
				X4D.Persistence.InviteList = " "..string.lower(unitName).." "
			elseif (string.find(X4D.Persistence.InviteList, "%s"..string.lower(unitName).."%s") == nil) then
				-- add if not exists
				X4D.Persistence.InviteList = X4D.Persistence.InviteList..string.lower(unitName).." "
			end
		else
			-- Delete
			if (X4D.Persistence.InviteList ~= nil) then
				local adjustedUnitName = string.sub(unitName, 2, string.len(unitName) - 1)
				adjustedUnitName = "%s"..string.lower(adjustedUnitName).."%s"
				X4D.Persistence.InviteList = string.gsub(X4D.Persistence.InviteList, adjustedUnitName, " ")
			end
		end
	end
	-- regenerate macro from current invite list
	local it_inviteList = X4D.Persistence.InviteList
	if (it_inviteList ~= nil) then
		local macro = "/xfollow\r\n"
		for param in string.gmatch(it_inviteList, "[^%s]+") do
			macro = macro.."/invite "..param.."\r\n"
		end			
		if (macro ~= "/xfollow\r\n") then
			macro = macro.."/xfollow\r\n"
		end
		X4D.Macros:CreateOrEditMacro("X4DINVITE", macro)
	end
end
