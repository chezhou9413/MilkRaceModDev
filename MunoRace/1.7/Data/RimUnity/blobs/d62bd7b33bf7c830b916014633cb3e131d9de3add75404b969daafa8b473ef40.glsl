#version 450
vec3 RU_Decode(vec3 c) {
    return mix(pow(max((c + 0.055) / 1.055, vec3(0.0)), vec3(2.4)), c / 12.92, lessThanEqual(c, vec3(0.04045)));
}
vec3 RU_Encode(vec3 c) {
    return mix(1.055 * pow(max(c, vec3(0.0)), vec3(1.0 / 2.4)) - 0.055, c * 12.92, lessThanEqual(c, vec3(0.0031308)));
}
vec4 RU_Linear(vec4 c) { return vec4(RU_Decode(c.rgb), c.a); }
vec3 RU_Linear(vec3 c) { return RU_Decode(c); }
vec2 RU_Linear(vec2 c) { return RU_Decode(vec3(c, 0.0)).xy; }
float RU_Linear(float c) { return RU_Decode(vec3(c)).x; }
vec4 RU_SampleLinear(vec4 c, float srgb) { return srgb > 0.5 ? RU_Linear(c) : c; }
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _CoreColor; // @default = vec4(1.65, 1.5, 1.05, 1) @color
    float _EffectAge; // @default = 0
    float _EmissionStrength; // @default = 1.15
    vec4 _HaloColor; // @default = vec4(0.36, 0.62, 0.33, 1) @color
}RU_Material_0;


#line 15 0
struct RU_CameraData_0
{
    mat4 ViewProj_0;
};


#line 16
layout(binding = 0, std140) uniform block_RU_CameraData_0
{
    mat4 ViewProj_0;
}RU_Camera_0;

#line 17
struct RU_ObjectData_0
{
    mat4 model_0;
};


#line 18
layout(push_constant, std430) uniform block_RU_ObjectData_0
{
    mat4 model_0;
}RU_Object_0;
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}


#line 23
layout(location = 1)
out vec4 entryPointParam_vert_uv_0;


#line 23
layout(location = 5)
out vec4 entryPointParam_vert_timing_0;


#line 23
layout(location = 0) in vec4 input_vertex_0; // @vertex Position required


#line 23
layout(location = 1) in vec4 input_uv_0; // @vertex TexCoord0 optional


#line 23
layout(location = 5) in vec4 input_timing_0; // @vertex TexCoord1 optional


#line 70
struct v2f_0
{
    vec4 vertex_0;
    vec4 uv_0;
    vec4 timing_0;
};


#line 70
void main()
{

#line 70
    v2f_0 output_0;

#line 70
    output_0.vertex_0 = UnityObjectToClipPos_0(input_vertex_0);

#line 70
    output_0.uv_0 = input_uv_0;

#line 70
    output_0.timing_0 = input_timing_0;

#line 70
    v2f_0 _S1 = output_0;

#line 70
    gl_Position = output_0.vertex_0;

#line 70
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 70
    entryPointParam_vert_timing_0 = _S1.timing_0;

#line 70
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
