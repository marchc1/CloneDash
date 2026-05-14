#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;

uniform vec2 uTexelSize;
uniform vec3 uTextColor;
uniform float uBorderSize;
uniform float uDarkenAmount;
uniform float uDesaturateAmount;
uniform float uSplitY;
uniform float uAlpha;

void main()
{
    vec4 texColor = texture(texture0, fragTexCoord);
    float centerAlpha = texColor.a;

    float borderAlpha = 0.0;
    const int SAMPLES = 16;
    for (int i = 0; i < SAMPLES; i++)
    {
        float angle = float(i) * (6.2831853 / float(SAMPLES));
        vec2 offset = vec2(cos(angle), sin(angle)) * uTexelSize * uBorderSize;
        borderAlpha = max(borderAlpha, texture(texture0, fragTexCoord + offset).a);
    }

    for (int i = 0; i < SAMPLES; i++)
    {
        float angle = float(i) * (6.2831853 / float(SAMPLES)) + 0.19635;
        vec2 offset = vec2(cos(angle), sin(angle)) * uTexelSize * uBorderSize * 0.5;
        borderAlpha = max(borderAlpha, texture(texture0, fragTexCoord + offset).a);
    }

    vec3 borderColor = uTextColor * (1.0 - uDarkenAmount);

    float v = fragTexCoord.y;
    float topFactor = smoothstep(uSplitY - 0.02, uSplitY + 0.02, v);

    vec3 topColor = uTextColor * (1.0 - uDarkenAmount * 0.4);
    float luma = dot(uTextColor, vec3(0.299, 0.587, 0.114));
    vec3 desaturated = mix(uTextColor, vec3(luma), uDesaturateAmount);
    vec3 bottomColor = desaturated * 1.25;

    vec3 bodyColor = mix(topColor, bottomColor, topFactor);

    // Border layer
    vec3 color = borderColor;
    float alpha = borderAlpha;

    color = mix(color, bodyColor, centerAlpha);
    alpha = max(alpha, centerAlpha);

    finalColor = vec4(color * alpha * uAlpha, alpha * uAlpha);
}
