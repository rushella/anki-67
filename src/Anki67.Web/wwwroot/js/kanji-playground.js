// Kanji stroke data: KanjiVG r20250816, CC BY-SA 3.0.
// https://kanjivg.tagaini.net/

const svgNamespace = "http://www.w3.org/2000/svg";
const kanjiVgRevision = "r20250816";
const kanjiVgBaseUrl = `https://cdn.jsdelivr.net/gh/KanjiVG/kanjivg@${kanjiVgRevision}/kanji`;
const states = new Map();

export async function loadAndAnimate(previewElementId, value, options) {
    const character = getSingleKanji(value);
    const state = await loadState(character);
    const previous = states.get(previewElementId);

    if (previous?.animationUrl) {
        URL.revokeObjectURL(previous.animationUrl);
    }

    states.set(previewElementId, state);
    renderPreview(previewElementId, state);
    animatePreview(state, options);

    return {
        character: state.character,
        strokeCount: state.strokes.length
    };
}

export function replay(previewElementId, options) {
    const state = requireState(previewElementId);
    animatePreview(state, options);
}

export function generateAnimatedSvg(previewElementId, options) {
    const state = requireState(previewElementId);
    const strokeDurationMs = clampInteger(options?.strokeDurationMs, 240, 1200, 560);
    const pauseMs = clampInteger(options?.pauseMs, 0, 600, 120);
    const finalHoldMs = clampInteger(options?.finalHoldMs, 300, 3000, 1200);
    const source = buildAnimatedSvg(state, strokeDurationMs, pauseMs, finalHoldMs);
    const blob = new Blob([source], { type: "image/svg+xml;charset=utf-8" });

    if (state.animationUrl) {
        URL.revokeObjectURL(state.animationUrl);
    }

    state.animationUrl = URL.createObjectURL(blob);

    return {
        url: state.animationUrl,
        fileName: `kanjivg-${state.fileName}-animated.svg`,
        byteLength: blob.size
    };
}

export function dispose(previewElementId) {
    const state = states.get(previewElementId);
    if (!state) {
        return;
    }

    cancelAnimations(state);
    if (state.animationUrl) {
        URL.revokeObjectURL(state.animationUrl);
    }

    states.delete(previewElementId);
}

async function loadState(character) {
    const codePoint = character.codePointAt(0);
    const fileName = codePoint.toString(16).padStart(5, "0");
    const sourceUrl = `${kanjiVgBaseUrl}/${fileName}.svg`;
    const response = await fetch(sourceUrl);

    if (!response.ok) {
        throw new Error(`KanjiVG has no Japanese stroke data for ${character} (U+${codePoint.toString(16).toUpperCase()}).`);
    }

    const source = await response.text();
    const documentNode = new DOMParser().parseFromString(source, "image/svg+xml");

    if (documentNode.querySelector("parsererror")) {
        throw new Error("KanjiVG returned SVG data that this browser could not parse.");
    }

    const strokes = [...documentNode.querySelectorAll("path[id]")]
        .filter(path => /-s\d+$/.test(path.id))
        .map(path => path.getAttribute("d"))
        .filter(Boolean);

    if (strokes.length === 0) {
        throw new Error(`No ordered Japanese strokes were found for ${character}.`);
    }

    return {
        character,
        codePoint,
        fileName,
        sourceUrl,
        strokes,
        previewPaths: [],
        animations: [],
        animationUrl: null
    };
}

function renderPreview(previewElementId, state) {
    const container = document.getElementById(previewElementId);
    if (!container) {
        throw new Error("The stroke-order preview element is missing.");
    }

    container.replaceChildren();
    const svg = createSvgRoot();
    svg.setAttribute("lang", "ja");
    svg.setAttribute("aria-hidden", "true");
    appendBackground(svg);
    appendGrid(svg);

    const guideGroup = createSvgElement("g", {
        fill: "none",
        stroke: "#d5dde8",
        "stroke-width": "3",
        "stroke-linecap": "round",
        "stroke-linejoin": "round"
    });
    const inkGroup = createSvgElement("g", {
        fill: "none",
        stroke: "#1e293b",
        "stroke-width": "4.8",
        "stroke-linecap": "round",
        "stroke-linejoin": "round"
    });

    for (const pathData of state.strokes) {
        guideGroup.append(createSvgElement("path", { d: pathData }));
        inkGroup.append(createSvgElement("path", { d: pathData }));
    }

    svg.append(guideGroup, inkGroup);
    container.append(svg);
    state.previewPaths = [...inkGroup.querySelectorAll("path")];
}

function animatePreview(state, options) {
    cancelAnimations(state);
    const duration = clampInteger(options?.strokeDurationMs, 240, 1200, 560);
    const pause = clampInteger(options?.pauseMs, 0, 600, 120);
    state.animations = [];

    state.previewPaths.forEach((path, index) => {
        const length = Math.max(1, path.getTotalLength());
        path.style.strokeDasharray = `${length}`;
        path.style.strokeDashoffset = `${length}`;

        const animation = path.animate(
            [
                { strokeDashoffset: length, stroke: "#dc2626" },
                { offset: 0.88, strokeDashoffset: 0, stroke: "#dc2626" },
                { strokeDashoffset: 0, stroke: "#1e293b" }
            ],
            {
                duration,
                delay: index * (duration + pause),
                easing: "ease-in-out",
                fill: "both"
            });

        state.animations.push(animation);
    });
}

function cancelAnimations(state) {
    for (const animation of state.animations ?? []) {
        animation.cancel();
    }
    state.animations = [];
}

function buildAnimatedSvg(state, strokeDurationMs, pauseMs, finalHoldMs) {
    const introMs = 300;
    const totalDurationMs = introMs
        + state.strokes.length * strokeDurationMs
        + Math.max(0, state.strokes.length - 1) * pauseMs
        + finalHoldMs;
    const svg = createSvgRoot();
    svg.setAttribute("width", "512");
    svg.setAttribute("height", "512");
    svg.setAttribute("lang", "ja");
    svg.setAttribute("role", "img");
    svg.setAttribute("aria-labelledby", "kanji-title kanji-description");

    const title = createSvgElement("title", { id: "kanji-title" });
    title.textContent = `${state.character} Japanese stroke order`;
    const description = createSvgElement("desc", { id: "kanji-description" });
    description.textContent = `Looping ${state.strokes.length}-stroke animation for ${state.character}.`;
    const metadata = createSvgElement("metadata", {});
    metadata.textContent = `Stroke data: KanjiVG ${kanjiVgRevision}, CC BY-SA 3.0, https://kanjivg.tagaini.net/`;
    const definitions = createSvgElement("defs", {});
    state.strokes.forEach((pathData, index) => {
        definitions.append(createSvgElement("path", {
            id: `stroke-${index + 1}`,
            d: pathData
        }));
    });
    svg.append(title, description, metadata, definitions);

    appendBackground(svg);
    appendGrid(svg);

    const guideGroup = createSvgElement("g", {
        fill: "none",
        stroke: "#cbd5e1",
        "stroke-width": "3",
        "stroke-linecap": "round",
        "stroke-linejoin": "round"
    });
    const animatedGroup = createSvgElement("g", {
        fill: "none",
        stroke: "#1e293b",
        "stroke-width": "4.8",
        "stroke-linecap": "round",
        "stroke-linejoin": "round"
    });

    state.strokes.forEach((_, index) => {
        const strokeReference = `#stroke-${index + 1}`;
        guideGroup.append(createSvgElement("use", { href: strokeReference }));
        const path = createSvgElement("use", {
            href: strokeReference,
            stroke: "#dc2626"
        });
        const length = Math.max(1, state.previewPaths[index].getTotalLength());
        const startMs = introMs + index * (strokeDurationMs + pauseMs);
        const drawEndMs = startMs + strokeDurationMs * 0.88;
        const completeMs = startMs + strokeDurationMs;

        path.setAttribute("stroke-dasharray", `${length}`);
        path.setAttribute("stroke-dashoffset", `${length}`);
        path.append(
            createSvgElement("animate", {
                attributeName: "stroke-dashoffset",
                values: `${length};${length};0;0`,
                keyTimes: `0;${ratio(startMs, totalDurationMs)};${ratio(drawEndMs, totalDurationMs)};1`,
                keySplines: "0 0 1 1;0.42 0 0.58 1;0 0 1 1",
                calcMode: "spline",
                dur: `${totalDurationMs}ms`,
                repeatCount: "indefinite"
            }),
            createSvgElement("animate", {
                attributeName: "stroke",
                values: "#dc2626;#dc2626;#1e293b;#1e293b",
                keyTimes: `0;${ratio(drawEndMs, totalDurationMs)};${ratio(completeMs, totalDurationMs)};1`,
                dur: `${totalDurationMs}ms`,
                repeatCount: "indefinite"
            })
        );
        animatedGroup.append(path);
    });

    svg.append(guideGroup, animatedGroup);
    return `<?xml version="1.0" encoding="UTF-8"?>\n${new XMLSerializer().serializeToString(svg)}`;
}

function ratio(value, total) {
    return Math.min(1, Math.max(0, value / total)).toFixed(6);
}

function createSvgRoot() {
    return createSvgElement("svg", {
        xmlns: svgNamespace,
        viewBox: "0 0 109 109",
        preserveAspectRatio: "xMidYMid meet"
    });
}

function appendBackground(svg) {
    svg.append(createSvgElement("rect", {
        x: "0",
        y: "0",
        width: "109",
        height: "109",
        fill: "#ffffff"
    }));
}

function appendGrid(svg) {
    const grid = createSvgElement("g", {
        fill: "none",
        stroke: "#e2e8f0",
        "stroke-width": "0.7",
        "stroke-dasharray": "2.5 2.5"
    });
    grid.append(
        createSvgElement("line", { x1: "54.5", y1: "4", x2: "54.5", y2: "105" }),
        createSvgElement("line", { x1: "4", y1: "54.5", x2: "105", y2: "54.5" }),
        createSvgElement("line", { x1: "8", y1: "8", x2: "101", y2: "101" }),
        createSvgElement("line", { x1: "101", y1: "8", x2: "8", y2: "101" })
    );
    svg.append(grid);
}

function createSvgElement(name, attributes) {
    const element = document.createElementNS(svgNamespace, name);
    for (const [key, value] of Object.entries(attributes)) {
        element.setAttribute(key, value);
    }
    return element;
}

function getSingleKanji(value) {
    const characters = Array.from((value ?? "").trim());
    if (characters.length !== 1 || !/^\p{Script=Han}$/u.test(characters[0])) {
        throw new Error("Enter exactly one Japanese kanji character.");
    }
    return characters[0];
}

function requireState(previewElementId) {
    const state = states.get(previewElementId);
    if (!state) {
        throw new Error("Load a kanji before generating its animation.");
    }
    return state;
}

function clampInteger(value, minimum, maximum, fallback) {
    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? Math.min(maximum, Math.max(minimum, parsed)) : fallback;
}
