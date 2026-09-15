#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform sampler2D uScratchesTex;
uniform sampler2D uDustTex;
uniform float uTime;
uniform float uStrength;

void main()
{
    vec4 color = texture(texture0, fragTexCoord);

    // Scroll each texture; the fast Y scroll makes the features jump frame to frame.
    vec2 sUV = fragTexCoord + vec2(uTime * 0.05, uTime * 0.10);
    vec2 dUV = fragTexCoord + vec2(uTime * 0.30, uTime * 0.50);

    float s = texture(uScratchesTex, sUV).r;
    float d = texture(uDustTex, dUV).r;

    float factor = (1.0 - s * uStrength) * (1.0 - d * uStrength);
    finalColor = vec4(color.rgb * factor, color.a);
}