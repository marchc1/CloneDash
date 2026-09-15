#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uOffset;

void main()
{
    float r = texture(texture0, vec2(fragTexCoord.x + uOffset, fragTexCoord.y)).r;
    float g = texture(texture0, fragTexCoord).g;
    float b = texture(texture0, vec2(fragTexCoord.x - uOffset, fragTexCoord.y)).b;
    finalColor = vec4(r, g, b, 1.0);
}