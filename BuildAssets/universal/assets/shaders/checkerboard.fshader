#version 330

in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;

uniform vec3 lightColor; // Color of light squares
uniform vec3 darkColor;  // Color of dark squares
uniform float scale;     // Scale of checkerboard pattern

void main()
{
    // Scale UV coordinates to control the size of the checkerboard
    vec2 scaledCoords = fragTexCoord * scale;

    // Compute the checkerboard pattern
    int checkX = int(floor(scaledCoords.x));
    int checkY = int(floor(scaledCoords.y));
    bool isLightSquare = (checkX + checkY) % 2 == 0;

    // Select the appropriate color
    vec3 color = isLightSquare ? lightColor : darkColor;

    // Output the final color
    finalColor = vec4(color, 1.0) * fragColor;
}