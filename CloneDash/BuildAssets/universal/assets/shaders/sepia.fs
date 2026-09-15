#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uStrength;

void main()
{
    vec4 color = texture(texture0, fragTexCoord);
    float gray = dot(color.rgb, vec3(0.30, 0.59, 0.11));
    vec3 result = mix(color.rgb, vec3(gray), uStrength);
    finalColor = vec4(result, color.a);
}
