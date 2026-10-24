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
    vec4 _BlackColor; // @default = vec4(0.72, 0.8, 0.48, 0) @color
    float _CoreBlend; // @default = 0.28
    vec4 _DarkColor; // @default = vec4(0.18, 0.27, 0.13, 0.09) @color
    float _DissolveEdgeStrength; // @default = 0.32
    float _DissolveSoftness; // @default = 0.12
    float _EdgeSoftness; // @default = 12
    float _EffectAge; // @default = 0
    float _EmissionStrength; // @default = 1.1
    vec4 _InnerColor; // @default = vec4(1.15, 1.08, 0.72, 0.78) @color
    float _NoiseScale; // @default = 24
    float _NoiseSpeed; // @default = 1.2
    vec4 _OuterColor; // @default = vec4(0.4, 0.58, 0.36, 0.3) @color
    float _OutlineSoftness; // @default = 12
    float _OutlineStrength; // @default = 0.18
    float _OutlineWidth; // @default = 2
    vec4 _PointA; // @default = vec4(0.5, 0.85, 0, 0)
    vec4 _PointB; // @default = vec4(0.15, 0.15, 0, 0)
    vec4 _PointC; // @default = vec4(0.85, 0.15, 0, 0)
    float _Progress; // @default = 0
    float _PulseSpeed; // @default = 7
    float _PulseStrength; // @default = 0.07
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


#line 118
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 118
layout(location = 0) in vec4 input_vertex_0; // @vertex Position required


#line 118
layout(location = 1) in vec2 input_uv_0; // @vertex TexCoord0 optional


#line 118
struct v2f_0
{
    vec4 vertex_0;
    vec2 uv_0;
};


#line 118
void main()
{

#line 118
    v2f_0 output_0;

#line 118
    output_0.vertex_0 = UnityObjectToClipPos_0(input_vertex_0);

#line 118
    output_0.uv_0 = input_uv_0;

#line 118
    v2f_0 _S1 = output_0;

#line 118
    gl_Position = output_0.vertex_0;

#line 118
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 118
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
