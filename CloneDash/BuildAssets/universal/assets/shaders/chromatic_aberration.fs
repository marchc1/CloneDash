#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uStrength;

void main()
{
    float offset = (fragTexCoord.x - 0.5) * uStrength * 0.01;

    float r = texture(texture0, fragTexCoord - vec2(offset, 0.0)).r;
    float g = texture(texture0, fragTexCoord).g;
    float b = texture(texture0, fragTexCoord + vec2(offset, 0.0)).b;

    finalColor = vec4(r, g, b, 1.0);
}