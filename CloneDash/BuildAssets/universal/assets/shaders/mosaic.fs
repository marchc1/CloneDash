#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;

uniform float uStrength;
uniform vec2 uResolution;

void main()
{
    float block = max(uStrength * min(uResolution.x, uResolution.y), 1.0);
    vec2 pix = fragTexCoord * uResolution;
    pix = floor(pix / block) * block;
    vec2 uv = pix / uResolution;
    finalColor = texture(texture0, uv);
}