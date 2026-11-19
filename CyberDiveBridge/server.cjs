// Exported factory lets integration tests use isolated ephemeral ports.
exports.startBridge=async function startBridge({host='127.0.0.1',mqttPort=1883,httpPort=8787,password}={}){
 if(!password)throw Error('Set CYBER_MQTT_PASSWORD in .env.local');
 const http=require('node:http'),net=require('node:net');
 const {Aedes}=await import('aedes');
 const broker=await Aedes.createBroker();const {createPoseStore,createControllerStore}=require('./stores.cjs');
 const pose=createPoseStore(),controller=createControllerStore();
 broker.authenticate=(client,user,pass,done)=>done(null,user==='catchlab' && pass?.toString()===password);
 broker.authorizePublish=(client,packet,done)=>done(!packet.retain && packet.payload.length<=1024 && (packet.topic==='catchlab/pose'||packet.topic==='catchlab/controller')?null:Error('Packet denied'));
 broker.on('publish',(p,client)=>{if(!client)return; if(p.topic==='catchlab/pose')pose.accept(p.payload.toString(),p.retain);if(p.topic==='catchlab/controller')controller.accept(p.payload.toString(),p.retain);});
 const mqtt=net.createServer(broker.handle);
 const web=http.createServer((req,res)=>{
  res.setHeader('Cache-Control','no-store');res.setHeader('Content-Type','application/json');
  if(req.method!=='GET'){res.writeHead(405,{Allow:'GET'}).end();return;}
  const path=req.url.split('?')[0];
  const data=path==='/pose/latest'?pose.snapshot():path==='/controller/latest'?controller.snapshot():path==='/health'?{ok:true}:null;
  if(!data){res.writeHead(404).end('{}');return;}res.end(JSON.stringify(data));
 });
 const listen=(server,port,address)=>new Promise((resolve,reject)=>{server.once('error',reject);server.listen(port,address,()=>{server.removeListener('error',reject);resolve();});});
 try{await listen(mqtt,mqttPort,host);await listen(web,httpPort,'127.0.0.1');}catch(e){mqtt.close();broker.close();throw e;}
 return {mqttPort:mqtt.address().port,httpPort:web.address().port,async close(){await new Promise(r=>web.close(r));await new Promise(r=>broker.close(r));await new Promise(r=>mqtt.close(r));}};
};
if(require.main===module)exports.startBridge({host:process.env.CYBER_MQTT_HOST,mqttPort:1883,httpPort:8787,password:process.env.CYBER_MQTT_PASSWORD}).then(b=>console.log(`CYBER//DIVE MQTT ${b.mqttPort}; HTTP http://127.0.0.1:${b.httpPort}`)).catch(e=>{console.error(e.message);process.exitCode=1;});
