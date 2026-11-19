const { GoogleAuth } = require("google-auth-library");

const spreadsheetId = process.env.CATCHLAB_SHEET_ID?.trim();
const tabTitle = "CatchLab Rounds";
const columns = [
    "วันเวลา (ไทย)", "ผู้เล่น", "รอบที่", "ระดับ", "คะแนน",
    "รับได้", "ของทั้งหมด", "ความแม่นยำ (%)", "เวลา (วินาที)", "รหัสรอบ"
];
const auth = spreadsheetId ? new GoogleAuth({
    scopes: ["https://www.googleapis.com/auth/spreadsheets"]
}) : null;
let pendingSync;
let syncAgain = false;

function isSheetConfigured() {
    return Boolean(spreadsheetId && /^[a-zA-Z0-9_-]+$/.test(spreadsheetId));
}

function queueSheetSync(db) {
    if (!isSheetConfigured()) return;
    if (pendingSync) {
        syncAgain = true;
        return;
    }
    pendingSync = (async () => {
        do {
            syncAgain = false;
            await syncSheet(db);
        } while (syncAgain);
    })().catch(error => {
        const detail = error.response?.data?.error?.message ?? error.message;
        console.warn("Google Sheets sync failed:", String(detail).slice(0, 250));
    }).finally(() => { pendingSync = undefined; });
}

async function syncSheet(db, request = googleRequest) {
    if (!isSheetConfigured())
        throw new Error("Set CATCHLAB_SHEET_ID to the ID from your Google Sheet URL.");

    const rows = await buildRows(db);
    const root = `https://sheets.googleapis.com/v4/spreadsheets/${spreadsheetId}`;
    const metadata = await request("GET", `${root}?fields=sheets(properties(sheetId,title,gridProperties(rowCount)))`);
    let sheet = metadata.sheets?.find(item => item.properties.title === tabTitle)?.properties;
    if (!sheet) {
        const created = await request("POST", `${root}:batchUpdate`, {
            requests: [{ addSheet: { properties: { title: tabTitle } } }]
        });
        sheet = created.replies[0].addSheet.properties;
        await request("POST", `${root}:batchUpdate`, {
            requests: [
                { updateSheetProperties: { properties: { sheetId: sheet.sheetId, gridProperties: { frozenRowCount: 1 } }, fields: "gridProperties.frozenRowCount" } },
                { repeatCell: { range: { sheetId: sheet.sheetId, startRowIndex: 0, endRowIndex: 1 }, cell: { userEnteredFormat: { textFormat: { bold: true }, backgroundColor: { red: 0.87, green: 0.93, blue: 1 } } }, fields: "userEnteredFormat" } },
                { setBasicFilter: { filter: { range: { sheetId: sheet.sheetId, startRowIndex: 0, startColumnIndex: 0, endColumnIndex: columns.length } } } }
            ]
        });
    }

    if (rows.length > (sheet.gridProperties?.rowCount ?? 1000)) {
        await request("POST", `${root}:batchUpdate`, {
            requests: [{ appendDimension: { sheetId: sheet.sheetId, dimension: "ROWS", length: rows.length - (sheet.gridProperties?.rowCount ?? 1000) } }]
        });
    }

    const range = `'${tabTitle}'!A1:J${rows.length}`;
    await request("PUT", `${root}/values/${encodeURIComponent(range)}?valueInputOption=RAW`, {
        range,
        majorDimension: "ROWS",
        values: rows
    });
    console.log(`Google Sheets synced: ${rows.length - 1} rounds`);
    return rows.length - 1;
}

async function googleRequest(method, url, data) {
    const client = await auth.getClient();
    const response = await client.request({ method, url, data, timeout: 10000 });
    return response.data;
}

async function buildRows(db) {
    const rounds = [];
    const players = await db.collection("players").get();
    for (const player of players.docs) {
        const snapshot = await player.ref.collection("rounds").get();
        for (const doc of snapshot.docs) {
            const data = doc.data();
            const completedAt = data.completedAt?.toDate?.() ?? null;
            rounds.push({ ...data, roundId: doc.id, playerId: data.playerId ?? player.id, completedAt });
        }
    }
    rounds.sort((a, b) => (a.completedAt?.getTime() ?? 0) - (b.completedAt?.getTime() ?? 0)
        || a.playerId.localeCompare(b.playerId) || a.playCount - b.playCount);

    return [columns, ...rounds.map(round => [
        round.completedAt ? new Intl.DateTimeFormat("sv-SE", {
            timeZone: "Asia/Bangkok", year: "numeric", month: "2-digit", day: "2-digit",
            hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23"
        }).format(round.completedAt) : "",
        round.playerId,
        round.playCount,
        round.level,
        round.score,
        round.itemsCaught,
        round.itemsSeen,
        round.itemsSeen > 0 ? Math.round(1000 * round.itemsCaught / round.itemsSeen) / 10 : "",
        Math.round(round.durationMs / 100) / 10,
        round.roundId
    ])];
}

module.exports = { buildRows, isSheetConfigured, queueSheetSync, syncSheet };
