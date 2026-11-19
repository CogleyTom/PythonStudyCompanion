let receiver;
let registration;
let observedWorker;
let statusWorker;
let controlled = false;
let readiness = { offlineReady: false, development: false };
let lastError = null;
let activationTimer;
let reloading = false;
let checking = false;
let disposed = true;

async function workerStatus(worker) {
    return new Promise(resolve => {
        const channel = new MessageChannel();
        const timer = setTimeout(() => {
            channel.port1.close();
            resolve({ offlineReady: false, development: false });
        }, 3000);
        channel.port1.onmessage = event => {
            clearTimeout(timer);
            channel.port1.close();
            resolve(event.data);
        };
        worker.postMessage({ type: "GET_PWA_STATUS" }, [channel.port2]);
    });
}

async function report() {
    if (disposed || !receiver) return;
    const controller = navigator.serviceWorker?.controller;
    const worker = controller ?? registration?.active;
    if (worker && worker !== statusWorker) {
        const status = await workerStatus(worker);
        if (disposed || (navigator.serviceWorker.controller ?? registration?.active) !== worker) return;
        statusWorker = worker;
        readiness = status;
    }
    if (disposed) return;
    await receiver.invokeMethodAsync("NotifyStatus",
        navigator.onLine, !!controller && readiness.offlineReady,
        readiness.development, !!registration?.waiting && !!controller, lastError);
}

function reportSafely() {
    void report().catch(() => {});
}

function updateFailed(message) {
    lastError = message;
    void receiver?.invokeMethodAsync("UpdateFailed", message).catch(() => {});
}

function installationChanged() {
    if (observedWorker?.state === "redundant" && !navigator.serviceWorker.controller) {
        lastError = "Offline setup didn't finish. Reconnect and check for updates to retry.";
    }
    setTimeout(reportSafely, 0);
}

function watchInstallation() {
    observedWorker?.removeEventListener("statechange", installationChanged);
    observedWorker = registration?.installing;
    observedWorker?.addEventListener("statechange", installationChanged);
    reportSafely();
}

async function controllerChanged() {
    clearTimeout(activationTimer);
    if (controlled && !reloading) {
        reloading = true;
        try {
            await receiver.invokeMethodAsync("PrepareForReload");
            window.location.reload();
        } catch {
            reloading = false;
            updateFailed("The app couldn't finish updating. Close its open tabs and reopen it.");
        }
    } else {
        controlled = true;
        reportSafely();
    }
}

function connectionChanged() {
    reportSafely();
    if (navigator.onLine) void checkForUpdates(true);
}

function visibilityChanged() {
    if (document.visibilityState === "visible" && navigator.onLine) {
        void checkForUpdates(true);
    }
}

async function register() {
    const workerUrl = new URL("service-worker.js", document.baseURI);
    registration?.removeEventListener("updatefound", watchInstallation);
    registration = await navigator.serviceWorker.getRegistration(document.baseURI);
    if (!registration || navigator.onLine) {
        registration = await navigator.serviceWorker.register(workerUrl, { updateViaCache: "none" });
    }
    if (disposed) return;
    registration.addEventListener("updatefound", watchInstallation);
    watchInstallation();
}

export async function initialize(dotNetReference) {
    receiver = dotNetReference;
    disposed = false;
    controlled = !!navigator.serviceWorker?.controller;
    statusWorker = null;
    reloading = false;
    lastError = null;
    if (!("serviceWorker" in navigator)) {
        await receiver.invokeMethodAsync("NotifyStatus", navigator.onLine, false, false, false,
            "This browser doesn't support offline installation. You can still study online.");
        return;
    }
    navigator.serviceWorker.addEventListener("controllerchange", controllerChanged);
    window.addEventListener("online", connectionChanged);
    window.addEventListener("offline", connectionChanged);
    document.addEventListener("visibilitychange", visibilityChanged);
    try {
        await register();
        await report();
    } catch {
        lastError = "Offline setup is unavailable. Reconnect and check for updates to retry.";
        await report();
    }
}

export async function checkForUpdates(suppressErrors = false) {
    if (checking || disposed || !("serviceWorker" in navigator)) return;
    if (!navigator.onLine) {
        reportSafely();
        return;
    }
    checking = true;
    try {
        lastError = null;
        if (!registration || (!registration.active && !registration.installing)) {
            await register();
        } else {
            await registration.update();
        }
    } catch {
        if (!suppressErrors) lastError = "Couldn't check for updates. Try again when your connection is available.";
    } finally {
        checking = false;
        await report().catch(() => {});
    }
}

export function applyUpdate() {
    const waiting = registration?.waiting;
    if (!waiting) return false;
    lastError = null;
    activationTimer = setTimeout(() => {
        updateFailed("The update hasn't activated. Close all app tabs and reopen to finish.");
    }, 20000);
    waiting.postMessage({ type: "ACTIVATE_UPDATE" });
    return true;
}

export function dispose() {
    disposed = true;
    clearTimeout(activationTimer);
    observedWorker?.removeEventListener("statechange", installationChanged);
    registration?.removeEventListener("updatefound", watchInstallation);
    navigator.serviceWorker?.removeEventListener("controllerchange", controllerChanged);
    window.removeEventListener("online", connectionChanged);
    window.removeEventListener("offline", connectionChanged);
    document.removeEventListener("visibilitychange", visibilityChanged);
    receiver = null;
}
