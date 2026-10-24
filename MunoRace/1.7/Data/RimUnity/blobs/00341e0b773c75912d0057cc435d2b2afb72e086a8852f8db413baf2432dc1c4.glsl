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
float RU_CompatSmoothstep(float low, float high, float x) {
    if (low == high) return step(low, x);
    float t = clamp((x - low) / (high - low), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatSmoothstep(vec2 low, vec2 high, vec2 x) {
    return vec2(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y));
}
vec2 RU_CompatSmoothstep(float low, float high, vec2 x) { return RU_CompatSmoothstep(vec2(low), vec2(high), x); }
vec3 RU_CompatSmoothstep(vec3 low, vec3 high, vec3 x) {
    return vec3(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z));
}
vec3 RU_CompatSmoothstep(float low, float high, vec3 x) { return RU_CompatSmoothstep(vec3(low), vec3(high), x); }
vec4 RU_CompatSmoothstep(vec4 low, vec4 high, vec4 x) {
    return vec4(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z), RU_CompatSmoothstep(low.w, high.w, x.w));
}
vec4 RU_CompatSmoothstep(float low, float high, vec4 x) { return RU_CompatSmoothstep(vec4(low), vec4(high), x); }


#line 56 0
struct RU_MaterialData_0
{
    vec4 _CoreColor; // @default = vec4(1.65, 1.5, 1.05, 1) @color
    float _EffectAge; // @default = 0
    float _EmissionStrength; // @default = 1.15
    vec4 _HaloColor; // @default = vec4(0.36, 0.62, 0.33, 1) @color
};


#line 62
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _CoreColor; // @default = vec4(1.65, 1.5, 1.05, 1) @color
    float _EffectAge; // @default = 0
    float _EmissionStrength; // @default = 1.15
    vec4 _HaloColor; // @default = vec4(0.36, 0.62, 0.33, 1) @color
}RU_Material_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 9392
void clip_0(float x_1)
{

#line 9398
    if(x_1 < 0.0)
    {

#line 9398
        discard;

#line 9398
    }

    return;
}


#line 70 0
float Gaussian_0(float value_0, float width_0)
{

#line 70
    float scaled_0 = value_0 / width_0;

#line 70
    return (exp2((- scaled_0 * scaled_0)));
}


#line 70
vec2 Beam_0(vec2 uv_0, float age_0, vec4 timing_0)
{

#line 70
    float _S1 = uv_0.x;

#line 70
    float traveled_0 = _S1 * timing_0.w;

#line 70
    float life_0 = (age_0 - traveled_0 / 220.0) / timing_0.y;

#line 70
    float _S2 = timing_0.z;

#line 70
    float _S3 = uv_0.y;

#line 70
    float crossSection_0 = (_S3 - sin(traveled_0 * 1.70000004768371582 - age_0 * 17.0 + _S2) * 0.03500000014901161) / mix(1.0, 0.31999999284744263, RU_CompatSmoothstep(0.44999998807907104, 1.0, life_0));

#line 70
    float _S4 = max((fwidth((_S3))) * 0.69999998807907104, 0.06499999761581421);

#line 70
    return vec2(Gaussian_0(crossSection_0, 0.64999997615814209) * 0.08500000089406967 + Gaussian_0(crossSection_0, 0.27000001072883606) * 0.27000001072883606 + (Gaussian_0(crossSection_0 - sin(traveled_0 * 1.20000004768371582 - age_0 * 24.0 + _S2) * 0.31000000238418579, _S4 * 0.64999997615814209) * 0.11999999731779099 + Gaussian_0(crossSection_0 - cos(traveled_0 * 0.89999997615814209 - age_0 * 19.0 + _S2 * 1.70000004768371582) * 0.43000000715255737, _S4 * 0.55000001192092896) * 0.07000000029802322), Gaussian_0(crossSection_0, _S4) * (0.92000001668930054 + sin(traveled_0 * 3.0 - age_0 * 37.0 + _S2) * 0.07999999821186066)) * (RU_CompatSmoothstep(0.0, 0.04500000178813934, life_0) * (1.0 - RU_CompatSmoothstep(0.41999998688697815, 1.0, life_0))) * (1.0 - RU_CompatSmoothstep(0.9649999737739563, 1.0, _S1));
}


#line 70
vec2 Flash_0(vec2 uv_1, float age_1, vec4 timing_1)
{

#line 70
    float life_1 = age_1 / timing_1.y;

#line 70
    float radius_0 = length(uv_1);

#line 70
    float _S5 = uv_1.y;

#line 70
    float _S6 = uv_1.x;

#line 70
    return vec2(Gaussian_0(radius_0, 0.41999998688697815 + life_1 * 0.25) * 0.27000001072883606, Gaussian_0(radius_0, 0.10000000149011612 + life_1 * 0.09000000357627869) + Gaussian_0(_S5, 0.01600000075995922) * Gaussian_0(_S6, 0.74000000953674316) * 0.40000000596046448 + Gaussian_0(_S6, 0.02199999988079071) * Gaussian_0(_S5, 0.44999998807907104) * 0.25) * (RU_CompatSmoothstep(0.0, 0.03500000014901161, life_1) * (exp2((- life_1 * 7.0))) * (1.0 - RU_CompatSmoothstep(0.69999998807907104, 1.0, life_1)));
}


#line 70
vec2 Ring_0(vec2 uv_2, float age_2, vec4 timing_2)
{

#line 70
    float life_2 = saturate_0(age_2 / timing_2.y);

#line 70
    float angle_0 = (atan((uv_2.y),(uv_2.x)));

#line 70
    float rim_0 = Gaussian_0(length(uv_2) - (0.07999999821186066 + 0.77999997138977051 * sqrt(life_2)), 0.01799999922513962 + life_2 * 0.01600000075995922) * RU_CompatSmoothstep(-0.25, 0.64999997615814209, sin(angle_0 * 5.0 + timing_2.z) + sin(angle_0 * 11.0 - life_2 * 7.0) * 0.30000001192092896);

#line 70
    float _S7 = 1.0 - life_2;

#line 70
    return vec2(rim_0, rim_0 * 0.11999999731779099) * (RU_CompatSmoothstep(0.0, 0.07999999821186066, life_2) * _S7 * _S7);
}


#line 70
vec2 Spark_0(vec2 uv_3, float age_3, vec4 timing_3)
{

#line 70
    float life_3 = saturate_0(age_3 / timing_3.y);

#line 70
    float offset_0 = 1.0 - (exp2((- life_3 * 4.19999980926513672))) - uv_3.x;

#line 70
    float trail_0 = RU_CompatSmoothstep(-0.01499999966472387, 0.01499999966472387, offset_0) * (exp2((- max(offset_0, 0.0) * 25.0)));

#line 70
    return vec2(trail_0 * 0.30000001192092896, trail_0 * 0.40000000596046448 + Gaussian_0(offset_0, 0.01799999922513962) * 0.69999998807907104) * Gaussian_0(uv_3.y, 0.30000001192092896) * (RU_CompatSmoothstep(0.0, 0.05000000074505806, life_3) * (1.0 - RU_CompatSmoothstep(0.34999999403953552, 1.0, life_3)));
}


#line 70
vec2 Wave_0(vec2 uv_4, float age_4, vec4 timing_4)
{

#line 70
    float front_0 = age_4 * 185.0 / timing_4.w;

#line 70
    float _S8 = uv_4.x;

#line 70
    float _S9 = uv_4.y;

#line 70
    return vec2(Gaussian_0(_S8 - front_0, 0.0130000002682209) * (0.55000001192092896 + sin(_S9 * 29.0 + _S8 * 11.0) * 0.25) * (1.0 - RU_CompatSmoothstep(0.55000001192092896, 1.0, abs(_S9))) * (RU_CompatSmoothstep(0.0, 0.02500000037252903, age_4) * (1.0 - RU_CompatSmoothstep(0.55000001192092896, 1.0, front_0))), 0.0);
}


#line 70
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 70
layout(location = 1) in vec4 input_uv_0; // @vertex TexCoord0 optional


#line 70
layout(location = 5) in vec4 input_timing_0; // @vertex TexCoord1 optional


#line 70
void RU_SourceMain()
{

#line 70
    float age_5 = RU_Material_0._EffectAge - input_timing_0.x;

#line 70
    clip_0(age_5);

#line 70
    float kind_0 = input_uv_0.z;

#line 70
    vec2 light_0;

#line 70
    if(kind_0 < 0.5)
    {

#line 70
        light_0 = Beam_0(input_uv_0.xy, age_5, input_timing_0);

#line 70
    }
    else
    {

#line 70
        if(kind_0 < 1.5)
        {

#line 70
            light_0 = Flash_0(input_uv_0.xy, age_5, input_timing_0);

#line 70
        }
        else
        {

#line 70
            if(kind_0 < 2.5)
            {

#line 70
                light_0 = Ring_0(input_uv_0.xy, age_5, input_timing_0);

#line 70
            }
            else
            {

#line 70
                if(kind_0 < 3.5)
                {

#line 70
                    light_0 = Spark_0(input_uv_0.xy, age_5, input_timing_0);

#line 70
                }
                else
                {

#line 70
                    light_0 = Wave_0(input_uv_0.xy, age_5, input_timing_0);

#line 70
                }

#line 70
            }

#line 70
        }

#line 70
    }

#line 70
    float _S10 = light_0.y;

#line 70
    float _S11 = light_0.x + _S10;

#line 70
    float intensity_0 = _S11 * input_uv_0.w;

#line 70
    clip_0(intensity_0 - 0.0020000000949949);

#line 70
    entryPointParam_frag_0 = vec4(mix(RU_Linear(RU_Material_0._HaloColor).xyz, RU_Linear(RU_Material_0._CoreColor).xyz, vec3(_S10 / max(_S11, 0.00009999999747379))) * RU_Material_0._EmissionStrength, saturate_0(intensity_0));

#line 70
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
