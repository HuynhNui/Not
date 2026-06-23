local spritePath = "C:/Users/Artermis/My project/Assets/_Project/Art/Maincharacter/UNIT07.aseprite"

local spr = app.open(spritePath)
if not spr then
  error("Could not open sprite: " .. spritePath)
end

local layer = spr.layers[1]
if not layer then
  error("Sprite has no editable layer")
end

local function c(r, g, b, a)
  return Color { r = r, g = g, b = b, a = a or 255 }
end

local transparent = c(0, 0, 0, 0)
local outline = c(22, 38, 56)
local deep = c(54, 59, 78)
local cloth = c(239, 239, 239)
local cloth_hi = c(228, 239, 253)
local cloth_shadow = c(119, 137, 164)
local hair_blue = c(169, 214, 242)
local red = c(212, 75, 64)
local boot = c(18, 3, 23)
local leg_mid = c(32, 124, 227)
local leg_dark = c(26, 81, 164)

local function px(img, x, y, color)
  if x >= 0 and x < spr.width and y >= 0 and y < spr.height then
    img:drawPixel(x, y, color)
  end
end

local function rect(img, x1, y1, x2, y2, color)
  for y = y1, y2 do
    for x = x1, x2 do
      px(img, x, y, color)
    end
  end
end

local function clearLower(img)
  for y = 23, 31 do
    for x = 8, 23 do
      px(img, x, y, transparent)
    end
  end
end

local function drawLeg(img, x, hipY, kneeY, footY, footShift, highlight)
  rect(img, x, hipY, x + 4, kneeY, outline)
  rect(img, x + 1, hipY + 1, x + 3, kneeY, leg_dark)
  rect(img, x + 2, hipY + 1, x + 3, kneeY - 1, leg_mid)
  if highlight then
    px(img, x + 3, hipY + 1, cloth_hi)
  end
  rect(img, x + footShift, kneeY + 1, x + footShift + 3, footY, outline)
  rect(img, x + footShift + 1, kneeY + 1, x + footShift + 2, footY - 1, leg_mid)
  rect(img, x + footShift - 1, footY, x + footShift + 4, footY, boot)
end

local function drawBelt(img)
  rect(img, 11, 23, 20, 23, deep)
  px(img, 10, 23, outline)
  px(img, 21, 23, outline)
end

local function drawNeutral(img)
  drawBelt(img)
  drawLeg(img, 10, 24, 27, 30, 0, true)
  drawLeg(img, 17, 24, 27, 30, 0, false)
  px(img, 15, 25, transparent)
  px(img, 16, 25, transparent)
end

local function drawLeftContact(img)
  drawBelt(img)
  drawLeg(img, 9, 24, 27, 31, 0, true)
  drawLeg(img, 17, 24, 26, 28, 1, false)
  rect(img, 17, 29, 20, 29, cloth_hi)
end

local function drawLeftPass(img)
  drawBelt(img)
  drawLeg(img, 10, 24, 26, 29, -1, true)
  drawLeg(img, 17, 24, 27, 30, 0, false)
  rect(img, 9, 30, 14, 30, boot)
end

local function drawRightContact(img)
  drawBelt(img)
  drawLeg(img, 10, 24, 26, 28, -1, true)
  drawLeg(img, 18, 24, 27, 31, 0, false)
  rect(img, 11, 29, 14, 29, cloth_hi)
end

local function drawRightPass(img)
  drawBelt(img)
  drawLeg(img, 10, 24, 27, 30, 0, true)
  drawLeg(img, 17, 24, 26, 29, 1, false)
  rect(img, 17, 30, 22, 30, boot)
end

local drawers = {
  drawNeutral,
  drawLeftContact,
  drawLeftPass,
  drawNeutral,
  drawRightContact,
  drawRightPass,
  drawNeutral,
}

local function findCel(targetFrame)
  for _, cel in ipairs(spr.cels) do
    if cel.layer == layer and cel.frame == targetFrame then
      return cel
    end
  end
  return nil
end

app.transaction(function()
  for i, draw in ipairs(drawers) do
    local frame = spr.frames[i + 6]
    local cel = findCel(frame)
    if cel then
      local img = Image(cel.image)
      clearLower(img)
      draw(img)
      cel.image = img
    end
  end
end)

app.command.SaveFile()
