const tokens = /(?<comment>#[^\r\n]*)|(?<string>(?:[rRuUbBfF]{1,2})?(?:"""[\s\S]*?"""|'''[\s\S]*?'''|"(?:\\[\s\S]|[^"\\\r\n])*"|'(?:\\[\s\S]|[^'\\\r\n])*'))|(?<number>\b(?:0[xX][\da-fA-F]+|\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\b)|(?<keyword>\b(?:False|None|True|and|as|assert|async|await|break|class|continue|def|del|elif|else|except|finally|for|from|global|if|import|in|is|lambda|nonlocal|not|or|pass|raise|return|try|while|with|yield)\b)|(?<builtin>\b(?:all|any|bool|dict|enumerate|float|int|len|list|max|min|print|range|reversed|set|sorted|str|sum|tuple|zip|ValueError)\b)/g;
const handlers = new WeakMap();

export function enhance(code, button, status) {
    const source = code.textContent;
    const fragment = document.createDocumentFragment();
    let position = 0;
    for (const match of source.matchAll(tokens)) {
        fragment.append(document.createTextNode(source.slice(position, match.index)));
        const span = document.createElement("span");
        span.className = `python-${Object.keys(match.groups).find(key => match.groups[key] !== undefined)}`;
        span.textContent = match[0];
        fragment.append(span);
        position = match.index + match[0].length;
    }
    fragment.append(document.createTextNode(source.slice(position)));
    code.replaceChildren(fragment);

    const previous = handlers.get(button);
    if (previous) button.removeEventListener("click", previous);
    // Bind directly so Safari's clipboard request stays inside the user's tap gesture.
    const copy = async () => {
        try {
            await navigator.clipboard.writeText(source);
            status.textContent = "Code copied, including indentation.";
        } catch {
            const selection = window.getSelection();
            const range = document.createRange();
            range.selectNodeContents(code);
            selection?.removeAllRanges();
            selection?.addRange(range);
            status.textContent = "Clipboard access was blocked. The code is selected; use your browser's Copy action.";
        }
    };
    handlers.set(button, copy);
    button.addEventListener("click", copy);
}
