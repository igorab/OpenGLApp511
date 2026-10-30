#version 430

layout (location = 0) in vec3 position;
layout (location = 1) in vec2 texCoord;

out vec2 vTexCoord;

uniform mat4 mv_matrix;
uniform mat4 proj_matrix;

void main()
{
    vTexCoord  = texCoord;
    gl_Position = proj_matrix * mv_matrix * vec4(position, 1.0);
}