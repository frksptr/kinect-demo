#version 330 core
 
in vec3 in_Position;
in vec2 vertexUV;

out vec2 UV;
 
uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;

void main(){ 
    gl_Position =  projectionMatrix * viewMatrix * vec4(in_Position,1);
    UV = vertexUV;
}
