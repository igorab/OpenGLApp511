#version 430

in vec2 vTexCoord;
out vec4 color;

uniform sampler2D brickTex;

void main()
{
    color = texture(brickTex, vTexCoord);
}