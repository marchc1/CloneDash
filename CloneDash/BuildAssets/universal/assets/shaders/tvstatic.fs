#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uTime;
uniform vec2 uResolution;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453) * 2.0 - 1.0;
}

float valueNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

void main()
{
    vec2 p = fragTexCoord * uResolution * 0.25 + vec2(uTime * 137.0, uTime * 91.0);
    float vn = valueNoise(p);
    float n = fract(sin(vn * 91.2196) * 43758.5453);
    float g = n * 0.4 + 0.4;
    finalColor = vec4(vec3(g), 1.0);
}