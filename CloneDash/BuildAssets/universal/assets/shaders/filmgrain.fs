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
    vec3 c = color.rgb;

    float scratchSeed = floor(uTime * 6.0);
    for (int i = 0; i < 3; i++) {
        float sx = hash(vec2(scratchSeed, float(i) * 7.1));
        sx = fract(sx + uTime * 0.05 * (0.5 + float(i)));          
        float line = smoothstep(0.0018, 0.0, abs(fragTexCoord.x - sx));
        c *= 1.0 - line * 0.5 * uStrength;                         
    }

    vec2 dp = fragTexCoord * vec2(220.0, 124.0) + vec2(uTime * 300.0, uTime * 120.0) * 0.01;
    float dust = hash(floor(dp));
    float speck = step(0.992, dust) * 0.6 * uStrength;            
    c *= 1.0 - speck;

    finalColor = vec4(c, color.a);
}