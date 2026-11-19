const projectId = process.env.CATCHLAB_FIREBASE_PROJECT_ID;
const defaultPlayerId = process.env.CATCHLAB_PLAYER_ID ?? "demo";
const playerPattern = /^[\p{L}\p{M}\p{N} _-]{1,40}$/u;
const maxBodyBytes = 2048;
const analyticsRoundLimit = 100;
const firebaseConfigured = Boolean(projectId) && playerPattern.test(defaultPlayerId);
const { queueSheetSync } = require("./sheets.cjs");
let firestoreConnection;

async function connectFirestore() {
    const { applicationDefault, initializeApp } = await import("firebase-admin/app");
    const { FieldValue, getFirestore } = await import("firebase-admin/firestore");
    const app = initializeApp({ projectId, credential: applicationDefault() });
    return { db: getFirestore(app), FieldValue };
}

async function handleRoundRequest(request, response) {
    response.setHeader("Cache-Control", "no-store");
    if (request.method !== "POST") {
        response.setHeader("Allow", "POST");
        sendJson(response, 405, { error: "Use POST." });
        return;
    }
    if (request.headers["content-type"]?.split(";")[0] !== "application/json") {
        sendJson(response, 415, { error: "Use application/json." });
        return;
    }
    if (!firebaseConfigured) {
        sendJson(response, 503, { error: "Configure the Firebase project and a valid player ID." });
        return;
    }

    const chunks = [];
    let bytes = 0;
    try {
        for await (const chunk of request) {
            bytes += chunk.length;
            if (bytes <= maxBodyBytes)
                chunks.push(chunk);
        }
    } catch {
        return;
    }
    if (bytes > maxBodyBytes) {
        sendJson(response, 413, { error: "Round data is too large." });
        return;
    }

    let round;
    try {
        round = JSON.parse(Buffer.concat(chunks).toString("utf8"));
    } catch {
        sendJson(response, 400, { error: "Invalid round JSON." });
        return;
    }

    if (!isValidRound(round)) {
        sendJson(response, 400, { error: "Invalid round fields." });
        return;
    }

    try {
        firestoreConnection ??= connectFirestore();
        const connection = await firestoreConnection;
        const result = await saveRound(connection, round, round.playerId ?? defaultPlayerId);
        if (result.created) queueSheetSync(connection.db);
        sendJson(response, result.created ? 201 : 200, { playCount: result.playCount });
    } catch (error) {
        console.error("Firestore round save failed:", error.code ?? error.name);
        sendJson(response, 502, { error: "Could not save the round to Firestore." });
    }
}

async function handleRoundAnalytics(request, response) {
    response.setHeader("Cache-Control", "no-store");
    if (request.method !== "GET") {
        response.setHeader("Allow", "GET");
        sendJson(response, 405, { error: "Use GET." });
        return;
    }
    if (!firebaseConfigured) {
        sendJson(response, 503, { error: "Configure the Firebase project and a valid player ID." });
        return;
    }

    try {
        firestoreConnection ??= connectFirestore();
        const { db } = await firestoreConnection;
        const requestedPlayer = new URL(request.url, "http://localhost").searchParams.get("playerId") ?? defaultPlayerId;
        if (!playerPattern.test(requestedPlayer) || requestedPlayer.trim() !== requestedPlayer) {
            sendJson(response, 400, { error: "Invalid player ID." });
            return;
        }
        const snapshot = await db.collection("players").doc(requestedPlayer).collection("rounds")
            .orderBy("playCount", "desc").limit(analyticsRoundLimit).get();
        const rounds = snapshot.docs.map(doc => doc.data()).reverse();
        sendJson(response, 200, {
            playerId: requestedPlayer,
            windowLimit: analyticsRoundLimit,
            roundsAnalyzed: rounds.length,
            levels: [1, 2, 3].map(level => summarizeLevel(level, rounds))
        });
    } catch (error) {
        console.error("Firestore round analytics failed:", error.code ?? error.name);
        sendJson(response, 502, { error: "Could not read rounds from Firestore." });
    }
}

function summarizeLevel(level, rounds) {
    const history = rounds.filter(round => round.level === level).map(round => {
        const hasCounts = isIntegerBetween(round.itemsSeen, 0, 1000) &&
            isIntegerBetween(round.itemsCaught, 0, round.itemsSeen);
        return {
            playCount: round.playCount,
            score: round.score,
            durationMs: round.durationMs,
            completedAt: round.completedAt?.toDate?.().toISOString() ?? null,
            itemsCaught: hasCounts ? round.itemsCaught : null,
            itemsSeen: hasCounts ? round.itemsSeen : null,
            accuracyPercent: hasCounts ? accuracyPercent(round.itemsCaught, round.itemsSeen) : null
        };
    });
    const measured = history.filter(round => round.accuracyPercent !== null);
    const first = measured[0];
    const last = measured.at(-1);
    return {
        level,
        roundCount: history.length,
        accuracyPercent: accuracyPercent(
            measured.reduce((sum, round) => sum + round.itemsCaught, 0),
            measured.reduce((sum, round) => sum + round.itemsSeen, 0)
        ),
        changePercentagePoints: measured.length >= 2
            ? roundToTenth(100 * (last.itemsCaught / last.itemsSeen - first.itemsCaught / first.itemsSeen))
            : null,
        history
    };
}

function accuracyPercent(caught, seen) {
    return seen > 0 ? roundToTenth(100 * caught / seen) : null;
}

function roundToTenth(value) {
    return Math.round(value * 10) / 10;
}

function isValidRound(round) {
    return round && typeof round.roundId === "string" && /^[a-f0-9]{32}$/.test(round.roundId) &&
        (round.playerId === undefined || (typeof round.playerId === "string" && playerPattern.test(round.playerId) && round.playerId.trim() === round.playerId)) &&
        isIntegerBetween(round.level, 1, 3) &&
        isIntegerBetween(round.durationMs, 1, 600000) &&
        isIntegerBetween(round.itemsSeen, 0, 1000) &&
        isIntegerBetween(round.itemsCaught, 0, round.itemsSeen) &&
        // Original UI uses 1/2/3; friend UI uses fuel 0, gold 4 and red 10.
        isIntegerBetween(round.score, 0, round.itemsCaught * 10);
}

function isIntegerBetween(value, min, max) {
    return Number.isInteger(value) && value >= min && value <= max;
}

async function saveRound({ db, FieldValue }, round, playerId) {
    const player = db.collection("players").doc(playerId);
    const result = player.collection("rounds").doc(round.roundId);
    return db.runTransaction(async transaction => {
        const existing = await transaction.get(result);
        if (existing.exists)
            return { created: false, playCount: existing.get("playCount") };

        const profile = await transaction.get(player);
        const playCount = (profile.get("playCount") ?? 0) + 1;
        transaction.set(player, { playCount }, { merge: true });
        transaction.create(result, {
            playerId,
            score: round.score,
            level: round.level,
            durationMs: round.durationMs,
            itemsCaught: round.itemsCaught,
            itemsSeen: round.itemsSeen,
            playCount,
            completedAt: FieldValue.serverTimestamp()
        });
        return { created: true, playCount };
    });
}

function sendJson(response, status, body) {
    if (!response.destroyed)
        response.writeHead(status, { "Content-Type": "application/json" }).end(JSON.stringify(body));
}

module.exports = { handleRoundRequest, handleRoundAnalytics, isValidRound };
