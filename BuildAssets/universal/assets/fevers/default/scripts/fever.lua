local fever_bg_middle = textures:loadMuseDashTexture("fever_bg_middle")
local fever_bg_edge = textures:loadMuseDashTexture("fever_bg_edge")
fever_bg_edge.wrap = TEXTURE_WRAP_CLAMP

local function easeInCubic(x) return x * x * x end
local function easeOutCubic(x) return 1 - math.pow(1 - x, 3) end
local function clamp(x, mi, ma) return math.min(math.max(x, mi), ma) end

local fever_fx = textures:loadMuseDashTexture("FxFever")

local stars = {}
local beams = {}

local function drawStar(x, y, size, rotation)
    graphics:setTexture(fever_fx)
    graphics:drawTextureUVRotated(x, y, size, size, 0.01, 0.01, 0.5 - 0.01, 1 - 0.01, rotation)
end

local function drawBeam(x, y, w, h)
    graphics:setTexture(fever_fx)
    graphics:drawTextureUV(x, y, w, h, 0.5, 0, 1, 1)
end

local function addBeam(ttl)
    beams[#beams + 1] = {
        x = math.random(-1, 3) * 80,
        y = math.random(-1, 1) * 0.7,
        h = math.random(32, 64),

        r = math.random(190, 225),
        g = math.random(63, 70),
        b = math.random(170, 220),

        birth = conductor.time,
        ttl = ttl
    }
end

local function addStar(x, y, size, rot, r, g, b, vel, rotVel, resistance)
    stars[#stars + 1] = {
        x = x,
        y = y,
        size = size,
        rotation = rot,
        r = r,
        g = g,
        b = b,
        vel = vel,
        rotVel = rotVel,
        resistance = resistance
    }
end

local width                 = 2000

local function hsvToRgb(h, s, v, a)
    local r, g, b

    local i = math.floor(h * 6);
    local f = h * 6 - i;
    local p = v * (1 - s);
    local q = v * (1 - f * s);
    local t = v * (1 - (1 - f) * s);

    i = i % 6

    if i == 0 then
        r, g, b = v, t, p
    elseif i == 1 then
        r, g, b = q, v, p
    elseif i == 2 then
        r, g, b = p, v, t
    elseif i == 3 then
        r, g, b = p, q, v
    elseif i == 4 then
        r, g, b = t, p, v
    elseif i == 5 then
        r, g, b = v, p, q
    end

    return r * 255, g * 255, b * 255, a * 255
end

function fever.think()
    local deltaTime = level.curtimeDelta
    local inFever   = game.inFever

    if not inFever then return end
    -- Beams

    for i = #beams, 1, -1 do
        local beam  = beams[i]
        local ratio = (conductor.time - beam.birth) / beam.ttl
        if ratio >= 1 then
            table.remove(beams, i)
        end
    end

    local addBeamChance = math.random(1, 20)
    if addBeamChance > 19.4 then
        addBeam(math.random(0.5, 0.9))
    end

    -- Stars
    for i = #stars, 1, -1 do
        local star  = stars[i]
        if star.x < -width * 1.5 or math.abs(star.vel) <= 2 then
            table.remove(stars, i)
        else
            star.x = star.x + (star.vel * deltaTime)
            star.rotation = star.rotation + (star.rotVel * deltaTime)
            star.vel = star.vel * star.resistance
            star.rotVel = star.rotVel * star.resistance
        end
    end

    local addStarChance = math.random(1, 20)
    if addStarChance > 19.4 then
        -- 0 == close, 1 == far
        local dist = math.random(0, 1)
        local velocity = 2000 + ((1 - dist) * 2500)
        local darkness = 0.3 + ((1 - dist) * 0.65)
        local r, g, b, _ = hsvToRgb(math.random(290, 320) / 360, math.random(0.7, 0.9), darkness, 1)

        addStar(width, math.random(-HALF_HEIGHT, HALF_HEIGHT),
                math.random(90, 200), math.random(-60, 60),
                r, g, b,
                -velocity,
                math.random(200, 350), 1)
    end
end

function fever.start()
    stars = {}
    beams = {}

    for i = 1, 20 do
        addStar(
            math.random(1024 - 128, 1024 + 128), -HALF_HEIGHT + ((i / 20) * HEIGHT),
            math.random(256, 512), math.random(-40, 40),
            math.random(120, 170), 60, 255,
            -math.random(7000, 11000),
            math.random(200, 350), .992)
    end
end

function fever.render()
    local length = game.feverTimeMax
    local time   = length - game.feverTimeLeft

    local x = easeOutCubic(clamp(time * 1.5, 0, 1))
    local timeToAlphaOut = 0.5
    local fadeoutV = (clamp(time, length - timeToAlphaOut, length) - (length - timeToAlphaOut)) / timeToAlphaOut
    local fadeout = easeOutCubic(fadeoutV)
    local fadeout2 = easeInCubic(fadeoutV)

    local xpos = (x * -3500) + 1000

    local rX, rY, rW, rH = xpos, -HALF_HEIGHT, 8000, HEIGHT

    graphics:setDrawColor(45, 10, 35, (1 - fadeout2) * 190)
    graphics:drawRectangle(rX, rY, rW, rH)

    graphics:setDrawColor(255, 255, 255, (1 - fadeout) * 255)
    graphics:setTexture(fever_bg_edge)
    graphics:drawTexture(xpos - (fever_bg_edge.width * 2) + 1, -HALF_HEIGHT, fever_bg_edge.width * 2, HEIGHT)

    graphics:setTexture(fever_bg_middle)
    graphics:drawTexture(rX, rY, rW, rH)

    for _, beam in ipairs(beams) do
        local ratio = 1 - ((conductor.time - beam.birth) / beam.ttl)
        graphics:setDrawColor(beam.r, beam.g, beam.b, 64 * ratio * (1 - fadeout))
        drawBeam(-width + beam.x + -500 + (ratio * 2048), beam.y * HALF_HEIGHT * 1.5, width * 2, beam.h * ratio)
    end

    for _, star in ipairs(stars) do
        graphics:setDrawColor(star.r, star.g, star.b, 255 * (1 - fadeout))
        drawStar(star.x, star.y, star.size, star.rotation)
    end
end