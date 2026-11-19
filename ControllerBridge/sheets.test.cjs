const { test } = require("node:test");
const assert = require("node:assert/strict");

process.env.CATCHLAB_SHEET_ID = "test-spreadsheet-id";
const { buildRows, syncSheet } = require("./sheets.cjs");

const round = (id, playerId, playCount, score, caught, seen, when) => ({
    id,
    data: () => ({ playerId, playCount, level: 2, score, itemsCaught: caught,
        itemsSeen: seen, durationMs: 30008, completedAt: { toDate: () => new Date(when) } })
});
const players = [
    { id: "pon", rounds: [round("a".repeat(32), "pon", 2, 84, 11, 24, "2026-11-13T06:32:38Z")] },
    { id: "guest", rounds: [round("b".repeat(32), "guest", 1, 10, 1, 1, "2026-11-12T00:00:00Z")] }
];
const db = { collection: name => {
    assert.equal(name, "players");
    return { get: async () => ({ docs: players.map(player => ({
        id: player.id,
        ref: { collection: child => {
            assert.equal(child, "rounds");
            return { get: async () => ({ docs: player.rounds }) };
        } }
    })) }) };
} };

test("rounds become Thai-readable rows in date order", async () => {
    const rows = await buildRows(db);
    assert.equal(rows.length, 3);
    assert.deepEqual(rows[1].slice(1, 5), ["guest", 1, 2, 10]);
    assert.deepEqual(rows[2].slice(1, 5), ["pon", 2, 2, 84]);
    assert.equal(rows[2][0], "2026-11-13 13:32:38");
    assert.equal(rows[2][7], 45.8);
    assert.equal(rows[2][8], 30);
});

test("sync creates only its own tab and fills all existing rounds", async () => {
    const calls = [];
    const request = async (method, url, data) => {
        calls.push({ method, url, data });
        if (method === "GET") return { sheets: [{ properties: { sheetId: 0, title: "Original" } }] };
        if (data?.requests?.[0]?.addSheet) return { replies: [{ addSheet: { properties: { sheetId: 7, title: "CatchLab Rounds" } } }] };
        return {};
    };
    const count = await syncSheet(db, request);
    assert.equal(count, 2);
    assert.equal(calls.filter(call => call.data?.requests?.[0]?.addSheet).length, 1);
    const write = calls.find(call => call.method === "PUT");
    assert.match(write.url, /CatchLab%20Rounds/);
    assert.equal(write.data.values.length, 3);
    assert.equal(write.data.values[2][4], 84);
    assert.ok(calls.every(call => !call.url.includes("Original!")));
});
