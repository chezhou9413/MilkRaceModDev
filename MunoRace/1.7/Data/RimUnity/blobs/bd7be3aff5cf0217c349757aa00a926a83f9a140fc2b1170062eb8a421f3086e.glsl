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
float RU_CompatPow2(float x) {
    float result = float(1.0);
    x *= x;
    result *= x;
    return result;
}
float RU_CompatSmoothstep(float low, float high, float x) {
    if (low == high) return step(low, x);
    float t = clamp((x - low) / (high - low), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatPow2(vec2 x) {
    vec2 result = vec2(1.0);
    x *= x;
    result *= x;
    return result;
}
vec2 RU_CompatSmoothstep(vec2 low, vec2 high, vec2 x) {
    return vec2(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y));
}
vec2 RU_CompatSmoothstep(float low, float high, vec2 x) { return RU_CompatSmoothstep(vec2(low), vec2(high), x); }
vec3 RU_CompatPow2(vec3 x) {
    vec3 result = vec3(1.0);
    x *= x;
    result *= x;
    return result;
}
vec3 RU_CompatSmoothstep(vec3 low, vec3 high, vec3 x) {
    return vec3(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z));
}
vec3 RU_CompatSmoothstep(float low, float high, vec3 x) { return RU_CompatSmoothstep(vec3(low), vec3(high), x); }
vec4 RU_CompatPow2(vec4 x) {
    vec4 result = vec4(1.0);
    x *= x;
    result *= x;
    return result;
}
vec4 RU_CompatSmoothstep(vec4 low, vec4 high, vec4 x) {
    return vec4(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z), RU_CompatSmoothstep(low.w, high.w, x.w));
}
vec4 RU_CompatSmoothstep(float low, float high, vec4 x) { return RU_CompatSmoothstep(vec4(low), vec4(high), x); }


#line 56 0
struct RU_MaterialData_0
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
};


#line 79
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


#line 14946
float saturate_0(float x_1)
{

#line 14954
    return clamp(x_1, 0.0, 1.0);
}


#line 118 0
float Cross2D_0(vec2 left_0, vec2 right_0)
{

#line 118
    return left_0.x * right_0.y - left_0.y * right_0.x;
}


#line 118
vec3 GetBarycentric_0(vec2 sampleUv_0)
{

#line 118
    float denominator_0 = Cross2D_0(RU_Material_0._PointB.xy - RU_Material_0._PointA.xy, RU_Material_0._PointC.xy - RU_Material_0._PointA.xy);

#line 118
    float safeDenominator_0;

#line 118
    if((abs(denominator_0)) < 0.00000999999974738)
    {

#line 118
        if(denominator_0 < 0.0)
        {

#line 118
            safeDenominator_0 = -0.00000999999974738;

#line 118
        }
        else
        {

#line 118
            safeDenominator_0 = 0.00000999999974738;

#line 118
        }

#line 118
    }
    else
    {

#line 118
        safeDenominator_0 = denominator_0;

#line 118
    }

#line 118
    float weightA_0 = Cross2D_0(RU_Material_0._PointB.xy - sampleUv_0, RU_Material_0._PointC.xy - sampleUv_0) / safeDenominator_0;

#line 118
    float weightB_0 = Cross2D_0(RU_Material_0._PointC.xy - sampleUv_0, RU_Material_0._PointA.xy - sampleUv_0) / safeDenominator_0;

#line 118
    return vec3(weightA_0, weightB_0, 1.0 - weightA_0 - weightB_0);
}


#line 118
float Hash21_0(vec2 value_0)
{

#line 118
    return fract(sin(dot(value_0, vec2(127.09999847412109375, 311.70001220703125))) * 43758.546875);
}


#line 118
float ValueNoise_0(vec2 position_0)
{

#line 118
    vec2 cell_0 = floor(position_0);

#line 118
    vec2 local_0 = fract(position_0);

#line 118
    vec2 blend_0 = local_0 * local_0 * (3.0 - 2.0 * local_0);

#line 118
    float _S1 = blend_0.x;

#line 118
    return mix(mix(Hash21_0(cell_0), Hash21_0(cell_0 + vec2(1.0, 0.0)), _S1), mix(Hash21_0(cell_0 + vec2(0.0, 1.0)), Hash21_0(cell_0 + vec2(1.0, 1.0)), _S1), blend_0.y);
}


#line 118
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 118
layout(location = 1) in vec2 input_uv_0; // @vertex TexCoord0 optional


#line 118
void RU_SourceMain()
{

#line 118
    vec3 barycentric_0 = GetBarycentric_0(input_uv_0);

#line 118
    float distanceAlong_0 = 1.0 - barycentric_0.x;

#line 118
    float _S2 = barycentric_0.y;

#line 118
    float _S3 = barycentric_0.z;

#line 118
    float transverse_0 = (_S2 - _S3) / max(distanceAlong_0, 0.03500000014901161);

#line 118
    float _S4 = min(_S2, _S3);

#line 118
    float _S5 = abs(transverse_0);

#line 118
    float coneMask_0 = RU_CompatSmoothstep(0.0, 1.5, _S4 / max(length(vec2(dFdx(_S4), dFdy(_S4))), 0.00000999999974738)) * (1.0 - RU_CompatSmoothstep(0.68000000715255737, 1.0, _S5)) * RU_CompatSmoothstep(0.0, 0.02500000037252903, distanceAlong_0);

#line 118
    float arrival_0 = RU_Material_0._EffectAge / 0.18000000715255737;

#line 118
    float advance_0 = 1.0 - RU_CompatSmoothstep(arrival_0 - (0.02500000037252903 + RU_Material_0._OutlineSoftness * 0.00300000002607703), arrival_0, distanceAlong_0);

#line 118
    float endFeather_0 = 1.0 - RU_CompatSmoothstep(0.80000001192092896, 1.0, distanceAlong_0);

#line 118
    vec2 flowUv_0 = vec2(transverse_0 * RU_Material_0._NoiseScale, (distanceAlong_0 - RU_Material_0._EffectAge * RU_Material_0._NoiseSpeed) * 3.79999995231628418);

#line 118
    float broadNoise_0 = ValueNoise_0(flowUv_0 * 0.4699999988079071 + vec2(13.19999980926513672, 4.69999980926513672));

#line 118
    float fineNoise_0 = ValueNoise_0(flowUv_0 + vec2(broadNoise_0 * 1.39999997615814209, 0.0));

#line 118
    float appear_0 = RU_CompatSmoothstep(0.0, 0.03500000014901161, RU_Material_0._EffectAge);

#line 118
    float sustain_0 = 1.0 - RU_CompatSmoothstep(0.20000000298023224, 0.77999997138977051, RU_Material_0._Progress);

#line 118
    float fade_0 = 1.0 - RU_CompatSmoothstep(0.60000002384185791, 1.0, RU_Material_0._Progress);

#line 118
    float pulse_0 = 1.0 + sin(distanceAlong_0 * 24.0 - RU_Material_0._EffectAge * RU_Material_0._PulseSpeed) * RU_Material_0._PulseStrength;

#line 118
    float dissolveNoise_0 = broadNoise_0 * 0.64999997615814209 + fineNoise_0 * 0.34999999403953552;

#line 118
    float dissolveThreshold_0 = mix(-0.30000001192092896, 1.29999995231628418, RU_CompatSmoothstep(0.41999998688697815, 1.0, RU_Material_0._Progress)) - distanceAlong_0 * 0.15999999642372131;

#line 118
    float dissolve_0 = RU_CompatSmoothstep(dissolveThreshold_0 - RU_Material_0._DissolveSoftness, dissolveThreshold_0 + RU_Material_0._DissolveSoftness, dissolveNoise_0);

#line 118
    float haze_0 = (0.02500000037252903 + broadNoise_0 * 0.05499999970197678) * (1.0 - saturate_0(_S5));

#line 118
    float light_0 = saturate_0(((RU_CompatSmoothstep(0.47999998927116394, 0.8399999737739563, fineNoise_0) * (0.40000000596046448 + 0.60000002384185791 * broadNoise_0) * 0.62000000476837158 + exp(- RU_CompatPow2((transverse_0 + sin(distanceAlong_0 * 17.0 - RU_Material_0._EffectAge * 13.0) * 0.02500000037252903) / (0.10000000149011612 + RU_Material_0._CoreBlend * 0.25))) * (0.64999997615814209 + 0.34999999403953552 * broadNoise_0) * 0.72000002861022949) * mix(1.0, 0.34999999403953552, saturate_0(distanceAlong_0)) + exp(- RU_CompatPow2((distanceAlong_0 - arrival_0 + 0.03500000014901161) / (0.01799999922513962 + RU_Material_0._OutlineWidth * 0.00800000037997961))) * (1.0 - RU_CompatSmoothstep(0.15000000596046448, 0.30000001192092896, RU_Material_0._EffectAge)) * RU_Material_0._OutlineStrength) * (0.34999999403953552 + sustain_0 * 0.64999997615814209 + exp(- RU_Material_0._EffectAge * 12.0) * 0.30000001192092896));

#line 118
    vec4 _S6 = mix(mix(RU_Linear(RU_Material_0._DarkColor), RU_Linear(RU_Material_0._OuterColor), vec4(saturate_0(haze_0 * 5.0 + (1.0 - sustain_0) * RU_Material_0._DissolveEdgeStrength * dissolveNoise_0))), RU_Linear(RU_Material_0._InnerColor), vec4(RU_CompatSmoothstep(0.11999999731779099, 0.64999997615814209, light_0)));

#line 118
    vec4 color_0 = _S6;

#line 118
    color_0.xyz = _S6.xyz * (RU_Material_0._EmissionStrength * pulse_0);

#line 118
    color_0.xyz = mix(RU_Linear(RU_Material_0._BlackColor).xyz, color_0.xyz, vec3(appear_0));

#line 118
    color_0[3] = saturate_0((haze_0 * RU_Linear(RU_Material_0._OuterColor).w + light_0 * RU_Linear(RU_Material_0._InnerColor).w) * (coneMask_0 * advance_0 * endFeather_0 * appear_0 * fade_0 * dissolve_0) * RU_CompatSmoothstep(0.0, max(RU_Material_0._EdgeSoftness * 0.00150000001303852, 0.0020000000949949), _S4));

#line 118
    clip_0(color_0.w - 0.00100000004749745);

#line 118
    entryPointParam_frag_0 = color_0;

#line 118
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
