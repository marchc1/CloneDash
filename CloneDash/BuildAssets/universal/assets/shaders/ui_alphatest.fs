#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;

void main()
{
    vec4 texColor = texture(texture0, fragTexCoord);
    vec4 color = texColor * fragColor;
    if (color.a < 0.01) discard;
    finalColor = color;
}