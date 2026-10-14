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


#line 56 0
struct RU_MaterialData_0
{
    vec4 _BlackColor; // @default = vec4(0, 0, 0, 1) @color
    float _CoreBlend; // @default = 0.2
    vec4 _DarkColor; // @default = vec4(0.015, 0, 0, 0.32) @color
    float _DissolveEdgeStrength; // @default = 1.2
    float _DissolveSoftness; // @default = 0.06
    float _EdgeSoftness; // @default = 18
    float _EmissionStrength; // @default = 1
    vec4 _InnerColor; // @default = vec4(1.4, 0.015, 0.005, 0.62) @color
    float _NoiseScale; // @default = 18
    float _NoiseSpeed; // @default = 0.15
    vec4 _OuterColor; // @default = vec4(0.005, 0.22, 0.025, 0.6) @color
    float _OutlineSoftness; // @default = 6
    float _OutlineStrength; // @default = 0.28
    float _OutlineWidth; // @default = 2
    vec4 _PointA; // @default = vec4(0.5, 0.85, 0, 0)
    vec4 _PointB; // @default = vec4(0.15, 0.15, 0, 0)
    vec4 _PointC; // @default = vec4(0.85, 0.15, 0, 0)
    float _Progress; // @default = 0
    float _PulseSpeed; // @default = 2.5
    float _PulseStrength; // @default = 0.04
    vec4 _Time;
};


#line 79
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _BlackColor; // @default = vec4(0, 0, 0, 1) @color
    float _CoreBlend; // @default = 0.2
    vec4 _DarkColor; // @default = vec4(0.015, 0, 0, 0.32) @color
    float _DissolveEdgeStrength; // @default = 1.2
    float _DissolveSoftness; // @default = 0.06
    float _EdgeSoftness; // @default = 18
    float _EmissionStrength; // @default = 1
    vec4 _InnerColor; // @default = vec4(1.4, 0.015, 0.005, 0.62) @color
    float _NoiseScale; // @default = 18
    float _NoiseSpeed; // @default = 0.15
    vec4 _OuterColor; // @default = vec4(0.005, 0.22, 0.025, 0.6) @color
    float _OutlineSoftness; // @default = 6
    float _OutlineStrength; // @default = 0.28
    float _OutlineWidth; // @default = 2
    vec4 _PointA; // @default = vec4(0.5, 0.85, 0, 0)
    vec4 _PointB; // @default = vec4(0.15, 0.15, 0, 0)
    vec4 _PointC; // @default = vec4(0.85, 0.15, 0, 0)
    float _Progress; // @default = 0
    float _PulseSpeed; // @default = 2.5
    float _PulseStrength; // @default = 0.04
    vec4 _Time;
}RU_Material_0;

#line 9392 1
void clip_0(float x_0)
{

#line 9398
    if(x_0 < 0.0)
    {

#line 9398
        discard;

#line 9398
    }

    return;
}


#line 14961
vec3 saturate_0(vec3 x_1)
{

#line 14969
    return clamp(x_1, vec3(0.0), vec3(1.0));
}


#line 14946
float saturate_1(float x_2)
{

#line 14954
    return clamp(x_2, 0.0, 1.0);
}


#line 103 0
float Cross2D_0(vec2 left_0, vec2 right_0)
{

#line 103
    return left_0.x * right_0.y - left_0.y * right_0.x;
}


#line 103
vec3 GetBarycentric_0(vec2 sampleUv_0, vec2 pointA_0, vec2 pointB_0, vec2 pointC_0)
{

#line 103
    float denominator_0 = Cross2D_0(pointB_0 - pointA_0, pointC_0 - pointA_0);

#line 103
    float safeDenominator_0;

#line 103
    if((abs(denominator_0)) < 0.00000999999974738)
    {

#line 103
        if(denominator_0 < 0.0)
        {

#line 103
            safeDenominator_0 = -0.00000999999974738;

#line 103
        }
        else
        {

#line 103
            safeDenominator_0 = 0.00000999999974738;

#line 103
        }

#line 103
    }
    else
    {

#line 103
        safeDenominator_0 = denominator_0;

#line 103
    }

#line 103
    vec2 _S1 = pointC_0 - sampleUv_0;

#line 103
    float weightA_0 = Cross2D_0(pointB_0 - sampleUv_0, _S1) / safeDenominator_0;

#line 103
    float weightB_0 = Cross2D_0(_S1, pointA_0 - sampleUv_0) / safeDenominator_0;

#line 103
    return vec3(weightA_0, weightB_0, 1.0 - weightA_0 - weightB_0);
}


#line 103
float GetEdgePixelDistance_0(vec3 barycentric_0)
{

#line 103
    float _S2 = barycentric_0.x;

#line 103
    float _S3 = barycentric_0.y;

#line 103
    float _S4 = barycentric_0.z;

#line 103
    return min(_S2 / max(length(vec2(dFdx(_S2), dFdy(_S2))), 0.00000999999974738), min(_S3 / max(length(vec2(dFdx(_S3), dFdy(_S3))), 0.00000999999974738), _S4 / max(length(vec2(dFdx(_S4), dFdy(_S4))), 0.00000999999974738)));
}


#line 103
float Hash21_0(vec2 value_0)
{

#line 103
    return fract(sin(dot(value_0, vec2(127.09999847412109375, 311.70001220703125))) * 43758.546875);
}


#line 103
float ValueNoise_0(vec2 uv_0)
{

#line 103
    vec2 cell_0 = floor(uv_0);

#line 103
    vec2 local_0 = fract(uv_0);

#line 103
    vec2 blend_0 = local_0 * local_0 * (3.0 - 2.0 * local_0);

#line 103
    float _S5 = blend_0.x;

#line 103
    return mix(mix(Hash21_0(cell_0), Hash21_0(cell_0 + vec2(1.0, 0.0)), _S5), mix(Hash21_0(cell_0 + vec2(0.0, 1.0)), Hash21_0(cell_0 + vec2(1.0, 1.0)), _S5), blend_0.y);
}


#line 103
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 103
layout(location = 1) in vec2 input_uv_0; // @vertex TexCoord0 optional


#line 103
void RU_SourceMain()
{

#line 103
    vec3 barycentric_1 = GetBarycentric_0(input_uv_0, RU_Material_0._PointA.xy, RU_Material_0._PointB.xy, RU_Material_0._PointC.xy);

#line 103
    float edgePixelDistance_0 = GetEdgePixelDistance_0(barycentric_1);

#line 103
    float _S6 = max(RU_Material_0._EdgeSoftness, 1.0);

#line 103
    float triangleMask_0 = smoothstep(0.0, _S6, edgePixelDistance_0);

#line 103
    float appearPhase_0 = smoothstep(0.0, 1.0, saturate_1(RU_Material_0._Progress / 0.25));

#line 103
    float radiationPhase_0 = smoothstep(0.0, 1.0, saturate_1((RU_Material_0._Progress - 0.25) / 0.40000000596046448));

#line 103
    float dissolvePhase_0 = smoothstep(0.0, 1.0, saturate_1((RU_Material_0._Progress - 0.64999997615814209) / 0.34999999403953552));

#line 103
    float centerFactor_0 = saturate_1(min(barycentric_1.x, min(barycentric_1.y, barycentric_1.z)) * 3.0);

#line 103
    vec4 _S7 = mix(RU_Linear(RU_Material_0._DarkColor), RU_Linear(RU_Material_0._InnerColor), vec4(smoothstep(RU_Material_0._CoreBlend, 1.0, centerFactor_0)));

#line 103
    vec4 radiationColor_0 = _S7;

#line 103
    radiationColor_0.xyz = _S7.xyz * (RU_Material_0._EmissionStrength * (1.0 + sin(RU_Material_0._Time.y * RU_Material_0._PulseSpeed + centerFactor_0 * 12.0) * RU_Material_0._PulseStrength));

#line 103
    float outlineBlend_0 = (1.0 - smoothstep(0.0, max(RU_Material_0._OutlineWidth * 0.5 + max(RU_Material_0._OutlineSoftness, 0.25), 0.5), abs(edgePixelDistance_0 - (_S6 + RU_Material_0._OutlineWidth * 0.5)))) * RU_Material_0._OutlineStrength;

#line 103
    vec4 outlineColor_0 = RU_Linear(RU_Material_0._OuterColor);

#line 103
    outlineColor_0.xyz = saturate_0(RU_Linear(RU_Material_0._OuterColor).xyz);

#line 103
    vec4 _S8 = mix(radiationColor_0, outlineColor_0, vec4(outlineBlend_0));

#line 103
    radiationColor_0 = _S8;

#line 103
    vec4 _S9 = mix(RU_Linear(RU_Material_0._BlackColor), _S8, vec4(radiationPhase_0));

#line 103
    vec4 stageColor_0 = _S9;

#line 103
    float dissolveNoise_0 = ValueNoise_0(input_uv_0 * RU_Material_0._NoiseScale + RU_Material_0._Time.y * RU_Material_0._NoiseSpeed * vec2(0.73000001907348633, 0.40999999642372131));

#line 103
    float dissolveThreshold_0 = mix(- RU_Material_0._DissolveSoftness, 1.0 + RU_Material_0._DissolveSoftness, dissolvePhase_0);

#line 103
    float dissolveMask_0 = smoothstep(dissolveThreshold_0 - RU_Material_0._DissolveSoftness, dissolveThreshold_0 + RU_Material_0._DissolveSoftness, dissolveNoise_0);

#line 103
    stageColor_0.xyz = _S9.xyz + RU_Linear(RU_Material_0._OuterColor).xyz * ((1.0 - smoothstep(0.0, RU_Material_0._DissolveSoftness * 2.0, abs(dissolveNoise_0 - dissolveThreshold_0))) * (step(0.00100000004749745, dissolvePhase_0) * (1.0 - step(0.99900001287460327, dissolvePhase_0)))) * RU_Material_0._DissolveEdgeStrength;

#line 103
    stageColor_0[3] = stageColor_0[3] * (triangleMask_0 * appearPhase_0 * dissolveMask_0);

#line 103
    clip_0(stageColor_0.w - 0.00100000004749745);

#line 103
    entryPointParam_frag_0 = stageColor_0;

#line 103
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
