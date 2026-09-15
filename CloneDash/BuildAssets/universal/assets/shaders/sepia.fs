#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uStrength;

const vec3 SEPIA_TINT = vec3(0.90, 0.80, 0.62);

void main()
{
    vec4 color = texture(texture0, fragTexCoord);

    float gray = dot(color.rgb, vec3(0.30, 0.59, 0.11));
    vec3 desat = mix(color.rgb, vec3(gray), uStrength);
    vec3 sepia = desat * SEPIA_TINT / 0.77;
    vec3 result = mix(desat, sepia, uStrength);

    finalColor = vec4(result, color.a);
}