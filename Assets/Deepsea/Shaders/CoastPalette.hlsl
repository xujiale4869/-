#ifndef DEEPSEA_COAST_PALETTE
#define DEEPSEA_COAST_PALETTE
float _CoastArtLook;
float CoastLandMask(float3 world,float role){return step(.5,_CoastArtLook)*step(.5,role)*smoothstep(.05,.5,world.y);}
half3 CoastPalette(half3 a,float role,float3 w,float3 n,float grassWeight){
 float mask=CoastLandMask(w,role);if(mask<.0001)return a;bool second=_CoastArtLook>1.5;
 float lum=dot(a,half3(.299,.587,.114));float detail=.72+.52*saturate(lum*2.4);
 half3 grass=second?half3(.018,.21,.085):half3(.045,.22,.025),sand=second?half3(.69,.46,.31):half3(.64,.48,.25);
 half3 target=a;float blend=.82;
 if(role<1.5){target=second?half3(.48,.31,.29):half3(.46,.37,.23);float patch=.5+.26*sin(w.x*.54+sin(w.z*.38)*2)+.20*sin(w.z*.83+w.y*.31);float moss=smoothstep(.55,.91,n.y)*smoothstep(.41,.77,patch)*smoothstep(.30,1.2,w.y);target=lerp(target,grass,moss*.72);}
 else if(role<2.5){float plant=smoothstep(.45,.95,grassWeight)*smoothstep(.4,1.55,w.y);float patch=.85+.15*sin(w.x*.45+sin(w.z*.29)*2);target=lerp(sand,grass,plant*patch);detail=.90+.12*saturate(lum*2.4);}
 else if(role<3.5){target=second?half3(.012,.23,.105):half3(.065,.26,.025);detail=.70+.67*saturate(lum*3.0);blend=.88;}
 else if(role<4.5)target=second?half3(.48,.18,.09):half3(.43,.19,.045);
 else if(role<5.5)target=second?half3(.13,.055,.05):half3(.17,.07,.02);
 else if(role<6.5){target=half3(.045,.065,.10);blend=.40;}
 else if(role<7.5){target=second?half3(.09,.045,.14):half3(.045,.075,.16);blend=.75;}
 else{target=second?half3(.73,.56,.48):half3(.75,.62,.39);blend=.55;detail=.88+.20*saturate(lum);}
 return lerp(a,target*detail,blend*mask);
}
half3 CoastPaletteLight(half3 c,float role,float3 w,float3 n,float3 lightDirection,float shadowAttenuation){
 float mask=CoastLandMask(w,role);if(mask<.0001)return c;bool second=_CoastArtLook>1.5;
 float shade=saturate((1-saturate(dot(n,lightDirection)*1.35))*.45+(1-shadowAttenuation)*.8);
 half3 warm=second?half3(1.18,.96,.93):half3(1.16,1.02,.88),cool=second?half3(.72,.52,1.60):half3(.65,.67,1.45);
 half3 lit=c*warm;half lum=dot(c,half3(.299,.587,.114));half3 coolShade=(lum*.84+.035)*cool;
 return lerp(c,lerp(lit,coolShade,shade*.72),mask);
}
#endif
