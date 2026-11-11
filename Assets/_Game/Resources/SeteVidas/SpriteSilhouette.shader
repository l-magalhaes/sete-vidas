// Silhueta do gato na sombra (melhoria 1). Escurece o desenho até _ShadowColor conforme _Amount (0 = normal,
// 1 = silhueta), mantém acesos os pixels da cor dos olhos (_EyeColor, quando _EyesOn = 1) e desenha um contorno
// de 1 px (_OutlineColor) em volta, nos pixels transparentes encostados no desenho.
// Fica em Resources para entrar no build e ser carregado por Resources.Load (ShadowSilhouette).
Shader "SeteVidas/SpriteSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Amount ("Silhueta", Range(0, 1)) = 0
        _ShadowColor ("Cor da silhueta", Color) = (0.078, 0.063, 0.094, 1)
        _OutlineColor ("Contorno", Color) = (0.227, 0.204, 0.314, 1)
        _EyeColor ("Olhos", Color) = (0.949, 0.647, 0.227, 1)
        _EyesOn ("Olhos acesos", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Amount;
            fixed4 _ShadowColor, _OutlineColor, _EyeColor;
            float _EyesOn;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                if (c.a < 0.01)
                {
                    // contorno: pixel vazio encostado (4 vizinhos) num pixel do desenho
                    float2 t = _MainTex_TexelSize.xy;
                    float n = tex2D(_MainTex, i.uv + float2(t.x, 0)).a + tex2D(_MainTex, i.uv - float2(t.x, 0)).a
                            + tex2D(_MainTex, i.uv + float2(0, t.y)).a + tex2D(_MainTex, i.uv - float2(0, t.y)).a;
                    float a = (n > 0.01 ? 1.0 : 0.0) * _Amount * _OutlineColor.a * i.color.a;
                    clip(a - 0.01);
                    return fixed4(_OutlineColor.rgb, a);
                }
                float3 d = c.rgb - _EyeColor.rgb;
                bool eye = _EyesOn > 0.5 && dot(d, d) < 0.004;
                float3 rgb = eye ? c.rgb : lerp(c.rgb, _ShadowColor.rgb, _Amount);
                return fixed4(rgb, c.a);
            }
            ENDCG
        }
    }
}
