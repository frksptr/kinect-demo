#version 330 core

in vec3 in_Position;
in vec4 in_Color;  
out vec4 pass_Color;
uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform vec3 uColor;
uniform float uSize;

void main(void) {

	gl_Position = projectionMatrix * viewMatrix * vec4(in_Position, 1.0);
	pass_Color = in_Color;
	gl_PointSize = uSize;
}