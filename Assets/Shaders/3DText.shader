Shader "GUI/3D Text Shader" { 
Properties { 
   _MainTex ("Font Texture", 2D) = "white" {} 
   _Color ("Text Color", Color) = (1,1,1,1) 
} 

SubShader { 
   Tags { "Queue"="Transparent+1" "IgnoreProjector"="True" "RenderType"="Transparent" } 
   Lighting Off ZWrite On Fog { Mode Off } 
   Blend SrcAlpha OneMinusSrcAlpha 
   Pass { 
      Color [_Color] 
      SetTexture [_MainTex] { 
         combine primary, texture * primary 
      } 
   } 
} 
}