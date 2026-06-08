#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uStrength;
uniform float uSoftness;

void main()
{
    vec4 color = texture(texture0, fragTexCoord);
    float dist = distance(fragTexCoord, vec2(0.5));
    float vignette = smoothstep(uStrength, uStrength - uSoftness, dist);
    finalColor = vec4(color.rgb * vignette, color.a);
}