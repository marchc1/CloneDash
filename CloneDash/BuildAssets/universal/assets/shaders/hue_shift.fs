#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform float hueShift;

void main()
{
    vec4 color = texture(texture0, fragTexCoord) * colDiffuse * fragColor;
    float angle = hueShift * 6.28318530718;
    vec3 axis = vec3(0.57735026919);
    float c = cos(angle);
    vec3 rgb = color.rgb * c + cross(axis, color.rgb) * sin(angle) + axis * dot(axis, color.rgb) * (1.0 - c);
    finalColor = vec4(clamp(rgb, 0.0, 1.0), color.a);
}