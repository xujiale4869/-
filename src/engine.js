/* Lantern Ward v0.1 — deterministic simulation, no browser dependency. */
(function(root){
'use strict';
const TOWERS={
 spark:{name:'萤灯塔',cost:70,color:'#ffcf75',hint:'精准 · 单体',desc:'收拢萤火，发射追踪光弹。适合消灭迅捷的偷灯蛾。',damage:[24,43,72],rate:[.70,.61,.52],range:[148,165,180],upgrade:[90,145]},
 bloom:{name:'爆蕾塔',cost:110,color:'#f09b88',hint:'绽放 · 范围',desc:'把蓄光花蕾抛向夜潮，炸开一圈暖光。对成群墨团尤其有效。',damage:[38,65,105],rate:[1.6,1.45,1.25],range:[160,174,190],upgrade:[125,180]},
 frost:{name:'月露塔',cost:95,color:'#9cded4',hint:'月露 · 减速',desc:'以月露冻结夜潮。减速效果不叠加，但能持续刷新。',damage:[10,19,31],rate:[.90,.76,.62],range:[143,158,176],upgrade:[105,155]}
};
const TYPES={ink:{name:'墨团',hp:62,speed:57,reward:12,armor:0,life:1},moth:{name:'偷灯蛾',hp:45,speed:90,reward:12,armor:0,life:1},shell:{name:'壳背',hp:220,speed:39,reward:23,armor:.32,life:2},boss:{name:'沉夜',hp:2350,speed:27,reward:160,armor:.18,life:5}};
const POINTS=[[-35,282],[119,282],[190,323],[210,460],[310,517],[416,484],[480,352],[598,306],[690,359],[718,498],[822,554],[923,493],[926,341],[861,268],[855,174]];
function makePath(points){let out=[];for(let i=0;i<points.length-1;i++){let a=points[Math.max(0,i-1)],b=points[i],c=points[i+1],d=points[Math.min(points.length-1,i+2)];for(let s=0;s<24;s++){let t=s/24,t2=t*t,t3=t2*t;out.push({x:.5*((2*b[0])+(-a[0]+c[0])*t+(2*a[0]-5*b[0]+4*c[0]-d[0])*t2+(-a[0]+3*b[0]-3*c[0]+d[0])*t3),y:.5*((2*b[1])+(-a[1]+c[1])*t+(2*a[1]-5*b[1]+4*c[1]-d[1])*t2+(-a[1]+3*b[1]-3*c[1]+d[1])*t3)});}}out.push({x:points.at(-1)[0],y:points.at(-1)[1]});let len=0;out[0].dist=0;for(let i=1;i<out.length;i++){len+=Math.hypot(out[i].x-out[i-1].x,out[i].y-out[i-1].y);out[i].dist=len;}return out;}
const PATH=makePath(POINTS),LENGTH=PATH.at(-1).dist;
function at(d){d=Math.max(0,Math.min(d,LENGTH));let lo=0,hi=PATH.length-1;while(lo<hi){let m=(lo+hi)>>1;if(PATH[m].dist<d)lo=m+1;else hi=m;}let a=PATH[Math.max(0,lo-1)],b=PATH[lo],t=(d-a.dist)/(b.dist-a.dist||1);return{x:a.x+(b.x-a.x)*t,y:a.y+(b.y-a.y)*t};}
const PADS=[[130,385],[265,255],[314,410],[370,590],[425,363],[551,420],[590,214],[640,506],[768,295],[805,442],[862,623],[840,352]].map((p,i)=>({id:i,x:p[0],y:p[1]}));
const OBSTACLES=[{id:2,hp:105,reward:45,name:'枯灯木'},{id:5,hp:180,reward:60,name:'结夜晶'},{id:8,hp:230,reward:75,name:'枯灯木'},{id:11,hp:270,reward:90,name:'结夜晶'}];
const WAVES=[
 ['ink','ink','ink','ink','ink','ink'],
 ['ink','moth','ink','moth','ink','ink','moth','ink'],
 ['ink','ink','ink','ink','moth','moth','moth','ink','ink'],
 ['shell','ink','ink','moth','ink','shell','moth','ink'],
 ['moth','moth','ink','ink','moth','moth','shell','ink','ink','ink'],
 ['shell','ink','ink','shell','moth','moth','ink','ink','shell','ink'],
 ['ink','ink','ink','ink','ink','moth','moth','moth','shell','shell','ink','ink'],
 ['shell','shell','moth','moth','ink','ink','shell','moth','moth','ink','ink','ink'],
 ['moth','moth','moth','moth','shell','shell','shell','ink','ink','ink','moth','moth'],
 ['shell','shell','ink','ink','ink','moth','moth','shell','shell','ink','ink','ink','ink','ink'],
 ['shell','moth','moth','moth','shell','shell','ink','ink','ink','ink','moth','moth','shell','shell'],
 ['ink','ink','shell','shell','moth','moth','boss','ink','ink','moth','moth','shell','shell','ink','ink']
];
class Game{
 constructor(mode='normal'){this.mode=mode;this.coins=mode==='gentle'?290:250;this.life=20;this.wave=0;this.phase='ready';this.enemies=[];this.towers=[];this.projectiles=[];this.obstacles=OBSTACLES.map(o=>({...o,maxHp:o.hp,alive:true}));this.events=[];this.spawn=[];this.spawnTimer=0;this.time=0;this.spellCooldown=0;this.selectedObstacle=null;this.kills=0;this.nextId=1;}
 emit(type,data={}){this.events.push({type,...data});}
 obstacle(id){return this.obstacles.find(o=>o.id===id&&o.alive);}
 build(id,type){if(!TOWERS[type]||!PADS[id]||this.towers.some(t=>t.id===id)||this.obstacle(id)||!['ready','wave'].includes(this.phase))return false;let def=TOWERS[type];if(this.coins<def.cost)return false;this.coins-=def.cost;this.towers.push({id,type,level:0,cool:.1,spent:def.cost,recoil:0,...PADS[id]});this.emit('build',{id,type});return true;}
 upgrade(id){let t=this.towers.find(t=>t.id===id);if(!t||t.level>=2||!['ready','wave'].includes(this.phase))return false;let cost=TOWERS[t.type].upgrade[t.level];if(this.coins<cost)return false;this.coins-=cost;t.spent+=cost;t.level++;this.emit('upgrade',{id});return true;}
 sell(id){let i=this.towers.findIndex(t=>t.id===id);if(i<0||!['ready','wave'].includes(this.phase))return false;let t=this.towers.splice(i,1)[0],money=Math.floor(t.spent*.7);this.coins+=money;this.emit('sell',{id,money});return true;}
 startWave(){if(this.phase!=='ready'||this.wave>=WAVES.length)return false;this.phase='wave';this.spawn=WAVES[this.wave].slice();this.wave++;this.spawnTimer=.4;this.emit('wave',{wave:this.wave});return true;}
 summon(type){let def=TYPES[type],scale=(1+(this.wave-1)*.065)*(this.mode==='gentle'?.8:1);let e={...def,type,id:this.nextId++,hp:def.hp*scale,maxHp:def.hp*scale,d:0,slow:0,flash:0,dead:false,...at(0)};this.enemies.push(e);this.emit('spawn',{id:e.id,type});return e;}
 damage(e,value,type){if(!e||e.dead)return;let armor=type==='frost'?0:e.armor;e.hp-=value*(1-armor);e.flash=.13;if(type==='frost')e.slow=2.3;if(e.hp<=0){e.dead=true;this.coins+=e.reward;this.kills++;this.emit('kill',{x:e.x,y:e.y,reward:e.reward,type:e.type});}}
 hitObstacle(o,damage){if(!o.alive)return;o.hp-=damage;if(o.hp<=0){o.hp=0;o.alive=false;this.coins+=o.reward;this.emit('clear',{id:o.id,money:o.reward,x:PADS[o.id].x,y:PADS[o.id].y});if(this.selectedObstacle===o.id)this.selectedObstacle=null;}}
 spell(x,y){if(this.spellCooldown>0||this.phase!=='wave')return false;this.spellCooldown=26;for(let e of this.enemies)if(!e.dead&&Math.hypot(e.x-x,e.y-y)<125){this.damage(e,190,'frost');e.slow=3.5;}this.emit('spell',{x,y});return true;}
 step(dt){if(!['ready','wave'].includes(this.phase))return;this.time+=dt;this.spellCooldown=Math.max(0,this.spellCooldown-dt);
 if(this.phase==='wave'){this.spawnTimer-=dt;while(this.spawn.length&&this.spawnTimer<=0){this.summon(this.spawn.shift());this.spawnTimer+=this.wave<3?1.45:this.wave<7?1.04:.88;}}
 for(let e of this.enemies){if(e.dead)continue;e.slow=Math.max(0,e.slow-dt);e.flash=Math.max(0,e.flash-dt);e.d+=e.speed*dt*(e.slow>0?(e.type==='boss'?.7:.5):1);Object.assign(e,at(e.d));if(e.d>=LENGTH){e.dead=true;this.life=Math.max(0,this.life-e.life);this.emit('leak',{life:e.life,x:e.x,y:e.y});}}
 for(let t of this.towers){t.cool-=dt;t.recoil=Math.max(0,t.recoil-dt);if(t.cool>0)continue;let def=TOWERS[t.type],r=def.range[t.level],target=null;for(let e of this.enemies)if(!e.dead&&Math.hypot(e.x-t.x,e.y-t.y)<r&&(!target||e.d>target.d))target=e;
 if(target){t.cool=def.rate[t.level];t.recoil=.18;this.projectiles.push({x:t.x,y:t.y-27,target:target.id,type:t.type,damage:def.damage[t.level],speed:t.type==='bloom'?270:430,radius:t.type==='bloom'?65+t.level*9:0});this.emit('shoot',{x:t.x,y:t.y,type:t.type});}
 else{let obs=this.obstacles.filter(o=>o.alive&&Math.hypot(PADS[o.id].x-t.x,PADS[o.id].y-t.y)<r).sort((a,b)=>(a.id===this.selectedObstacle?-1:0)-(b.id===this.selectedObstacle?-1:0))[0];if(obs){t.cool=def.rate[t.level];t.recoil=.18;this.hitObstacle(obs,def.damage[t.level]);this.emit('chip',{from:t.id,id:obs.id,type:t.type});}}
 }
 for(let p of this.projectiles){if(p.done)continue;let target=this.enemies.find(e=>e.id===p.target&&!e.dead);if(!target){p.done=true;continue;}let dx=target.x-p.x,dy=target.y-p.y,dist=Math.hypot(dx,dy);if(dist<p.speed*dt+8){if(p.radius){for(let e of this.enemies)if(!e.dead&&Math.hypot(e.x-target.x,e.y-target.y)<p.radius)this.damage(e,p.damage,p.type);this.emit('burst',{x:target.x,y:target.y,r:p.radius});}else this.damage(target,p.damage,p.type);p.done=true;}else{p.x+=dx/dist*p.speed*dt;p.y+=dy/dist*p.speed*dt;}}
 this.projectiles=this.projectiles.filter(p=>!p.done);this.enemies=this.enemies.filter(e=>!e.dead);
 if(this.life<=0){this.phase='defeat';this.emit('defeat');return;}
 if(this.phase==='wave'&&!this.spawn.length&&!this.enemies.length){this.coins+=25;this.projectiles=[];this.phase=this.wave>=WAVES.length?'victory':'ready';this.emit(this.phase==='victory'?'victory':'waveEnd',{wave:this.wave,money:25});}
 }
 snapshot(){return{version:1,mode:this.mode,coins:this.coins,life:this.life,wave:this.wave,towers:this.towers.map(t=>({...t})),obstacles:this.obstacles.map(o=>({...o})),kills:this.kills,spellCooldown:this.spellCooldown};}
 static restore(s){if(!s||s.version!==1||!Number.isInteger(s.wave)||s.wave<0||s.wave>=12||!Number.isInteger(s.life)||s.life<=0||s.life>20||!Number.isFinite(s.coins)||s.coins<0||!Array.isArray(s.towers)||!Array.isArray(s.obstacles))return null;let g=new Game(s.mode);g.coins=Math.max(0,s.coins);g.life=s.life;g.wave=s.wave;g.towers=s.towers.filter(t=>PADS[t.id]&&TOWERS[t.type]&&[0,1,2].includes(t.level)).map(t=>({...t,...PADS[t.id],cool:.1,recoil:0}));g.obstacles=s.obstacles.filter(o=>OBSTACLES.some(k=>k.id===o.id)).map(o=>({...o}));g.kills=s.kills||0;g.spellCooldown=s.spellCooldown||0;return g;}
}
const api={Game,TOWERS,TYPES,PADS,OBSTACLES,WAVES,PATH,LENGTH,at};if(typeof module!=='undefined')module.exports=api;root.LanternEngine=api;
})(typeof window!=='undefined'?window:globalThis);
