#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uTime;
uniform float uStrength;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233)) + uTime) * 43758.5453);
}

void main()
{
    vec4 color = texture(texture0, fragTexCoord);

    float grain = (hash(fragTexCoord) - 0.5) * uStrength;

    float speck = hash(fragTexCoord * 347.0 + uTime * 3.7);
    speck = step(0.997, speck) * 0.8 * uStrength;

    float dirt = hash(fragTexCoord * 521.0 + uTime * 2.3);
    dirt = step(0.998, dirt) * -0.6 * uStrength;

    float scratchSeed = floor(uTime * 12.0);
    float scratchX = hash(vec2(scratchSeed, 0.0));
    float scratch = step(0.999, 1.0 - abs(fragTexCoord.x - scratchX) * 800.0) * 0.3 * uStrength;

    float flicker = (hash(vec2(floor(uTime * 24.0), 0.0)) - 0.5) * 0.04 * uStrength;

    vec3 result = color.rgb + grain + speck + dirt + scratch + flicker;
    finalColor = vec4(result, color.a);
}