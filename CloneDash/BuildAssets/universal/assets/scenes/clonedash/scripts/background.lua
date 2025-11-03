HEIGHT      = 1600
HALF_HEIGHT = 800

local s01_arrow = textures:loadMuseDashSprite("s01_arrow")
s01_arrow.wrap = TEXTURE_WRAP_REPEAT

local s01_well_side = textures:loadMuseDashSprite("s01_well_side")
s01_well_side.wrap = TEXTURE_WRAP_REPEAT

local s01_floor_1     = textures:loadMuseDashSprite("s01_floor_1")
local s01_floor_2     = textures:loadMuseDashSprite("s01_floor_2")
local s01_floor_light = textures:loadMuseDashSprite("s01_floor_light")

local pink                  = Color(225, 5, 70)
local pink_end              = Color(155, 5, 70, 0)
local purple                = Color(49, 32, 78)
local purple_grad           = Color(20, 7, 25, 255)
local purple_grad_end       = Color(42, 18, 40, 0)

local size_of_top_gradient  = 2
local size_of_left_gradient = 1 / 4
local width                 = 2000

local function renderBackground()
    -- The main background rectangle
    graphics:setDrawColor(purple.r, purple.g, purple.b, purple.a)
    graphics:drawRectangle(-width, -HALF_HEIGHT, width * 2, HEIGHT)
end

local function renderVerticalGradients()
    -- The top and bottom gradients
    graphics:drawGradientV(-width, -HALF_HEIGHT, width * 2, HALF_HEIGHT / size_of_top_gradient, purple_grad, purple_grad_end)
    graphics:drawGradientV(-width, HALF_HEIGHT / size_of_top_gradient, width * 2, HALF_HEIGHT / size_of_top_gradient, purple_grad_end, purple_grad)
end

local function renderHorizontalGradients()
    -- The left-side gradient
    graphics:drawGradientH(-width * 0.9, -HALF_HEIGHT, width * 2 * size_of_left_gradient, HEIGHT, pink, pink_end)
end

local function drawScrollingTextureV(x, y, w, h, scroll, tex, flip)
    graphics:setTexture(tex)
    local widthRatio = w / tex.width

    local additive = (scroll * widthRatio) % tex.width

    local s = additive
    local e = ((h / tex.height) / widthRatio) + additive

    graphics:drawTextureUV(x, y, w, h, 0, flip and e or s, 1, flip and s or e)
end

local s01_well_1 = textures:loadMuseDashSprite("s01_well_1")
local s01_well_2 = textures:loadMuseDashSprite("s01_well_2")
local s01_well_3 = textures:loadMuseDashSprite("s01_well_3")

local wells = {s01_well_1, s01_well_2, s01_well_3}

local eachPieceW = 512
local eachPieceH = 256
local function oneWell(xOffset, speed, inPattern)
    local well = {}
    local MAX_PIECES = 10
    well.pattern = inPattern

    function well:render(wellX, time)
        local pattern = self.pattern
        local len = #pattern
        local scroll = ((time + 100) * 64 * speed)
        local yOffset = scroll % (eachPieceH * len)

        for i = -len, len do
            local texID = pattern[(i % len) + 1]
            if texID ~= 0 then
                local tex = wells[texID]
                graphics:setTexture(tex)
                graphics:drawTexture((wellX - 512) + xOffset, ((i * eachPieceH) + yOffset) - (eachPieceH * (MAX_PIECES / 2)), eachPieceW, eachPieceH)
            end
        end
    end

    return well
end

local well_1 = oneWell(0, 1.1, {1, 1, 3, 1, 1, 1, 1, 1, 1})
local well_2 = oneWell(-eachPieceW * 1, 0.8, {1, 1, 3, 3, 1, 1, 0, 3, 1})
local well_3 = oneWell(-eachPieceW * 2, 0.92, {1, 1, 2, 0, 1, 1, 1, 2, 1})
local well_4 = oneWell(-eachPieceW * 3, 0.86, {3, 1, 1, 1, 1, 0, 1, 0, 1})

local function renderWell()
    graphics:setDrawColor(255, 255, 255, 255)

    local wellX = (math.sin(conductor.time / 8) * 180) - 500

    well_1:render(wellX, conductor.time * 2)
    well_2:render(wellX, conductor.time * 2)
    well_3:render(wellX, conductor.time * 2)
    well_4:render(wellX, conductor.time * 2)

    graphics:setTexture(s01_well_side)
    graphics:drawTexture(wellX, -HEIGHT, s01_well_side.width * 2, HEIGHT * 2)

    local arrowsY = conductor.time
    drawScrollingTextureV(wellX + 16 + 1, -HEIGHT, 45 - 2, HEIGHT * 2, -arrowsY, s01_arrow, true)
    drawScrollingTextureV(wellX + 16 + 1 + 56, -HEIGHT, 45 - 2, HEIGHT * 2, -arrowsY, s01_arrow, false)
    drawScrollingTextureV(wellX + 16 + 1 + (56 * 2), -HEIGHT, 45 - 2, HEIGHT * 2, -arrowsY, s01_arrow, true)
end

local function renderRoad()
    graphics:setDrawColor(255, 255, 255, 255)
    local roadX = conductor.time * 1024
    local eachPieceW2 = 256

    local movement = (roadX % eachPieceW2)
    local texOffset = math.floor(roadX / eachPieceW2)
    for i = 16, -3, -1 do
        local isFirst = ((i + texOffset) % 3) == 0
        local tex = isFirst == true and s01_floor_2 or s01_floor_1 -- lua interpreter bug I need to report later: isFirst doesn't work here?

        local w = tex.width * 2
        local h = tex.height * 2

        graphics:setDrawColor(255, 255, 255, 255)
        graphics:setTexture(tex)
        graphics:drawTexture( -1500 + (i * eachPieceW2 - movement), 333, w, h)

        if isFirst then
            graphics:setTexture(s01_floor_light)
            local w2 = s01_floor_light.width * 2
            local h2 = s01_floor_light.height * 2

            local timeMult = 10
            local s = (math.sin(conductor.time * timeMult) + 1) / 2
            local c = (math.cos(conductor.time * timeMult) + 1) / 2

            s = ((s / 2) + 0.5) * 255
            c = ((c / 2) + 0.5) * 255

            graphics:setDrawColor(s, s, s, 255)
            graphics:drawTexture(-1500 + (i * eachPieceW2 - movement) + 70, 527, w2, h2)
            graphics:setDrawColor(c, c, c, 255)
            graphics:drawTexture(-1500 + (i * eachPieceW2 - movement) + 70 + 142, 527, w2, h2)
        end
    end
end

function scene.render()
    local scale = 1
    renderBackground(width)

    graphics:pushMatrix()
    graphics:scale(scale, scale, 1)
    renderWell()
    graphics:popMatrix()

    renderVerticalGradients(width)
    renderHorizontalGradients(width)

    renderRoad(width)
end