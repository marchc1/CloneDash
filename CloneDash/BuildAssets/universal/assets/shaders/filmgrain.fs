#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uTime;
uniform float uStrength;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void main()
{
    vec4 color = texture(texture0, fragTexCoord);

    vec2 gp = fragTexCoord * vec2(640.0, 360.0) + vec2(uTime * 53.0, uTime * 97.0);
    float grain = hash(floor(gp));
    float grainDarken = mix(1.0, grain, 0.35 * uStrength);

    float scratchSeed = floor(uTime * 12.0);
    float sx = hash(vec2(scratchSeed, 3.7));
    float scratch = smoothstep(0.0015, 0.0, abs(fragTexCoord.x - sx)) * 0.4 * uStrength;

    vec3 c = color.rgb * grainDarken + scratch + 0.02 * uStrength;

    vec3 contrasted = c + (c * c * c - c);
    c = mix(c, contrasted, uStrength);

    finalColor = vec4(c, color.a);
}