#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float distortion;
uniform float alpha;
uniform float size;
uniform vec4 color;
uniform vec4 tint;
uniform vec3 addColor;
float hash(vec2 cell) {
    return fract(sin(cell.x + cell.y * 7.0) * size * 43.0);
}
void main() {
    vec4 texColor = texture(texture0, fragTexCoord) * fragColor;
    float cutoff = fract(distortion * 0.9999);
    vec2 position = fragTexCoord * 3.5;
    float noise = 0.0;
    float frequency = 2.0;
    for (int i = 0; i < 9; i++) {
        vec2 cell = floor(position * frequency);
        vec2 f = position * frequency - cell;
        vec2 u = f * f * (3.0 - 2.0 * f);
        float bottom = mix(hash(cell), hash(cell + vec2(1.0, 0.0)), u.x);
        float top = mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0, 1.0)), u.x);
        noise += mix(bottom, top, u.y) / frequency;
        frequency *= 2.0;
    }
    float visible = clamp((noise - cutoff * 0.833333) / (cutoff + 0.1 - cutoff * 0.833333), 0.0, 1.0);
    visible = visible * visible * (3.0 - 2.0 * visible);
    vec3 rgb = texColor.rgb * visible;
    rgb = mix(rgb, rgb * (1.0 - texColor.a * visible) + tint.rgb, distortion) + addColor;
    vec4 result = clamp(vec4(rgb, texColor.a * visible - alpha) * color, 0.0, 1.0);
    finalColor = vec4(result.rgb * result.a, result.a);
}