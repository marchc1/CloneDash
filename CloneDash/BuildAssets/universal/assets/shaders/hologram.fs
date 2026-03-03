#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float time;
void main() {
    vec4 texColor = texture(texture0, fragTexCoord);
    float alpha = clamp(1.0 - time, 0.0, 1.0);
    float brighten = 1.0 + alpha * 1.5;
    vec4 color = texColor * vec4(0.6, 0.8, 1.0, 1.0) * brighten;
    color.a = texColor.a * alpha;
    finalColor = color * fragColor;
    finalColor.rgb *= finalColor.a;
}