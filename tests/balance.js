const {Game,PADS}=require('../src/engine');
for(const mode of ['normal','gentle']){
const g=new Game(mode);g.build(0,'spark');g.build(4,'bloom');g.build(1,'spark');
const plan=[[9,'frost'],[5,'bloom'],[7,'spark'],[10,'spark'],[8,'frost'],[2,'bloom'],[11,'spark'],[6,'spark'],[3,'spark']];
for(let w=0;w<12;w++){
 for(let i=0;i<300;i++)g.step(1/60);
 for(let [id,type] of plan)g.build(id,type);
 for(let id of [4,0,9,5,7,10,8,2,1,11,6,3])g.upgrade(id);
 const money=g.coins;g.startWave();let spells=0;
 for(let i=0;i<7000&&g.phase==='wave';i++){
 g.step(1/60);
 if(g.spellCooldown===0&&g.enemies.length){let boss=g.enemies.find(e=>e.type==='boss'&&e.d>400);let e=boss||g.enemies.find(e=>e.d>500&&g.enemies.filter(q=>Math.hypot(q.x-e.x,q.y-e.y)<125).length>=3);if(e){g.spell(e.x,e.y);spells++}}
 for(let [id,type] of plan)g.build(id,type);
 for(let id of [4,0,9,5,7,10,8,2,1,11,6,3])g.upgrade(id);
 }
 console.log(mode,w+1,'life',g.life,'towers',g.towers.map(t=>t.id+':'+t.level).join(','),'money',money,'spells',spells,'phase',g.phase);
 if(g.phase==='defeat')break;
}
}
