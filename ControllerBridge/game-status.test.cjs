const test=require("node:test");
const assert=require("node:assert/strict");
const http=require("node:http");
const {normalizeStatus,encodeStatus,createGameStatusHandler}=require("./game-status.cjs");
const status={score:42,seconds:29,level:3,mode:"FSR",state:"PLAY"};
test("LCD packet contains bounded score, timer, difficulty, mode and state",()=>{
    assert.equal(encodeStatus(normalizeStatus(status)),"42,29,3,FSR,PLAY");
    for(const change of [{score:-1},{seconds:31},{seconds:1.2},{level:4},{mode:"IR"},{state:"PLAY,EVIL"}])
        assert.equal(normalizeStatus({...status,...change}),null);
});
test("endpoint delivers updated state, suppresses duplicates, rejects malformed/large data",async()=>{
    const packets=[];
    const server=http.createServer(createGameStatusHandler(p=>packets.push(p)));
    await new Promise(resolve=>server.listen(0,"127.0.0.1",resolve));
    const url=`http://127.0.0.1:${server.address().port}/game/status`;
    try {
        const post=body=>fetch(url,{method:"POST",body:JSON.stringify(body)});
        assert.equal((await post(status)).status,204);
        assert.equal((await post(status)).status,204);
        assert.equal(packets.length,1);
        assert.equal((await post({...status,mode:"JOY",state:"PAUSE"})).status,204);
        assert.equal(packets[1],"42,29,3,JOY,PAUSE");
        assert.equal((await (await fetch(url)).json()).mode,"JOY");
        assert.equal((await post({...status,score:-1})).status,400);
        assert.equal((await post("x".repeat(513))).status,413);
        assert.equal(packets.length,2);
    } finally { await new Promise(resolve=>server.close(resolve)); }
});
