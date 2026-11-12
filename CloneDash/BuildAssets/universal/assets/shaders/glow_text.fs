#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;

uniform vec2 uTexelSize;
uniform vec2 uGradientRange;
uniform vec3 uTopColor;
uniform vec3 uBottomColor;
uniform vec3 uGlowColor;
uniform float uGlowOffset;
uniform float uGlowInner;
uniform float uGlowOuter;
uniform float uGlowPower;
uniform float uAlpha;

const int ANGLES = 16;
const int MAX_RADIUS = 24;

float signedDistance(float centerAlpha)
{
    if (centerAlpha > 0.0 && centerAlpha < 1.0)
        return 0.5 - centerAlpha;

    bool inside = centerAlpha >= 0.5;
    float reach = inside ? uGlowOffset + uGlowInner : uGlowOffset + uGlowOuter;
    for (int r = 1; r <= MAX_RADIUS; r++)
    {
        if (float(r) > reach + 1.0)
            break;
        for (int i = 0; i < ANGLES; i++)
        {
            float angle = float(i) * (6.2831853 / float(ANGLES));
            vec2 offset = vec2(cos(angle), sin(angle)) * uTexelSize * float(r);
            float a = texture(texture0, fragTexCoord + offset).a;
            if ((a >= 0.5) != inside)
                return inside ? -(float(r) - 0.5) : float(r) - 0.5;
        }
    }
    return inside ? -float(MAX_RADIUS) : float(MAX_RADIUS);
}

void main()
{
    float centerAlpha = texture(texture0, fragTexCoord).a;

    float t = clamp((fragTexCoord.y - uGradientRange.x) / (uGradientRange.y - uGradientRange.x), 0.0, 1.0);
    vec3 face = mix(uTopColor, uBottomColor, t) * centerAlpha;

    float x = signedDistance(centerAlpha) - uGlowOffset;
    float width = x >= 0.0 ? uGlowOuter : uGlowInner;
    float g = clamp(abs(x) / width, 0.0, 1.0);
    float glow = clamp(2.0 * (1.0 - pow(g, uGlowPower)), 0.0, 1.0);

    vec3 color = face + uGlowColor * glow;
    finalColor = vec4(color * uAlpha, centerAlpha * uAlpha);
}
