Shader "AntsJam/Worker Colors"
{
    Properties
    {
        _Shine ("Gloss highlight strength", Range(0,1)) = 0.35
        _Color ("Candy color", Color) = (0.82,0.58,0.26,1)
        _ShadowColor ("Groove tint", Color) = (0.34,0.19,0.055,1)
        _HighlightColor ("Fiber highlight", Color) = (1,0.88,0.57,1)
        _LightDirection ("Art light direction", Vector) = (-0.5,0.85,-0.3,0)
        _Opacity ("Inside Pipe Opacity", Range(0,1)) = 1.0
        _PipeTint ("Inside Pipe Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry+50" }
        Pass
        {
            Cull Back
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 world : TEXCOORD1; float2 uv : TEXCOORD2; float4 color : COLOR; };
            fixed4 _Color, _ShadowColor, _HighlightColor;
            float4 _LightDirection; float _Shine;
            float _Opacity;
            fixed4 _PipeTint;
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.uv=v.uv;o.color=v.color;return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 n=normalize(i.normal),l=normalize(_LightDirection.xyz),v=normalize(_WorldSpaceCameraPos.xyz-i.world);
                float diffuse=saturate(dot(n,l)*0.65+0.40);
                float3 baseColor=lerp(_ShadowColor.rgb,_Color.rgb,smoothstep(0,1,diffuse));
                float shine=pow(saturate(dot(n,normalize(l+v))),42)*_Shine;
                // Fine longitudinal fibers stay subtle at small screen sizes.
                float fibers=1;
                float3 rgb=baseColor*i.color.rgb*fibers+_HighlightColor.rgb*shine;
                rgb *= _PipeTint.rgb;
                float alpha = (_Opacity > 0.0001) ? _Opacity : 1.0;
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}





