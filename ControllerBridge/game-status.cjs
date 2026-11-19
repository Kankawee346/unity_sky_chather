const states = new Set(["READY", "PLAY", "PAUSE", "END"]);
function normalizeStatus(data) {
    if (!data || !Number.isInteger(data.score) || data.score < 0 || data.score > 999999 ||
        !Number.isInteger(data.seconds) || data.seconds < 0 || data.seconds > 30 ||
        !Number.isInteger(data.level) || data.level < 1 || data.level > 3 ||
        !["FSR", "JOY"].includes(data.mode) || !states.has(data.state)) return null;
    return { score: data.score, seconds: data.seconds, level: data.level, mode: data.mode, state: data.state };
}
function encodeStatus(s) { return `${s.score},${s.seconds},${s.level},${s.mode},${s.state}`; }
function createGameStatusHandler(publish) {
    let latest = null;
    let publishedAt = 0;
    return (request, response) => {
        response.setHeader("Cache-Control", "no-store");
        if (request.method === "GET") {
            response.writeHead(200, {"Content-Type":"application/json"}).end(JSON.stringify(latest)); return;
        }
        if (request.method !== "POST") { response.writeHead(405, {Allow:"GET, POST"}).end(); return; }
        let raw="", tooLarge=false;
        request.on("data", chunk => {
            if (tooLarge) return;
            raw+=chunk;
            if (Buffer.byteLength(raw)>512) { tooLarge=true; response.writeHead(413).end(); }
        });
        request.on("error", () => response.destroy());
        request.on("end", () => {
            if (tooLarge) return;
            let data;
            try { data=normalizeStatus(JSON.parse(raw)); } catch { data=null; }
            if (!data) { response.writeHead(400).end("Invalid game status"); return; }
            // Keep heartbeat delivery so a reconnected LCD never holds stale game state.
            const now=Date.now();
            if (!latest || encodeStatus(data)!==encodeStatus(latest) || now-publishedAt>1500) {
                publish(encodeStatus(data)); publishedAt=now;
            }
            latest=data;
            response.writeHead(204).end();
        });
    };
}
module.exports={normalizeStatus,encodeStatus,createGameStatusHandler};
