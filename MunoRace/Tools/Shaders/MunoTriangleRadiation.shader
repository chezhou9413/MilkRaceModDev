Shader "MunoRace/Effects/TriangleRadiation"
{
    Properties
    {
        _EffectAge ("播放秒数", Float) = 0
        [HDR] _CoreColor ("白热核心", Color) = (1.65, 1.5, 1.05, 1)
        _HaloColor ("电离余辉", Color) = (0.36, 0.62, 0.33, 1)
        _EmissionStrength ("发光强度", Range(0, 4)) = 1.15
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One, Zero One

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _EffectAge;
            float4 _CoreColor, _HaloColor;
            float _EmissionStrength;

            //采样坐标同时携带面片类型、强度与独立的延迟和寿命。
            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
                float4 timing : TEXCOORD1;
            };

            //网格固定在发射位置，时间驱动片元内部的传导和飞散。
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 timing : TEXCOORD1;
            };

            //投影束带与各层面片，完整保留四维纹理坐标。
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.timing = input.timing;
                return output;
            }

            //高斯截面让细核心和大范围柔光连续过渡。
            float Gaussian(float value, float width)
            {
                float scaled = value / width;
                return exp2(-scaled * scaled);
            }

            //轴心保持连续，外层细丝沿程错相振荡，收束波从炮口向末端传递。
            float2 Beam(float2 uv, float age, float4 timing)
            {
                float traveled = uv.x * timing.w;
                float life = (age - traveled / 220.0) / timing.y;
                float envelope = smoothstep(0.0, 0.045, life) * (1.0 - smoothstep(0.42, 1.0, life));
                float width = lerp(1.0, 0.32, smoothstep(0.45, 1.0, life));
                float bend = sin(traveled * 1.7 - age * 17.0 + timing.z) * 0.035;
                float crossSection = (uv.y - bend) / width;
                float pixelWidth = max(fwidth(uv.y) * 0.7, 0.065);
                float core = Gaussian(crossSection, pixelWidth);
                float sheath = Gaussian(crossSection, 0.27) * 0.27;
                float halo = Gaussian(crossSection, 0.65) * 0.085;
                float strandA = sin(traveled * 1.2 - age * 24.0 + timing.z) * 0.31;
                float strandB = cos(traveled * 0.9 - age * 19.0 + timing.z * 1.7) * 0.43;
                float strands = Gaussian(crossSection - strandA, pixelWidth * 0.65) * 0.12;
                strands += Gaussian(crossSection - strandB, pixelWidth * 0.55) * 0.07;
                float surge = 0.92 + sin(traveled * 3.0 - age * 37.0 + timing.z) * 0.08;
                float endFade = 1.0 - smoothstep(0.965, 1.0, uv.x);
                return float2(halo + sheath + strands, core * surge) * envelope * endFade;
            }

            //短促白热闪点带柔光与细长衍射尖峰，亮度随时间迅速衰减。
            float2 Flash(float2 uv, float age, float4 timing)
            {
                float life = age / timing.y;
                float envelope = smoothstep(0.0, 0.035, life) * exp2(-life * 7.0);
                envelope *= 1.0 - smoothstep(0.7, 1.0, life);
                float radius = length(uv);
                float core = Gaussian(radius, 0.10 + life * 0.09);
                float corona = Gaussian(radius, 0.42 + life * 0.25) * 0.27;
                float horizontal = Gaussian(uv.y, 0.016) * Gaussian(uv.x, 0.74);
                float vertical = Gaussian(uv.x, 0.022) * Gaussian(uv.y, 0.45);
                return float2(corona, core + horizontal * 0.4 + vertical * 0.25) * envelope;
            }

            //命中余波以断续圆弧扩散，避免规则整圆形成技能范围标记感。
            float2 Ring(float2 uv, float age, float4 timing)
            {
                float life = saturate(age / timing.y);
                float radius = length(uv);
                float angle = atan2(uv.y, uv.x);
                float expansion = 0.08 + 0.78 * sqrt(life);
                float breakup = smoothstep(-0.25, 0.65, sin(angle * 5.0 + timing.z) + sin(angle * 11.0 - life * 7.0) * 0.3);
                float rim = Gaussian(radius - expansion, 0.018 + life * 0.016) * breakup;
                float envelope = smoothstep(0.0, 0.08, life) * (1.0 - life) * (1.0 - life);
                return float2(rim, rim * 0.12) * envelope;
            }

            //亮点在固定轨迹内减速移动，后方保留很短的渐淡尾迹。
            float2 Spark(float2 uv, float age, float4 timing)
            {
                float life = saturate(age / timing.y);
                float head = 1.0 - exp2(-life * 4.2);
                float offset = head - uv.x;
                float trail = smoothstep(-0.015, 0.015, offset) * exp2(-max(offset, 0.0) * 25.0);
                float tip = Gaussian(offset, 0.018);
                float shape = Gaussian(uv.y, 0.3);
                float envelope = smoothstep(0.0, 0.05, life) * (1.0 - smoothstep(0.35, 1.0, life));
                return float2(trail * 0.3, trail * 0.4 + tip * 0.7) * shape * envelope;
            }

            //地面只显示掠过的弧形压力前沿，不给整个攻击区域填色。
            float2 Wave(float2 uv, float age, float4 timing)
            {
                float front = age * 185.0 / timing.w;
                float edge = Gaussian(uv.x - front, 0.013);
                float sides = 1.0 - smoothstep(0.55, 1.0, abs(uv.y));
                float breaks = 0.55 + sin(uv.y * 29.0 + uv.x * 11.0) * 0.25;
                float envelope = smoothstep(0.0, 0.025, age) * (1.0 - smoothstep(0.55, 1.0, front));
                return float2(edge * breaks * sides * envelope, 0.0);
            }

            //独立面片共享加色管线，暗部透明，避免用低饱和色块覆盖地形。
            float4 frag(v2f input) : SV_Target
            {
                float age = _EffectAge - input.timing.x;
                clip(age);
                float kind = input.uv.z;
                float2 light;
                if (kind < 0.5) light = Beam(input.uv.xy, age, input.timing);
                else if (kind < 1.5) light = Flash(input.uv.xy, age, input.timing);
                else if (kind < 2.5) light = Ring(input.uv.xy, age, input.timing);
                else if (kind < 3.5) light = Spark(input.uv.xy, age, input.timing);
                else light = Wave(input.uv.xy, age, input.timing);
                float intensity = (light.x + light.y) * input.uv.w;
                clip(intensity - 0.002);
                float coreBlend = light.y / max(light.x + light.y, 0.0001);
                float3 color = lerp(_HaloColor.rgb, _CoreColor.rgb, coreBlend) * _EmissionStrength;
                return float4(color, saturate(intensity));
            }
            ENDCG
        }
    }
    FallBack Off
}
