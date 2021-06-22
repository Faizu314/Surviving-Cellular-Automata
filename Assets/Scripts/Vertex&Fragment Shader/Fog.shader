Shader "Custom/Fog"
{
    Properties
    {
        _MainTex ("MainTexture", 2D) = "white" {}
        _NoiseTex ("NoiseTexture", 2D) = "white" {}
        _FogColor ("FogColor", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "HLSLSupport.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
               
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            sampler2D _CameraDepthTexture;

            float2 _PlayerUVPos;
            float2 _CameraOffset;
            float _CameraOrthographicSize;
            float _CameraAngleOfIncident;

            //Depth Debug
            float _DebugWallThreshold;

            //Noise Debug
            float _NoiseScale;
            float _FogIntensity;
            float _MinFog;
            float _FogSpeed;
            float _VisionRadius;
            float _Exp;
            fixed4 _FogColor;

            float DropOffFunction(float distanceFromEdge) {
                return (-distanceFromEdge / _VisionRadius) + 1;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float linearDepth = LinearEyeDepth(depth);

                float2 noiseCoor;
                noiseCoor.x = ((((i.uv.x + _CameraOffset.x + (_Time.x) * _FogSpeed) / _NoiseScale) % 1) + 1) % 1;
                noiseCoor.y = ((((i.uv.y + _CameraOffset.y + (_Time.x) * _FogSpeed) / _NoiseScale) % 1) + 1) % 1;

                float fogValue = _MinFog + tex2D(_NoiseTex, noiseCoor) * _FogIntensity;

                int2 myPixelPos = int2(i.uv.x * _ScreenParams.x, i.uv.y * _ScreenParams.y);
                int2 playerPixelPos = int2(_PlayerUVPos.x * _ScreenParams.x, _PlayerUVPos.y * _ScreenParams.y);

                float screenHeightUnits = (_CameraOrthographicSize * 2) / cos(_CameraAngleOfIncident * 0.0174533);
                float verticalPixelToUnits = screenHeightUnits / _ScreenParams.y;
                float horizontalPixelToUnits = _CameraOrthographicSize * 2 / _ScreenParams.y;

                float2 pixelToPlayerWorldUnits = playerPixelPos - myPixelPos;
                pixelToPlayerWorldUnits.x *= horizontalPixelToUnits;
                pixelToPlayerWorldUnits.y *= verticalPixelToUnits;

                float unitDistToPlayer = length(pixelToPlayerWorldUnits);
                float maxDist = length(float2((_ScreenParams.x / 2) * horizontalPixelToUnits, (_ScreenParams.y / 2)* verticalPixelToUnits));

                if (unitDistToPlayer < _VisionRadius + fogValue - _MinFog) {
                    fogValue = fogValue * DropOffFunction(_VisionRadius + fogValue - _MinFog - unitDistToPlayer);
                }

                fixed4 colToFog = _FogColor - col;
                col = col + (colToFog * fogValue);

                return col;
            }             
            ENDCG
        }
    }
}
