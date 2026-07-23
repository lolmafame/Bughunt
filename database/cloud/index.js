const functions = require("firebase-functions");
const { onSchedule } = require("firebase-functions/v2/scheduler");
const admin = require("firebase-admin");

if (!admin.apps.length) admin.initializeApp();

// ─────────────────────────────────────────────────────────────────────────────
// MAYA SANDBOX KEYS  (sandbox only — swap for Secret Manager in production)
// ─────────────────────────────────────────────────────────────────────────────
const MAYA_PUBLIC_KEY = "pk-eo4sL393CWU5KmveJUaW8V730TTei2zY8zE4dHJDxkF";
const MAYA_SECRET_KEY = "sk-KfmfLJXFdV5t1inYN8lIOwSrueC1G27SCAklBqYCdrU";

// ─────────────────────────────────────────────────────────────────────────────
// SECURE SERVER-SIDE PRICE CATALOG
// ─────────────────────────────────────────────────────────────────────────────
const storePrices = {
    "player_skin1": 50.00,
    "premium_plan": 200.00
};

const PENDING_EXPIRY_MS = 30 * 60 * 1000; // 30 minutes

// ─────────────────────────────────────────────────────────────────────────────
// HELPER: Resolve the caller's playerId from either context.auth OR an explicit
// idToken in the payload.
// ─────────────────────────────────────────────────────────────────────────────
async function resolvePlayerId(context, requestData) {
    if (context.auth && context.auth.uid) {
        return context.auth.uid;
    }

    const idToken = requestData.idToken;
    if (idToken) {
        try {
            const decoded = await admin.auth().verifyIdToken(idToken);
            return decoded.uid;
        } catch (err) {
            console.error("resolvePlayerId: verifyIdToken failed:", err.message);
            return null;
        }
    }

    return null;
}

// ─────────────────────────────────────────────────────────────────────────────
// HELPER: Resolve a human-readable label for a player for debug logs.
// Returns "DisplayName (uid)" — falls back gracefully if Auth lookup fails.
// ─────────────────────────────────────────────────────────────────────────────
async function getPlayerLabel(playerId) {
    try {
        const userRecord = await admin.auth().getUser(playerId);
        const name = userRecord.displayName || userRecord.email || null;
        return name ? `${name} (${playerId})` : playerId;
    } catch (err) {
        // Non-fatal — keep the uid so logs are never blank
        return playerId;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// HELPER: Clean up stale PENDING orders for a given player
// ─────────────────────────────────────────────────────────────────────────────
async function cleanupStalePendingPayments(playerId) {
    const cutoff = new Date(Date.now() - PENDING_EXPIRY_MS);
    const db     = admin.firestore();

    const staleDocs = await db.collection("payments")
        .where("playerId",  "==", playerId)
        .where("status",    "==", "PENDING")
        .where("createdAt", "<",  cutoff)
        .get();

    if (staleDocs.empty) return;

    const batch = db.batch();
    staleDocs.forEach(doc => batch.update(doc.ref, { status: "EXPIRED" }));
    await batch.commit();
    console.log(`Cleaned up ${staleDocs.size} stale PENDING order(s) for player ${playerId}`);
}

// ─────────────────────────────────────────────────────────────────────────────
// 1. CHECKOUT CREATOR
// ─────────────────────────────────────────────────────────────────────────────
exports.createMayaCheckout = functions.https.onCall(async (data, context) => {
    console.log("RAW DATA FROM UNITY:", data);

    const requestData = data.data || data;
    const playerId    = await resolvePlayerId(context, requestData);

    if (!playerId) {
        console.error("createMayaCheckout: could not resolve playerId.");
        return { error: "Not authenticated." };
    }

    const itemId   = requestData.itemId   || "unknown_item";
    const itemName = requestData.itemName || "Unknown Item";

    const rawAmount = storePrices[itemId];
    if (!rawAmount) {
        console.error("Invalid itemId:", itemId, "from player:", playerId);
        return { error: "Invalid item. Transaction cancelled." };
    }

    await cleanupStalePendingPayments(playerId);

    if (itemId === "premium_plan") {
        const userDoc = await admin.firestore().collection("users").doc(playerId).get();
        if (userDoc.exists) {
            const purchases = userDoc.data().purchases || {};
            if (purchases["premium_plan"] === true) {
                console.warn("Player already has premium_plan:", playerId);
                return { error: "You already have an active Premium subscription." };
            }
        }
    }

    const numericAmount = parseFloat(Number(rawAmount).toFixed(2));
    const mayaUrl       = "https://pg-sandbox.paymaya.com/checkout/v1/checkouts";
    const base64Key     = Buffer.from(MAYA_PUBLIC_KEY + ":").toString("base64");
    const orderId       = "ORDER_" + Date.now();

    const payload = {
        totalAmount: { value: numericAmount, currency: "PHP" },
        items: [{
            name:        itemName,
            quantity:    1,
            amount:      { value: numericAmount },
            totalAmount: { value: numericAmount }
        }],
        requestReferenceNumber: orderId,
        redirectUrl: {
            success: "https://www.google.com",
            failure: "https://www.google.com",
            cancel:  "https://www.google.com"
        }
    };

    try {
        await admin.firestore().collection("payments").doc(orderId).set({
            playerId:  playerId,
            itemId:    itemId,
            itemName:  itemName,
            status:    "PENDING",
            amount:    numericAmount,
            createdAt: admin.firestore.FieldValue.serverTimestamp()
        });

        const response = await fetch(mayaUrl, {
            method:  "POST",
            headers: {
                "Authorization": `Basic ${base64Key}`,
                "Content-Type":  "application/json"
            },
            body: JSON.stringify(payload)
        });

        const textResponse = await response.text();
        let responseData;

        try {
            responseData = JSON.parse(textResponse);
        } catch (parseError) {
            console.error("CRITICAL: Maya returned non-JSON:", textResponse);
            await admin.firestore().collection("payments").doc(orderId).update({ status: "FAILED" });
            return { error: "Maya API sent invalid data." };
        }

        if (!response.ok) {
            console.error("Maya rejected the payload:", responseData);
            await admin.firestore().collection("payments").doc(orderId).update({ status: "FAILED" });
            return { error: responseData };
        }

        console.log("Maya full response:", JSON.stringify(responseData));

        await admin.firestore().collection("payments").doc(orderId)
              .update({ checkoutId: responseData.checkoutId });

        return { checkoutUrl: responseData.redirectUrl, orderId: orderId };

    } catch (error) {
        console.error("Server crash details:", error);
        throw new functions.https.HttpsError("internal", "Failed to contact Maya.");
    }
});

// ─────────────────────────────────────────────────────────────────────────────
// 2. MAYA WEBHOOK
// ─────────────────────────────────────────────────────────────────────────────
exports.mayaWebhook = functions.https.onRequest(async (req, res) => {
    const paymentData = req.body;

    try {
        const orderId    = paymentData.requestReferenceNumber;
        const mayaStatus = paymentData.status;

        if (!orderId) {
            console.warn("Webhook received with no orderId.");
            return res.status(200).send("OK");
        }

        const db         = admin.firestore();
        const paymentRef = db.collection("payments").doc(orderId);
        const paymentDoc = await paymentRef.get();

        if (!paymentDoc.exists) {
            console.warn("Webhook for unknown orderId:", orderId);
            return res.status(200).send("OK");
        }

        const currentStatus = paymentDoc.data().status;
        if (currentStatus === "PAID") {
            console.log("Webhook ignored — order already PAID:", orderId);
            return res.status(200).send("OK");
        }

        const playerId = paymentDoc.data().playerId;
        const itemId   = paymentDoc.data().itemId;

        if (mayaStatus === "PAYMENT_SUCCESS") {
            await paymentRef.update({ status: "PAID", paidAt: admin.firestore.FieldValue.serverTimestamp() });

            await db.collection("users").doc(playerId).set({
                purchases: { [itemId]: true }
            }, { merge: true });

            if (itemId === "premium_plan") {
                await db.collection("users").doc(playerId).set({ isPremium: true }, { merge: true });
            }

            const label = await getPlayerLabel(playerId);
            console.log(`✅ Webhook PAID — ${itemId} unlocked for ${label}`);

        } else if (mayaStatus === "PAYMENT_FAILED" || mayaStatus === "PAYMENT_CANCELLED") {
            const newStatus = mayaStatus === "PAYMENT_FAILED" ? "FAILED" : "CANCELLED";
            await paymentRef.update({ status: newStatus });
            const label = await getPlayerLabel(playerId);
            console.log(`❌ Payment ${newStatus} for order ${orderId} — player ${label}`);
        }

        res.status(200).send("OK");

    } catch (error) {
        console.error("Webhook Error:", error);
        res.status(200).send("OK");
    }
});

// ─────────────────────────────────────────────────────────────────────────────
// 3. SCHEDULED CLEANUP
// ─────────────────────────────────────────────────────────────────────────────
exports.scheduledPaymentCleanup = onSchedule("every 60 minutes", async () => {
    const db     = admin.firestore();
    const cutoff = new Date(Date.now() - PENDING_EXPIRY_MS);

    const staleDocs = await db.collection("payments")
        .where("status",    "==", "PENDING")
        .where("createdAt", "<",  cutoff)
        .get();

    if (staleDocs.empty) {
        console.log("Scheduled cleanup: no stale PENDING orders found.");
        return null;
    }

    const batch = db.batch();
    staleDocs.forEach(doc => batch.update(doc.ref, { status: "EXPIRED" }));
    await batch.commit();

    console.log(`Scheduled cleanup: expired ${staleDocs.size} stale PENDING order(s).`);
    return null;
});

// ─────────────────────────────────────────────────────────────────────────────
// DEBUG ENDPOINT — REMOVE BEFORE GOING TO PRODUCTION
// https://<region>-<project>.cloudfunctions.net/debugMayaCheckout?orderId=ORDER_xxx
// ─────────────────────────────────────────────────────────────────────────────
exports.debugMayaCheckout = functions.https.onRequest(async (req, res) => {
    const orderId = req.query.orderId;
    if (!orderId) return res.status(400).json({ error: "Pass ?orderId=ORDER_xxx" });

    const db         = admin.firestore();
    const paymentDoc = await db.collection("payments").doc(orderId).get();
    if (!paymentDoc.exists) return res.status(404).json({ error: "Order not found in Firestore" });

    const checkoutId = paymentDoc.data().checkoutId;
    if (!checkoutId) return res.status(400).json({ error: "No checkoutId saved for this order" });

    const playerId  = paymentDoc.data().playerId;
    const base64Key = Buffer.from(MAYA_SECRET_KEY + ":").toString("base64");

    // ── Resolve display name for clearer debug output ─────────────────────────
    const playerLabel = await getPlayerLabel(playerId);

    // Raw checkout object
    const checkoutResp = await fetch(
        `https://pg-sandbox.paymaya.com/checkout/v1/checkouts/${checkoutId}`,
        { headers: { "Authorization": `Basic ${base64Key}` } }
    );
    const checkoutData = await checkoutResp.json();

    // Sub-endpoint Maya uses for the actual payment record
    const paymentsResp = await fetch(
        `https://pg-sandbox.paymaya.com/checkout/v1/checkouts/${checkoutId}/payment`,
        { headers: { "Authorization": `Basic ${base64Key}` } }
    );
    const paymentsData = await paymentsResp.json();

    res.json({
        // ── Player identity ───────────────────────────────────────────────────
        player: {
            uid:   playerId,
            label: playerLabel      // e.g. "John Doe (yt5ISrS9ueeHJzI4OYSq4RgY7V23)"
        },
        firestoreDoc: paymentDoc.data(),
        checkoutData: checkoutData,
        paymentsData: paymentsData
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// 4. VERIFY PAYMENT
// ─────────────────────────────────────────────────────────────────────────────
exports.verifyMayaPayment = functions.https.onCall(async (data, context) => {
    const requestData = data.data || data;

    const playerId = await resolvePlayerId(context, requestData);
    if (!playerId) {
        console.error("verifyMayaPayment: not authenticated.");
        return { error: "Not authenticated." };
    }

    const orderId = requestData.orderId;
    if (!orderId) return { error: "Missing orderId." };

    const db         = admin.firestore();
    const paymentRef = db.collection("payments").doc(orderId);
    const paymentDoc = await paymentRef.get();

    if (!paymentDoc.exists) return { error: "Order not found." };
    if (paymentDoc.data().playerId !== playerId) return { error: "Unauthorized." };

    // Already confirmed by webhook — return immediately
    const currentStatus = paymentDoc.data().status;
    if (currentStatus === "PAID")      return { status: "PAID" };
    if (currentStatus === "FAILED")    return { status: "FAILED" };
    if (currentStatus === "CANCELLED") return { status: "CANCELLED" };

    const checkoutId = paymentDoc.data().checkoutId;
    if (!checkoutId) return { error: "No checkoutId on record." };

    const base64Secret = Buffer.from(MAYA_SECRET_KEY + ":").toString("base64");

    try {
        const checkoutResp = await fetch(
            `https://pg-sandbox.paymaya.com/checkout/v1/checkouts/${checkoutId}`,
            { headers: { "Authorization": `Basic ${base64Secret}` } }
        );
        const checkoutJson = await checkoutResp.json();
        console.log("MAYA CHECKOUT RAW:", JSON.stringify(checkoutJson));

        const topStatus     = checkoutJson.paymentStatus || checkoutJson.status || "";
        const paymentObj    = checkoutJson.payment || checkoutJson.payments?.[0] || null;
        const paymentStatus = paymentObj?.status || paymentObj?.paymentStatus || "";

        console.log(`topStatus="${topStatus}" paymentStatus="${paymentStatus}"`);

        const resolvedStatus = paymentStatus || topStatus;

        if (resolvedStatus === "PAYMENT_SUCCESS") {
            await _markPaid(db, paymentRef, paymentDoc, playerId);
            return { status: "PAID" };
        }

        if (resolvedStatus === "PAYMENT_FAILED") {
            await paymentRef.update({ status: "FAILED" });
            return { status: "FAILED" };
        }

        // ── Fallback: try the /payment sub-endpoint ───────────────────────────
        const subResp = await fetch(
            `https://pg-sandbox.paymaya.com/checkout/v1/checkouts/${checkoutId}/payment`,
            { headers: { "Authorization": `Basic ${base64Secret}` } }
        );
        const subJson = await subResp.json();
        console.log("MAYA PAYMENT SUB-ENDPOINT RAW:", JSON.stringify(subJson));

        const subStatus = subJson.status || subJson.paymentStatus || "";

        if (subStatus === "PAYMENT_SUCCESS") {
            await _markPaid(db, paymentRef, paymentDoc, playerId);
            return { status: "PAID" };
        }

        if (subStatus === "PAYMENT_FAILED") {
            await paymentRef.update({ status: "FAILED" });
            return { status: "FAILED" };
        }

        return { status: resolvedStatus || subStatus || "PENDING" };

    } catch (error) {
        console.error("verifyMayaPayment error:", error);
        return { error: "Could not reach Maya." };
    }
});

// ── Shared helper: mark an order PAID and unlock the item ────────────────────
async function _markPaid(db, paymentRef, paymentDoc, playerId) {
    const itemId = paymentDoc.data().itemId;
    await paymentRef.update({
        status: "PAID",
        paidAt: admin.firestore.FieldValue.serverTimestamp()
    });
    await db.collection("users").doc(playerId).set({
        purchases: { [itemId]: true }
    }, { merge: true });
    if (itemId === "premium_plan") {
        await db.collection("users").doc(playerId).set({ isPremium: true }, { merge: true });
    }
    const label = await getPlayerLabel(playerId);
    console.log(`✅ PAID confirmed — ${itemId} unlocked for ${label}`);
}