const storageKey = "python-study-companion.progress.v1";

export function read() {
    return localStorage.getItem(storageKey);
}

export function write(expected, value) {
    if (localStorage.getItem(storageKey) !== expected) {
        return false;
    }

    localStorage.setItem(storageKey, value);
    return true;
}

export function download(json, filename) {
    const url = URL.createObjectURL(new Blob([json], { type: "application/json" }));
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}
