#version 330 core
 
out vec4 out_Color;

uniform sampler2D myTextureSampler;
uniform vec3 uColor;
 
void main(){
    out_Color = vec4(uColor, 1.0);
}