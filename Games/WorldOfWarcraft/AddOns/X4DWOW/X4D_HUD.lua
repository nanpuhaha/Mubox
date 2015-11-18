local X4D_HUD = LibStub:NewLibrary("X4D_HUD", 2000)
if (not X4D_HUD) then
	return
end
local X4D = LibStub("X4D")
X4D.HUD = X4D_HUD

function X4D_HUD:Write(message)
	if (message ~= nil) then
		UIErrorsFrame:AddMessage(message, 1.0, 1.0, 0.0, 1.0, UIERRORS_HOLD_TIME)
	end
end
