#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uStrength;

void main()
{
    vec2 dir = fragTexCoord - 0.5;
    vec2 offset = dir * uStrength * 0.01;

    float r = texture(texture0, fragTexCoord - offset).r;
    float g = texture(texture0, fragTexCoord).g;
    float b = texture(texture0, fragTexCoord + offset).b;

    finalColor = vec4(r, g, b, 1.0);
}