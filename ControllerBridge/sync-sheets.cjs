const { applicationDefault, initializeApp } = require("firebase-admin/app");
const { getFirestore } = require("firebase-admin/firestore");
const { syncSheet } = require("./sheets.cjs");

async function main() {
    const projectId = process.env.CATCHLAB_FIREBASE_PROJECT_ID;
    if (!projectId) throw new Error("Set CATCHLAB_FIREBASE_PROJECT_ID in .env.local.");
    const app = initializeApp({ projectId, credential: applicationDefault() });
    await syncSheet(getFirestore(app));
}

main().catch(error => {
    const detail = error.response?.data?.error?.message ?? error.message;
    console.error("Google Sheets sync failed:", detail);
    process.exitCode = 1;
});
