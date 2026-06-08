#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uStrength;

void main()
{
    vec2 uv = fragTexCoord;
    float blockSize = max(uStrength, 0.001);
    uv = floor(uv / blockSize) * blockSize + blockSize * 0.5;
    finalColor = texture(texture0, uv);
}