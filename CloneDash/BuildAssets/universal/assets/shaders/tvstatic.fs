#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform float uTime;

void main()
{
    float n = fract(sin(dot(fragTexCoord, vec2(12.9898, 78.233)) + uTime) * 43758.5453);
    finalColor = vec4(vec3(n), 1.0);
}