#ifndef SEA_LIFE_V22_WAVES
#define SEA_LIFE_V22_WAVES
float _WaterV22Time,_WaterV22Motion;
// Return height, dH/dX, dH/dZ. Kept in sync with SeaLifeWaterV22.Field.
float3 WaterV22Field(float2 p,float t){
 float a=p.x*.72+p.y*.36-t*1.12;
 float b=-p.x*.51+p.y*1.05-t*1.47+.8;
 float c=p.x*.29-p.y*.43-t*.76+2.1;
 return float3(.040*sin(a)+.024*sin(b)+.016*sin(c),
 .040*.72*cos(a)-.024*.51*cos(b)+.016*.29*cos(c),
 .040*.36*cos(a)+.024*1.05*cos(b)-.016*.43*cos(c))*_WaterV22Motion;
}
#endif
