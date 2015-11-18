local X4D = LibStub("X4D")

SLASH_X4D1 = "/x4d"
SLASH_XGROUP1 = "/xgroup"
SLASH_XFOLLOW1 = "/xfollow"
SLASH_XINVITE1 = "/xinvite"
SLASH_XSORT1 = "/xsort"
SLASH_XBG1 = "/xbg"
SlashCmdList["X4D"] = (function(...) 
	X4D.Player:X4DCommandHandler(...)
end)
SlashCmdList["XFOLLOW"] = (function(...) 
	X4D.Group:FollowCommandHandler(...)
end)
SlashCmdList["XGROUP"] = (function(...) 
	X4D.Group:GroupNameCommandHandler(...)
end)
SlashCmdList["XINVITE"] = (function(...) 
	X4D.Group:MassInvite(...)
end)
SlashCmdList["XSORT"] = (function(...) 
	X4D.Inventory:Sort(...)
end)
SlashCmdList["XBG"] = (function(...) 
	X4D.PvP:DumpBattlegroundInfo(...)
end)

RegisterAddonMessagePrefix("EXBLZZ")

X4D.Frame:SetScript("OnEvent", function (...)
	X4D:OnEvent(...)
end)

X4D.Frame:SetScript("OnUpdate", function (...)
	X4D:OnUpdate(...)
end)

--print("X4D Loaded.")