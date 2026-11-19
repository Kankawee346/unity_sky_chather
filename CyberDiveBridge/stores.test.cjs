const {test}=require('node:test');
const a=require('node:assert/strict');
const {createPoseStore,createControllerStore}=require('./stores.cjs');
const pose=(seq=1,sessionId='a')=>({v:1,sessionId,seq,gesture:'PARRY',active:true,confidence:.95});
test('pose expires and duplicate sequence cannot keep stale action alive',()=>{
 let now=0;const s=createPoseStore(()=>now); a.equal(s.snapshot().valid,false);
 a.equal(s.accept(JSON.stringify(pose())),true);a.equal(s.snapshot().active,true);
 now=500;a.equal(s.snapshot().valid,true); a.equal(s.accept(JSON.stringify(pose())),false);
 now=501;a.equal(s.snapshot().valid,false);a.equal(s.snapshot().active,false);
 a.equal(s.accept(JSON.stringify(pose(1,'b'))),true);a.equal(s.snapshot().sessionId,'b');
});
test('invalid and retained packets never overwrite fresh pose',()=>{
 const s=createPoseStore(()=>0);
 for(const p of ['bad','null','[]',JSON.stringify({...pose(),v:2}),JSON.stringify({...pose(),confidence:2}),JSON.stringify({...pose(),seq:-1}),JSON.stringify({...pose(),active:'true'}),JSON.stringify({...pose(),gesture:'HACK'}),JSON.stringify({...pose(),sessionId:''}),'x'.repeat(1025)])a.equal(s.accept(p),false);
 a.equal(s.accept(JSON.stringify(pose()),true),false);
 a.equal(s.accept(JSON.stringify(pose())),true);
 a.equal(s.accept(JSON.stringify(pose(0))),false);a.equal(s.snapshot().seq,1);
});
test('controller modes remain isolated and stale direction becomes neutral',()=>{
 let now=0;const s=createControllerStore(()=>now);
 a.equal(s.accept('1,0,1'),true);a.equal(s.snapshot().left,true);a.equal(s.snapshot().direction,1);
 a.equal(s.accept('1,0,2'),false);a.equal(s.accept('1,0,1',true),false);
 now=201;a.equal(s.snapshot().valid,false);a.equal(s.snapshot().direction,0);a.equal(s.snapshot().left,false);
});
