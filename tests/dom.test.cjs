const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {createCanvas,GlobalFonts}=require('@napi-rs/canvas');const ROOT=path.resolve(__dirname,'..');
(async()=>{
const {Window}=await import(require.resolve('happy-dom',{paths:[process.env.GAME_TEST_DEPS||ROOT]}));
let fontDir=process.env.GAME_FONT_DIR||'/root/.local/share/fonts';for(let [file,family]of [['NotoSansCJK-Regular.ttc','Microsoft YaHei'],['NotoSerifCJK-Regular.ttc','SimSun']]){let p=path.join(fontDir,file);if(fs.existsSync(p))GlobalFonts.registerFromPath(p,family);}
function environment(saved={}){
 let window=new Window({url:'file:///LanternWard.html?test=1',width:1280,height:800});window.document.write(fs.readFileSync(path.join(ROOT,'src/index.html'),'utf8'));let frame;window.requestAnimationFrame=f=>{frame=f;return 1};window.cancelAnimationFrame=()=>{};
 window.HTMLCanvasElement.prototype.getContext=function(){if(!this._canvas){this._canvas=createCanvas(this.width,this.height);let c=this._canvas.getContext('2d');this._ctx=new Proxy(c,{get(target,k){let v=target[k];if(k==='drawImage')return(img,...args)=>target.drawImage(img._canvas||img,...args);return typeof v==='function'?v.bind(target):v;},set(target,k,v){target[k]=v;return true;}});}return this._ctx};
 window.HTMLCanvasElement.prototype.getBoundingClientRect=function(){return{left:0,top:0,width:1280,height:800,right:1280,bottom:800}};
 window.document.hasFocus=()=>true;
 for(let[k,v]of Object.entries(saved))window.localStorage.setItem(k,v);
 for(let file of ['engine.js','game.js'])window.eval(fs.readFileSync(path.join(ROOT,'src',file),'utf8'));
 return{window,doc:window.document,frame:ts=>frame(ts),click:id=>{let el=window.document.getElementById(id);assert(el,id+' exists');el.click()},export:()=>Object.fromEntries(['lantern-ward-v01','lantern-ward-best-v01'].map(k=>[k,window.localStorage.getItem(k)]).filter(([k,v])=>v))};
}
let env=environment(),{window:w,doc:d}=env;
env.click('newGame');env.frame(16);assert.equal(w.__test.game.phase,'ready');
let scene=d.getElementById('scene');function pointer(x,y){scene.onpointerdown({clientX:x,clientY:y,button:0})}
pointer(130,370);d.querySelector('[data-build="spark"]').click();assert.equal(w.__test.game.coins,180);
pointer(425,348);d.querySelector('[data-build="bloom"]').click();assert.equal(w.__test.game.coins,70);
pointer(265,240);d.querySelector('[data-build="spark"]').click();assert.equal(w.__test.game.coins,0);
pointer(100,600);env.click('startWave');w.__test.step(2000);w.__test.render();
fs.writeFileSync(path.join(ROOT,'dist/scene-preview.png'),scene._canvas.toBuffer('image/png'));
w.__test.step(7000);assert.equal(w.__test.game.phase,'ready');assert.equal(w.__test.game.life,20);
let store=env.export();assert.equal(JSON.parse(store['lantern-ward-v01']).wave,1);
env=environment(store);w=env.window;d=env.doc;env.click('continueGame');assert.equal(w.__test.game.wave,1);assert.equal(w.__test.game.towers.length,3);
env.click('pause');assert(w.__test.paused);let time=w.__test.game.time;env.frame(100);env.frame(200);assert.equal(w.__test.game.time,time);env.click('resumeBtn');assert(!w.__test.paused);
env.click('help');assert(w.__test.paused);env.click('closeHelp');assert(!w.__test.paused);env.click('speed');assert.equal(d.getElementById('speed').textContent,'2×');
env.click('menuBtn');env.click('confirmBack');assert(!d.getElementById('menu').classList.contains('hidden'));env.click('newGame');env.click('confirmNew');assert.equal(w.__test.game.wave,0);
w.__test.game.coins=10000;w.__test.select(0);d.querySelector('[data-build="spark"]').click();env.click('upgradeBtn');assert.equal(w.__test.game.towers[0].level,1);env.click('sellBtn');assert.equal(w.__test.game.towers.length,0);
env.click('startWave');w.__test.step(300);env.click('spell');scene=d.getElementById('scene');pointer=(x,y)=>scene.onpointerdown({clientX:x,clientY:y,button:0});pointer(180,280);assert(w.__test.game.spellCooldown>0);
while(w.__test.game.phase!=='defeat'&&w.__test.game.wave<12){if(w.__test.game.phase==='ready')w.__test.game.startWave();w.__test.step(7500)}assert.equal(w.__test.game.phase,'defeat');assert(d.getElementById('againBtn'));env.click('againBtn');assert.equal(w.__test.game.life,20);
// Reproduce a real-economy twelve-wave victory through the actual event/UI layer.
let captured=false;let g=w.__test.game;g.build(0,'spark');g.build(4,'bloom');g.build(1,'spark');let plan=[[9,'frost'],[5,'bloom'],[7,'spark'],[10,'spark'],[8,'frost'],[2,'bloom'],[11,'spark'],[6,'spark'],[3,'spark']];
for(let wave=0;wave<12;wave++){
 w.__test.step(300);for(let[id,t]of plan)g.build(id,t);for(let id of [4,0,9,5,7,10,8,2,1,11,6,3])g.upgrade(id);env.click('startWave');
 for(let i=0;i<1000&&g.phase==='wave';i++){w.__test.step(6);for(let[id,t]of plan)g.build(id,t);for(let id of [4,0,9,5,7,10,8,2,1,11,6,3])g.upgrade(id);if(wave===11&&!captured&&g.enemies.some(e=>e.type==='boss'&&e.d>300)){w.__test.render(2);w.__test.render();let preview=createCanvas(984,800);preview.getContext('2d').drawImage(d.getElementById('scene')._canvas,0,0);fs.writeFileSync(path.join(ROOT,'dist/scene-preview.png'),preview.toBuffer('image/png'));captured=true;}if(g.spellCooldown===0&&g.enemies.length>4){let e=g.enemies.at(-3);g.spell(e.x,e.y)}}
 assert.notEqual(g.phase,'defeat');
}
assert.equal(g.phase,'victory');assert(d.getElementById('againBtn'));assert(env.export()['lantern-ward-best-v01']);assert(!env.export()['lantern-ward-v01']);
console.log('PASS actual DOM handlers: pointer build, upgrade, sell, spell, pause, help, speed, menu, checkpoint reload, defeat/retry, twelve-wave victory + best score; actual canvas rendered.');
await env.window.happyDOM.abort();
})().catch(e=>{console.error(e);process.exit(1)});
