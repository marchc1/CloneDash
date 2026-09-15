#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;

uniform float uStrength;

void main()
{
    vec4 color = texture(texture0, fragTexCoord);

    float p = (fragTexCoord.x * (1.0 - fragTexCoord.x)) *
              (fragTexCoord.y * (1.0 - fragTexCoord.y));
    float vfactor = clamp(p * 16.0, 0.0, 1.0);

    color.rgb *= mix(1.0, vfactor, uStrength);
    finalColor = color;
}