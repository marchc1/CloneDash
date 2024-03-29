#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

// Input uniform values
uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform vec3 inputHSV; // User-input HSV values

// Output fragment color
out vec4 finalColor;

// NOTE: Add here your custom variables

void main()
{
    // Texel color fetching from texture sampler
    vec4 texelColor = texture(texture0, fragTexCoord)*colDiffuse*fragColor;

    // Convert RGB to HSV
    float cmax = max(max(texelColor.r, texelColor.g), texelColor.b);
    float cmin = min(min(texelColor.r, texelColor.g), texelColor.b);
    float delta = cmax - cmin;

    float hue = 0.0;
    if (delta > 0.0) {
        if (cmax == texelColor.r) {
            hue = mod((texelColor.g - texelColor.b) / delta, 6.0);
        } else if (cmax == texelColor.g) {
            hue = ((texelColor.b - texelColor.r) / delta) + 2.0;
        } else {
            hue = ((texelColor.r - texelColor.g) / delta) + 4.0;
        }

        hue *= 60.0;
        if (hue < 0.0) {
            hue += 360.0;
        }
    }

    float saturation = (cmax > 0.0) ? (delta / cmax) : 0.0;
    float value = cmax;

    // Apply user-input HSV values
    hue += inputHSV.x;
    saturation *= inputHSV.y;
    value *= inputHSV.z;

    // Convert HSV back to RGB
    float chroma = value * saturation;
    float hue_ = hue / 60.0;
    float x = chroma * (1.0 - abs(mod(hue_, 2.0) - 1.0));

    vec3 rgb = vec3(0.0);
    if (hue_ >= 0.0 && hue_ <= 1.0) {
        rgb = vec3(chroma, x, 0.0);
    } else if (hue_ > 1.0 && hue_ <= 2.0) {
        rgb = vec3(x, chroma, 0.0);
    } else if (hue_ > 2.0 && hue_ <= 3.0) {
        rgb = vec3(0.0, chroma, x);
    } else if (hue_ > 3.0 && hue_ <= 4.0) {
        rgb = vec3(0.0, x, chroma);
    } else if (hue_ > 4.0 && hue_ <= 5.0) {
        rgb = vec3(x, 0.0, chroma);
    } else if (hue_ > 5.0 && hue_ <= 6.0) {
        rgb = vec3(chroma, 0.0, x);
    }

    float m = value - chroma;
    rgb += m;

    // Calculate final fragment color
    finalColor = vec4(rgb, texelColor.a);
}