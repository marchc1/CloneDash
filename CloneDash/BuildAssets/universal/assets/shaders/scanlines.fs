#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uTime;
uniform float uStrength;

float hash(float n) {
    return fract(sin(n) * 43758.5453);
}

void main()
{
    float band = floor(fragTexCoord.y * 5.0);
    float tick = floor(uTime * 8.0);
    float activeBand = floor(hash(tick) * 5.0);

    vec2 uv = fragTexCoord;
    if (band == activeBand) {
        float bandPos = fract(fragTexCoord.y * 5.0);
        float envelope = sin(bandPos * 3.14159);
        float wave = sin(fragTexCoord.x * 12.0 + hash(tick + 3.0) * 50.0);
        float jitter = (hash(tick + 7.0) - 0.5) * 2.0 * uStrength;
        uv.x += (jitter + wave * uStrength * 0.3) * envelope;
    }

    finalColor = texture(texture0, uv) * fragColor;
}