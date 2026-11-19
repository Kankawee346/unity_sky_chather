const http = require("node:http");
const net = require("node:net");
const { performance } = require("node:perf_hooks");
const { createReadStream, existsSync, readFileSync, stat } = require("node:fs");
const { extname, join, resolve, sep } = require("node:path");
const createBroker = require("aedes");
const { handleRoundRequest, handleRoundAnalytics } = require("./rounds.cjs");
const { createGameStatusHandler } = require("./game-status.cjs");

const keyboardOnly = process.argv.includes("--keyboard-only");
const settingsPath = join(__dirname, "..", "Firmware", "CatchController", "Settings.h");
const settings = existsSync(settingsPath) ? readFileSync(settingsPath, "utf8") : "";
const host = process.env.CATCHLAB_MQTT_HOST ?? settings.match(/BrokerHost\[\]\s*=\s*"([^"]+)"/)?.[1];
const password = process.env.CATCHLAB_MQTT_PASSWORD ??
    settings.match(/BrokerPassword\[\]\s*=\s*"([^"]+)"/)?.[1];
if (!keyboardOnly && (!host || !password)) {
    throw new Error("Configure BrokerHost and BrokerPassword in Settings.h or set the bridge environment variables.");
}

const topic = "catchlab/controller";
const httpPort = Number(process.env.CATCHLAB_HTTP_PORT ?? 8787);
if (!Number.isInteger(httpPort) || httpPort < 1 || httpPort > 65535)
    throw new Error("CATCHLAB_HTTP_PORT must be a valid port number.");
// The original build stays the default; opt into the friend UI with --friend-ui.
const webRoot = process.argv.includes("--friend-ui")
    ? resolve(__dirname, "../CatchLabFriend/Build/Web")
    : resolve(__dirname, "../CatchLab/Build/Web");
const contentTypes = {
    ".html": "text/html; charset=utf-8",
    ".js": "application/javascript",
    ".css": "text/css",
    ".wasm": "application/wasm",
    ".json": "application/json",
    ".png": "image/png",
    ".svg": "image/svg+xml",
    ".ico": "image/x-icon"
};
const samplePattern = /^[01],[01],(?:-1|0|1)$/;
const waiters = new Set();
const changes = [];
let revision = Date.now();
let latestSample;
let receivedAt = -Infinity;
let broker;
const handleGameStatus = createGameStatusHandler(packet => {
    if (broker) broker.publish({ topic: "catchlab/game/status", payload: Buffer.from(packet), qos: 0, retain: true }, error => {
        if (error) console.warn("LCD status publish failed:", error.message);
    });
});

if (!keyboardOnly) {
    broker = createBroker();
    broker.authenticate = (client, username, supplied, done) => {
        done(null, username === "catchlab" && supplied?.toString() === password);
    };

    broker.on("publish", (packet, client) => {
        if (!client || packet.topic !== topic || packet.retain || packet.payload.length > 6)
            return;

        const sample = packet.payload.toString();
        if (samplePattern.test(sample)) {
            const now = performance.now();
            const changed = sample !== latestSample || now - receivedAt > 200;
            latestSample = sample;
            receivedAt = now;
            if (changed) {
                changes.push({ revision: ++revision, sample, at: now });
                if (changes.length > 32)
                    changes.shift();
                for (const respond of waiters)
                    respond();
            }
        }
    });

    net.createServer(broker.handle).listen(1883, host, () => {
        console.log(`MQTT listening on ${host}:1883`);
    });
} else {
    console.log("Keyboard mode: MQTT is disabled.");
}

http.createServer((request, response) => {
    const [path, query] = request.url.split("?");
    if (path === "/game/status") {
        handleGameStatus(request, response);
        return;
    }
    if (path === "/rounds") {
        void handleRoundRequest(request, response);
        return;
    }
    if (path === "/rounds/analytics") {
        void handleRoundAnalytics(request, response);
        return;
    }
    if (path !== "/controller/latest") {
        serveWebFile(request, response, path);
        return;
    }
    if (request.method !== "GET") {
        response.writeHead(405, { Allow: "GET" }).end();
        return;
    }

    response.setHeader("Cache-Control", "no-store");
    if (keyboardOnly) {
        response.writeHead(204).end();
        return;
    }
    const after = Number(new URLSearchParams(query).get("after"));
    if (!after || after > revision) {
        sendSample(response, latestSample, revision);
        return;
    }

    const next = changes.find(change => change.revision > after && performance.now() - change.at <= 120);
    if (next) {
        sendSample(response, next.sample, next.revision);
        return;
    }

    const respond = () => {
        clearTimeout(timer);
        waiters.delete(respond);
        sendSample(response, latestSample, revision);
    };
    const timer = setTimeout(respond, 80);
    waiters.add(respond);
    response.once("close", () => {
        clearTimeout(timer);
        waiters.delete(respond);
    });
}).listen(httpPort, "127.0.0.1", () => {
    console.log(`CatchLab Web: http://127.0.0.1:${httpPort}/`);
    console.log(`Unity endpoint: http://127.0.0.1:${httpPort}/controller/latest`);
});

function serveWebFile(request, response, path) {
    if (request.method !== "GET" && request.method !== "HEAD") {
        response.writeHead(405, { Allow: "GET, HEAD" }).end();
        return;
    }

    const relativePath = path === "/" ? "index.html" : path.slice(1);
    if (!/^[a-zA-Z0-9_./-]+$/.test(relativePath)) {
        response.writeHead(404).end();
        return;
    }

    const file = resolve(webRoot, relativePath);
    if (!file.startsWith(webRoot + sep)) {
        response.writeHead(404).end();
        return;
    }

    stat(file, (error, info) => {
        if (error || !info.isFile()) {
            response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" })
                .end(path === "/" ? `Build the selected Unity Web project into ${webRoot} first.` : undefined);
            return;
        }

        const encoding = file.endsWith(".br") ? "br" : file.endsWith(".gz") ? "gzip" : null;
        const suffixLength = encoding ? 3 : file.endsWith(".unityweb") ? 9 : 0;
        const originalFile = suffixLength ? file.slice(0, -suffixLength) : file;
        const headers = {
            "Cache-Control": "no-store",
            "Content-Length": info.size,
            "Content-Type": contentTypes[extname(originalFile)] ?? "application/octet-stream"
        };
        if (encoding)
            headers["Content-Encoding"] = encoding;

        response.writeHead(200, headers);
        if (request.method === "HEAD") {
            response.end();
            return;
        }

        const stream = createReadStream(file);
        stream.on("error", () => response.destroy());
        response.once("close", () => stream.destroy());
        stream.pipe(response);
    });
}

function sendSample(response, sample, sampleRevision) {
    if (performance.now() - receivedAt > 200) {
        response.writeHead(204).end();
        return;
    }

    response.writeHead(200, {
        "Content-Type": "text/plain; charset=utf-8",
        "X-CatchLab-Revision": sampleRevision
    }).end(sample);
}
