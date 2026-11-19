const {test}=require('node:test');
const assert=require('node:assert/strict');
const net=require('node:net');
const packet=require('mqtt-packet');
const {startBridge}=require('./server.cjs');
function connect(port,password){
 return new Promise((resolve,reject)=>{
  const socket=net.connect(port,'127.0.0.1');const parser=packet.parser();
  const timer=setTimeout(()=>{socket.destroy();reject(Error('MQTT connect timed out'));},2000);
  socket.on('error',reject);socket.on('data',d=>parser.parse(d));
  parser.on('packet',p=>{if(p.cmd==='connack'){clearTimeout(timer);resolve({socket,code:p.returnCode});}});
  socket.on('connect',()=>socket.write(packet.generate({cmd:'connect',protocolId:'MQTT',protocolVersion:4,clean:true,clientId:'test-pose',keepalive:30,username:'catchlab',password:Buffer.from(password)})));
 });
}
test('real MQTT auth and publish reach HTTP; stale inputs fail safe',{timeout:10000},async()=>{
 const b=await startBridge({host:'127.0.0.1',mqttPort:0,httpPort:0,password:'test-only-secret'});
 let good,bad;
 try{
  bad=await connect(b.mqttPort,'wrong');assert.notEqual(bad.code,0);bad.socket.destroy();
  good=await connect(b.mqttPort,'test-only-secret');assert.equal(good.code,0);
  const payload=JSON.stringify({v:1,sessionId:'integration',seq:1,gesture:'NONE',active:false,confidence:.99});
  good.socket.write(packet.generate({cmd:'publish',topic:'catchlab/pose',payload,qos:0,retain:false,dup:false}));
  let state;
  for(let i=0;i<15;i++){
   state=await (await fetch(`http://127.0.0.1:${b.httpPort}/pose/latest`)).json();
   if(state.valid)break;await new Promise(r=>setTimeout(r,20));
  }
  assert.equal(state.valid,true);assert.equal(state.sessionId,'integration');assert.equal(state.gesture,'NONE');
  const method=await fetch(`http://127.0.0.1:${b.httpPort}/pose/latest`,{method:'POST'});assert.equal(method.status,405);
  await new Promise(r=>setTimeout(r,550));
  const expired=await (await fetch(`http://127.0.0.1:${b.httpPort}/pose/latest`)).json();assert.equal(expired.valid,false);assert.equal(expired.active,false);
 }finally{good?.socket.destroy();bad?.socket.destroy();await b.close();}
});
