const {performance}=require('node:perf_hooks');
exports.createPoseStore=(now=()=>performance.now())=>{
 let latest=null,at=-Infinity,revision=0;
 return {
  accept(payload,retained=false){
   if(retained || Buffer.byteLength(payload)>1024)return false;
   let p;try{p=JSON.parse(payload);}catch{return false;}
   if(!p || p.v!==1 || typeof p.sessionId!=='string' || !/^[a-zA-Z0-9_-]{1,64}$/.test(p.sessionId) || !Number.isSafeInteger(p.seq) || p.seq<0 || !['NONE','PARRY'].includes(p.gesture) || typeof p.active!=='boolean' || !Number.isFinite(p.confidence) || p.confidence<0 || p.confidence>1 || (p.active !== (p.gesture==='PARRY')))return false;
   if(latest && latest.sessionId===p.sessionId && p.seq<=latest.seq)return false;
   latest={sessionId:p.sessionId,seq:p.seq,gesture:p.gesture,active:p.active,confidence:p.confidence};at=now();revision++;return true;
  },
  snapshot(){const age=latest?Math.max(0,now()-at):null;const valid=!!latest && age<=500;return {valid,revision,sessionId:latest?.sessionId??'',seq:latest?.seq??0,gesture:valid?latest.gesture:'NONE',active:valid?latest.active:false,confidence:valid?latest.confidence:0,ageMs:age};}
 };
};
exports.createControllerStore=(now=()=>performance.now())=>{
 let sample=null,at=-Infinity;
 return {
  accept(payload,retained=false){if(retained || !/^[01],[01],(?:-1|0|1)$/.test(payload))return false;sample=String(payload).split(',').map(Number);at=now();return true;},
  snapshot(){const age=sample?Math.max(0,now()-at):null,valid=!!sample && age<=200;return {valid,direction:valid?sample[2]:0,left:valid?!!sample[0]:false,right:valid?!!sample[1]:false,ageMs:age};}
 };
};
