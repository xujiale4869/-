#ifndef HARBOR_V27_LIGHTS
#define HARBOR_V27_LIGHTS
float _HarborV27LightsOn;
float4 _HarborV27Boat, _HarborV27Pier, _HarborV27BoatColor, _HarborV27PierColor;
half3 HarborPointV27(float3 p, half3 n, float4 light, half3 color) {
 float3 d=light.xyz-p;float d2=dot(d,d);float fade=saturate(1.0-d2/max(0.01,light.w*light.w));
 return color * saturate(dot(n,normalize(d+float3(0,.001,0)))) * fade*fade / (1.0+d2*1.8);
}
half3 HarborDiffuseV27(float3 p, half3 n) {
 return _HarborV27LightsOn*(HarborPointV27(p,n,_HarborV27Boat,_HarborV27BoatColor.rgb)+HarborPointV27(p,n,_HarborV27Pier,_HarborV27PierColor.rgb));
}
half3 HarborWaterV27(float3 p,half3 n,half3 v) {
 float3 db=_HarborV27Boat.xyz-p, dp=_HarborV27Pier.xyz-p;
 half sb=pow(saturate(dot(n,normalize(normalize(db)+v))),48);
 half sp=pow(saturate(dot(n,normalize(normalize(dp)+v))),48);
 return _HarborV27LightsOn * (HarborPointV27(p,n,_HarborV27Boat,_HarborV27BoatColor.rgb)*(.08+sb*.55)+HarborPointV27(p,n,_HarborV27Pier,_HarborV27PierColor.rgb)*(.08+sp*.55));
}
#endif
