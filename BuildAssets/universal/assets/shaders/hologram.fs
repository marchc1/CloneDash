// Noise from: https://gist.github.com/patriciogonzalezvivo/670c22f3966e662d2f83

#version 330

in vec4 color;
uniform float time;
out vec4 FragColor;

vec3 permute(vec3 x) { return mod(((x*34.0)+1.0)*x, 289.0); }

float simplexNoise(vec2 v){
    const vec4 C = vec4(0.211324865405187, 0.366025403784439,
            -0.577350269189626, 0.024390243902439);
    vec2 i  = floor(v + dot(v, C.yy) );
    vec2 x0 = v -   i + dot(i, C.xx);
    vec2 i1;
    i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
    vec4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;
    i = mod(i, 289.0);
    vec3 p = permute( permute( i.y + vec3(0.0, i1.y, 1.0 ))
    + i.x + vec3(0.0, i1.x, 1.0 ));
    vec3 m = max(0.5 - vec3(dot(x0,x0), dot(x12.xy,x12.xy),
        dot(x12.zw,x12.zw)), 0.0);
    m = m*m ;
    m = m*m ;
    vec3 x = 2.0 * fract(p * C.www) - 1.0;
    vec3 h = abs(x) - 0.5;
    vec3 ox = floor(x + 0.5);
    vec3 a0 = x - ox;
    m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );
    vec3 g;
    g.x  = a0.x  * x0.x  + h.x  * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0 * dot(m, g);
}

void main() {

    float speed = 1.5;
    float factor = 1 - (time * speed);
    int noiseScale = 80;

    vec4 color = vec4(0.2, 0.6, 0.4, 1);
    vec4 dissolveColor = vec4(0.3, 0.3, 1, 1);

    float noise = simplexNoise(gl_FragCoord.xy / noiseScale);

    float sn1 = step(noise, factor);
    float sn2 = step(noise, factor + 0.1) - sn1;

    dissolveColor *= sn2 * color.a;
    color.a *= sn1;

    FragColor = dissolveColor + color;
}
