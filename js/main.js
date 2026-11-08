/* ZenForms showcase — vanilla JS, no dependencies */
(() => {
'use strict';
const $ = (s, r = document) => r.querySelector(s);
const $$ = (s, r = document) => [...r.querySelectorAll(s)];
const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
const C = { primary: '#7C5CFF', accent: '#2AD3BE', fg: '#EFF2FF', muted: '#97A1BD', border: '#37415B', raised: '#222A42', bg: '#0F1322', surface: '#191F33', warn: '#FFBE46', danger: '#FF5B75' };

/* ---------- Catalogue data ---------- */
const ICONS = {
  Inputs: '<rect x="3" y="7" width="18" height="10" rx="3"/><path d="M7 12h5"/>',
  Layout: '<rect x="3" y="3" width="18" height="18" rx="3"/><path d="M3 9h18M9 21V9"/>',
  Navigation: '<circle cx="12" cy="12" r="9"/><path d="M15.5 8.5l-2 5-5 2 2-5z"/>',
  Feedback: '<path d="M18 8a6 6 0 10-12 0c0 7-3 8-3 8h18s-3-1-3-8M13.7 20a2 2 0 01-3.4 0"/>',
  Data: '<path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>',
  'Media & 3D': '<path d="M12 2l9 5v10l-9 5-9-5V7zM12 22V12M21 7l-9 5-9-5"/>',
  Effects: '<path d="M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8zM19 17l.7 2 2 .7-2 .7-.7 2-.7-2-2-.7 2-.7z"/>',
  Developer: '<path d="M8 6l-6 6 6 6M16 6l6 6-6 6M14 4l-4 16"/>',
  Creative: '<path d="M12 3a9 9 0 100 18c1.7 0 2-1.2 1.4-2.2-.7-1.2 0-2.8 1.6-2.8H17a4 4 0 004-4c0-5-4-9-9-9zM7.5 11h.01M10 7.5h.01M14.5 7.5h.01"/>',
  Tools: '<path d="M14.7 6.3a4 4 0 005 5L21 17l-4 4-5.7-1.3a4 4 0 01-5-5L3 9l4-4z"/>'
};
const D = (name, cat, desc, tags = []) => ({ name, cat, desc, tags });
const COMPONENTS = [
  // Inputs
  D('ZenButtonBase', 'Inputs', 'A rounded, custom-painted button with smooth hover, pressed and focus states. Corner radius, border and every colour are configurable.', ['CornerRadius', 'HoverBackColor']),
  D('ZenRippleButton', 'Inputs', 'A standalone button with a smooth radial ripple that starts at the click point, a hover glow and full keyboard support.', ['RippleColor', 'HoverGlowColor']),
  D('ZenTextBox', 'Inputs', 'A single-line text field with an optional caption, native placeholder text, password mode and a focus highlight.', ['Placeholder', 'LabelText']),
  D('ZenMaskedTextBox', 'Inputs', 'A masked input for phone numbers, dates, postcodes and codes, restyled with the Zen dark palette.'),
  D('ZenRichTextBox', 'Inputs', 'A rich-text editing surface that follows the theme, so longer content stays easy on the eyes.'),
  D('ZenComboBox', 'Inputs', 'A drop-down selector with a themed field and list, exposing the familiar Items and DropDownStyle properties.'),
  D('ZenNumericUpDown', 'Inputs', 'A numeric spinner with a flat, dark field that sits naturally beside the other inputs.'),
  D('ZenDateTimePicker', 'Inputs', 'A date picker whose pop-up calendar is fully themed, with a violet title bar and muted trailing days.'),
  D('ZenCheckBox', 'Inputs', 'A flat check box with a violet checked state and subtle border.'),
  D('ZenRadioButton', 'Inputs', 'A themed radio button for mutually exclusive choices.'),
  D('ZenToggle', 'Inputs', 'An animated pill switch with a CheckedChanged event — ideal for settings and preferences.', ['Checked', 'Event']),
  D('ZenTrackBar', 'Inputs', 'A slim slider with a violet accent, ranging from 0 to 100 by default.'),
  D('ZenSpringSlider', 'Inputs', 'A numeric slider whose thumb eases toward its value with spring physics and a slight overshoot. Tune SpringStrength and Damping.', ['Spring physics', 'Step']),
  D('ZenTagEditor', 'Inputs', 'A chip-style tag input: press Enter to add a tag, Backspace to remove the last one. Supports duplicate prevention and a maximum count.', ['TagAdded', 'MaximumTags']),
  D('ZenShortcutRecorder', 'Inputs', 'A click-to-record keyboard shortcut field that captures key combinations including modifiers.', ['ShortcutKeys', 'ShortcutChanged']),
  D('ZenRating', 'Inputs', 'An interactive star rating control that responds to hover and click.', ['Interactive']),
  D('ZenListBox', 'Inputs', 'A dark list with generous row height for comfortable scanning.'),
  D('ZenCheckedListBox', 'Inputs', 'A list in which every row has a check box, themed to match the rest of the library.'),
  D('ZenLabel', 'Inputs', 'A transparent, auto-sizing label that always uses the theme’s foreground colour and font.'),
  D('ZenLinkLabel', 'Inputs', 'A turquoise hyperlink label with an underline on hover and a violet active state.'),
  // Layout
  D('ZenPanel', 'Layout', 'A rounded container with a hairline border and comfortable padding. Corner radius and border colour are adjustable.', ['CornerRadius', 'BorderColor']),
  D('ZenCard', 'Layout', 'An elevated, rounded card surface for grouping related content into tidy blocks.', ['CornerRadius']),
  D('ZenGroupBox', 'Layout', 'A group box with rounded corners and a semibold caption for organising related fields.'),
  D('ZenFlowLayoutPanel', 'Layout', 'A flow layout container that arranges children in a row or column and wraps them neatly.'),
  D('ZenTableLayoutPanel', 'Layout', 'A grid layout container with themed colours for precise, tabular arrangements.'),
  D('ZenSplitContainer', 'Layout', 'A resizable two-pane splitter with slim dividers and padded, themed panels.'),
  D('ZenTabControl', 'Layout', 'An owner-drawn tab control with fixed, evenly sized tabs and a clear active indicator.'),
  D('ZenAccordion', 'Layout', 'An expandable section with a header and a content area for child controls. Raises an ExpandedChanged event.', ['Expanded', 'ContentHeight']),
  D('ZenSeparator', 'Layout', 'A one-pixel divider line in the theme’s border colour.'),
  // Navigation
  D('ZenMenuStrip', 'Navigation', 'A menu bar with a custom renderer, hidden grip and the Zen surface colour.'),
  D('ZenContextMenuStrip', 'Navigation', 'A right-click menu that shares the same renderer and palette as the main menu.'),
  D('ZenToolStrip', 'Navigation', 'A tool bar with the custom renderer for crisp, modern icons and separators.'),
  D('ZenStatusStrip', 'Navigation', 'A status bar with muted text, perfect for progress messages and hints.'),
  D('ZenMenuRenderer', 'Navigation', 'The shared renderer that gives every menu, tool bar and status bar its distinctive look.', ['Helper']),
  D('ZenToolTip', 'Navigation', 'An owner-drawn tooltip with a raised surface, comfortable padding and quick reaction times.'),
  D('ZenBreadcrumb', 'Navigation', 'A neon breadcrumb bar that shows a path as clickable segments, with full keyboard navigation.', ['Segments', 'SegmentClicked']),
  D('ZenOrbitMenu', 'Navigation', 'An expandable radial menu whose commands orbit a central hub. Add items in code and react to ItemSelected.', ['AddItem', 'ItemSelected']),
  D('ZenCommandPalette', 'Navigation', 'A searchable, keyboard-driven command palette: filter as you type, then run the selected action with Enter or double-click.', ['AddCommand', 'Open()']),
  // Feedback
  D('ZenProgressBar', 'Feedback', 'A sleek progress bar from 0 to 100 with an optional percentage label.', ['Value', 'ShowPercentage']),
  D('ZenCircularProgress', 'Feedback', 'A circular progress indicator with the percentage displayed at its centre.'),
  D('ZenLoadingSpinner', 'Feedback', 'An animated circular loading indicator for operations of unknown length.', ['Animated']),
  D('ZenOrbitLoader', 'Feedback', 'A neon loader with satellites orbiting a glowing core. Configure the count, speed and direction, then call Start().', ['Animated', 'Start()']),
  D('ZenMorphingGauge', 'Feedback', 'A neon circular gauge that morphs smoothly from its current value to a target, with an optional target marker and pulsing glow.', ['StartAnimation', 'Animated']),
  D('ZenNotification', 'Feedback', 'A coloured inline message in four variants — information, success, warning and error — with a dismiss button.', ['Type', 'Dismissed']),
  D('ZenBadge', 'Feedback', 'A compact status pill in five styles: primary, success, warning, danger and neutral.', ['Style']),
  D('ZenAvatar', 'Feedback', 'A circular avatar that shows an image or initials, with an optional outline ring.', ['Image', 'Initials']),
  D('ZenLiquidGauge', 'Feedback', 'A circular gauge filled with gently animated liquid waves. Mesmerising and informative at once.', ['Animated']),
  // Data
  D('ZenDataGridView', 'Data', 'A data grid with flat headers, no row headers and subtle grid lines for a clean, readable table.'),
  D('ZenChart', 'Data', 'A modern chart control supporting line, spline, column, bar, area, pie, doughnut and stacked column types, with optional legend, grid and markers.', ['8 chart types', 'Two series']),
  D('ZenDataProfiler', 'Data', 'Profiles any DataTable: completeness, missing values, cardinality and numerical statistics at a glance.', ['DataTable']),
  D('ZenHeatmapCalendar', 'Data', 'An annual heat-map calendar for visualising activity, in the style of a contribution graph.'),
  D('ZenTimeline', 'Data', 'An interactive vertical timeline for events and milestones.', ['Interactive']),
  D('ZenKanbanBoard', 'Data', 'A kanban board whose cards can be dragged between columns, ready for task and workflow management.', ['Drag & drop']),
  D('ZenLogConsole', 'Data', 'An application console with colour-coded log levels, built-in search and a configurable history limit.', ['Search', 'History limit']),
  D('ZenServiceMonitor', 'Data', 'A scrollable status board for your services with Healthy, Warning, Error and Offline states, latency and messages. You run the checks; it displays them.', ['UpdateService', 'Latency']),
  D('ZenAgendaCalendar', 'Data', 'A monthly calendar with selectable days and coloured event markers, plus full keyboard navigation between days and months.', ['AddEvent', 'DateSelected']),
  D('ZenTreeView', 'Data', 'A hierarchical tree with a dark surface and theme-matched selection colours.'),
  // Media & 3D
  D('ZenMediaPlayer', 'Media & 3D', 'A compact MP3/MP4 player built on the Windows Media Player control, with open, play, pause, seek, volume, time display and full-screen.', ['MP3 · MP4', 'Windows Media Player']),
  D('ZenModelViewer', 'Media & 3D', 'A 3D model viewer with built-in readers for OBJ, STL, PLY and OFF files. Reports vertex and triangle counts.', ['OBJ · STL · PLY · OFF', 'Software rendered']),
  D('ZenGCodePreview', 'Media & 3D', 'A G-code viewer that previews the toolpath of a 3D print before you send it to the machine.'),
  D('ZenWaveformAnalyzer', 'Media & 3D', 'Reads WAV files in PCM or IEEE-float format and draws their waveform for quick inspection.', ['PCM', 'IEEE float']),
  D('ZenWaveformScroller', 'Media & 3D', 'A scrolling waveform strip with a simulated playhead and range selection, for mock-ups and editors where your own code handles the audio.', ['Playhead', 'Selection']),
  // Effects
  D('ZenHoloCard', 'Effects', 'A modern card with a light-and-depth animation that follows the mouse pointer.', ['Animated', 'Mouse-reactive']),
  D('ZenParticleField', 'Effects', 'An animated field of drifting particles with trails and connecting lines, ideal for backdrops.', ['Animated']),
  D('ZenNeuralConstellation', 'Effects', 'A procedural, animated neural web that reacts to the mouse — a striking splash-screen background.', ['Procedural', 'Mouse-reactive']),
  D('ZenCodeRain', 'Effects', 'Sci-fi, terminal-style glyph rain with subtle animation.', ['Animated']),
  D('ZenAnalogClock', 'Effects', 'A precision analogue clock with a glowing neon dial.', ['Animated']),
  D('ZenAuroraPanel', 'Effects', 'A rounded panel with an animated aurora of gradients and softly moving light blobs — a beautiful backdrop for any form.', ['Animated', 'Intensity']),
  D('ZenGlitchLabel', 'Effects', 'A neon label with a controlled RGB-split glitch and scanlines. Tune intensity, frequency and channel offset.', ['Animated', 'Scanlines']),
  D('ZenMatrixConsole', 'Effects', 'Matrix-style falling characters with a short message console on top. Call WriteLine to print status messages.', ['Animated', 'WriteLine']),
  D('ZenParticleEmitter', 'Effects', 'A particle canvas with continuous emission and click-triggered explosions, with optional connecting lines between particles.', ['Animated', 'EmitBurst']),
  D('ZenConfettiCelebration', 'Effects', 'A transparent overlay that fires a confetti burst with optional celebration text. Call Play() when something deserves a party.', ['Play()', 'Palette']),
  // Developer
  D('ZenApiWorkbench', 'Developer', 'A visual REST client: method selector, URL, headers, body, and a response viewer with status, timing and size. Send requests with SendAsync.', ['GET · POST · PUT', 'SendAsync']),
  D('ZenDataFlowStudio', 'Developer', 'A visual pipeline editor for a DataTable. Chain filter, sort, select-column and computed-column stages, preview each step and export to CSV.', ['DataTable', 'Pipeline']),
  D('ZenRegexStudio', 'Developer', 'An interactive regular-expression tester with live match list, replacement preview and a timeout guard against catastrophic patterns.', ['Live matches', 'Timeout']),
  D('ZenJsonExplorer', 'Developer', 'A dark neon tree viewer for JSON. Objects and arrays become expandable nodes with value and type information; invalid JSON never throws.', ['JsonText', 'NodeSelected']),
  D('ZenHexExplorer', 'Developer', 'A hexadecimal viewer and editor with offset, byte and ASCII columns, hex search, go-to-offset and safe saving. Opens files up to 64 MiB.', ['FindHex', 'Editable']),
  D('ZenCircuitLab', 'Developer', 'A digital-logic simulator: wire inputs, AND / OR / XOR / NAND / NOT gates and outputs, toggle inputs live and generate truth tables.', ['CreateTruthTable', 'Logic gates']),
  D('ZenTextEditor', 'Developer', 'A compact rich-text editor with a built-in toolbar: open and save RTF, TXT and basic DOCX, find and replace, formatting, colours and zoom.', ['RTF · TXT · DOCX', 'Find & replace']),
  // Creative
  D('ZenFractalExplorer', 'Creative', 'An interactive Mandelbrot and Julia set explorer. Zoom with the wheel, pan by dragging and tweak the iteration count.', ['Mandelbrot · Julia', 'Zoom']),
  D('ZenPixelForge', 'Creative', 'A small pixel-art editor with pen, eraser and fill tools, a colour palette, grid, undo/redo history and PNG import and export.', ['Undo / Redo', 'PNG']),
  D('ZenScreenCaptureStudio', 'Creative', 'A screenshot tool with simple editing: capture, crop, redact regions, copy to the clipboard and save as PNG or JPEG.', ['Capture', 'Redact']),
  D('ZenVisionAccessibility', 'Creative', 'Checks how a design reads under colour-vision deficiencies and measures the contrast ratio between any two pixels of an image.', ['Colour-blind sim', 'Contrast']),
  // Tools
  D('ZenPomodoroClock', 'Tools', 'A visual focus and break timer with configurable work, short-break and long-break lengths; a long break follows every fourth session.', ['Work · Break', 'PhaseChanged']),
  D('ZenColorStudio', 'Tools', 'An HSV colour studio with HEX codes and instantly generated harmonic palettes.', ['HSV', 'HEX', 'Palettes']),
  D('ZenFileDropZone', 'Tools', 'A drag-and-drop zone that lists dropped files and raises events you can handle in code.', ['Drag & drop']),
  D('ZenWorkflowCanvas', 'Tools', 'An interactive canvas for prototyping flows: add draggable nodes, connect them and react to selection.', ['AddNode', 'Connect']),
  D('ZenFormulaPlotter', 'Tools', 'A mathematical plotter that draws functions from formulas you type.'),
  D('ZenDiffViewer', 'Tools', 'A side-by-side text comparison table that highlights what has changed.'),
  D('ZenMarkdownView', 'Tools', 'A viewer that renders Markdown files in the Zen style.')
];
const CATS = ['All', ...Object.keys(ICONS)];
const total = COMPONENTS.length;

/* ---------- Counters ---------- */
$$('[data-count-total]').forEach(e => e.textContent = total);
const statEl = $('.stat b[data-total]');
function countUp(el, to) {
  if (reduce) { el.textContent = to; return; }
  const t0 = performance.now();
  (function f(t) { const p = Math.min(1, (t - t0) / 1400); el.textContent = Math.round(to * (1 - Math.pow(1 - p, 3))); if (p < 1) requestAnimationFrame(f); })(t0);
}

/* ---------- Catalogue rendering ---------- */
const catalog = $('#catalog'), filters = $('#filters'), empty = $('#empty'), q = $('#q');
let activeCat = 'All';
filters.innerHTML = CATS.map(c => `<button data-c="${c}" class="${c === 'All' ? 'on' : ''}">${c}<sup>${c === 'All' ? total : COMPONENTS.filter(x => x.cat === c).length}</sup></button>`).join('');
function renderCatalog() {
  const term = q.value.trim().toLowerCase();
  const list = COMPONENTS.filter(c => (activeCat === 'All' || c.cat === activeCat) && (!term || (c.name + ' ' + c.desc + ' ' + c.cat + ' ' + c.tags.join(' ')).toLowerCase().includes(term)));
  catalog.innerHTML = list.map((c, i) => `
    <article class="comp" style="animation-delay:${Math.min(i, 14) * 25}ms">
      <div class="top"><div class="ci"><svg viewBox="0 0 24 24">${ICONS[c.cat]}</svg></div><div><div class="cat">${c.cat}</div><h3>${c.name}</h3></div></div>
      <p>${c.desc}</p>
      ${c.tags.length ? `<div class="tags">${c.tags.map((t, k) => `<span class="${k === 0 ? 'hot' : ''}">${t}</span>`).join('')}</div>` : ''}
    </article>`).join('');
  empty.hidden = list.length > 0;
}
filters.addEventListener('click', e => {
  const b = e.target.closest('button'); if (!b) return;
  activeCat = b.dataset.c; $$('button', filters).forEach(x => x.classList.toggle('on', x === b)); renderCatalog();
});
q.addEventListener('input', renderCatalog);
catalog.addEventListener('pointermove', e => {
  const c = e.target.closest('.comp'); if (!c) return;
  const r = c.getBoundingClientRect(); c.style.setProperty('--mx', (e.clientX - r.left) + 'px'); c.style.setProperty('--my', (e.clientY - r.top) + 'px');
});
renderCatalog();

/* ---------- Palette swatches ---------- */
const SW = [['Background', C.bg], ['Surface', C.surface], ['SurfaceRaised', C.raised], ['Primary', C.primary], ['Accent', C.accent], ['Foreground', C.fg], ['Muted', C.muted], ['Border', C.border], ['Danger', C.danger], ['Warning', C.warn]];
$('#swatches').innerHTML = SW.map(([n, h]) => `<button class="sw" data-h="${h}" title="Copy ${h}"><div class="c" style="background:${h}"></div><div><b>${n}</b><span>${h}</span></div></button>`).join('');
const toastEl = $('#toast'); let tt;
function toast(msg) { toastEl.textContent = msg; toastEl.classList.add('show'); clearTimeout(tt); tt = setTimeout(() => toastEl.classList.remove('show'), 1800); }
function copy(text, msg) {
  const done = () => toast(msg);
  if (navigator.clipboard && window.isSecureContext) navigator.clipboard.writeText(text).then(done, fallback); else fallback();
  function fallback() { const a = document.createElement('textarea'); a.value = text; a.style.position = 'fixed'; a.style.opacity = 0; document.body.appendChild(a); a.select(); try { document.execCommand('copy'); } catch (e) {} a.remove(); done(); }
}
$('#swatches').addEventListener('click', e => { const b = e.target.closest('.sw'); if (b) copy(b.dataset.h, 'Copied ' + b.dataset.h); });
$('#copyCode').addEventListener('click', () => copy($('#codeBlock').innerText, 'Code copied to clipboard'));

/* ---------- Nav & reveal ---------- */
const nav = $('#nav');
addEventListener('scroll', () => nav.classList.toggle('scrolled', scrollY > 30), { passive: true });
$('#yr').textContent = new Date().getFullYear();
const io = new IntersectionObserver(es => es.forEach(en => {
  if (!en.isIntersecting) return;
  en.target.classList.add('in'); io.unobserve(en.target);
  if (en.target.contains(statEl)) countUp(statEl, total);
}), { threshold: .12 });
$$('.reveal').forEach((el, i) => { el.style.setProperty('--d', (el.closest('.hero') ? i * 0.08 : (i % 4) * 0.06) + 's'); io.observe(el); });

/* ---------- Hero: neural constellation (canvas) ---------- */
(function () {
  const cv = $('#constellation'), ctx = cv.getContext('2d'); let W, H, pts = [], mouse = { x: -999, y: -999 }, dpr = Math.min(devicePixelRatio || 1, 2);
  function size() {
    const r = cv.getBoundingClientRect(); W = r.width; H = r.height; cv.width = W * dpr; cv.height = H * dpr; ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    const n = Math.round(Math.min(110, W * H / 12000)); pts = Array.from({ length: n }, () => ({ x: Math.random() * W, y: Math.random() * H, vx: (Math.random() - .5) * .35, vy: (Math.random() - .5) * .35, r: Math.random() * 1.6 + .8 }));
  }
  addEventListener('resize', size); size();
  cv.parentElement.addEventListener('pointermove', e => { const r = cv.getBoundingClientRect(); mouse.x = e.clientX - r.left; mouse.y = e.clientY - r.top; });
  cv.parentElement.addEventListener('pointerleave', () => { mouse.x = mouse.y = -999; });
  let vis = true; new IntersectionObserver(e => vis = e[0].isIntersecting).observe(cv);
  function frame() {
    requestAnimationFrame(frame); if (!vis) return;
    ctx.clearRect(0, 0, W, H);
    for (const p of pts) {
      p.x += p.vx; p.y += p.vy; if (p.x < 0 || p.x > W) p.vx *= -1; if (p.y < 0 || p.y > H) p.vy *= -1;
      const dx = p.x - mouse.x, dy = p.y - mouse.y, d = Math.hypot(dx, dy);
      if (d < 140 && d > 0) { p.x += dx / d * 1.1; p.y += dy / d * 1.1; }
    }
    for (let i = 0; i < pts.length; i++) {
      const a = pts[i];
      for (let j = i + 1; j < pts.length; j++) {
        const b = pts[j], d = Math.hypot(a.x - b.x, a.y - b.y);
        if (d < 130) { ctx.strokeStyle = `rgba(124,92,255,${(1 - d / 130) * .45})`; ctx.lineWidth = 1; ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.stroke(); }
      }
      const dm = Math.hypot(a.x - mouse.x, a.y - mouse.y);
      if (dm < 190) { ctx.strokeStyle = `rgba(42,211,190,${(1 - dm / 190) * .7})`; ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(mouse.x, mouse.y); ctx.stroke(); }
      ctx.fillStyle = dm < 190 ? C.accent : '#a995ff'; ctx.beginPath(); ctx.arc(a.x, a.y, a.r, 0, 7); ctx.fill();
    }
  }
  if (!reduce) frame(); else { frame(); }
})();

/* ---------- Hero window: tilt, chart, ring, bar ---------- */
(function () {
  const w = $('#heroWindow');
  if (!reduce && matchMedia('(hover:hover)').matches) {
    w.parentElement.addEventListener('pointermove', e => {
      const r = w.getBoundingClientRect(), x = (e.clientX - r.left) / r.width - .5, y = (e.clientY - r.top) / r.height - .5;
      w.style.transform = `perspective(1200px) rotateY(${x * 5}deg) rotateX(${-y * 4}deg)`;
    });
    w.parentElement.addEventListener('pointerleave', () => w.style.transform = '');
  }
  const data = [30, 42, 36, 58, 50, 72, 64, 88, 76, 95];
  const W = 300, H = 110, step = W / (data.length - 1);
  const pts = data.map((v, i) => [i * step, H - 8 - v / 100 * (H - 16)]);
  const line = pts.map((p, i) => (i ? 'L' : 'M') + p[0].toFixed(1) + ' ' + p[1].toFixed(1)).join('');
  $('#heroChart').innerHTML = `<defs><linearGradient id="hg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${C.primary}" stop-opacity=".45"/><stop offset="1" stop-color="${C.primary}" stop-opacity="0"/></linearGradient></defs><path d="${line}L${W} ${H}L0 ${H}Z" fill="url(#hg)"/><path d="${line}" fill="none" stroke="${C.primary}" stroke-width="2.5" stroke-linejoin="round" stroke-linecap="round"/>${pts.map(p => `<circle cx="${p[0]}" cy="${p[1]}" r="3" fill="${C.bg}" stroke="${C.accent}" stroke-width="2"/>`).join('')}`;
  setRing($('#heroRing'), 72);
  setTimeout(() => $('#heroBar').style.width = '68%', 500);
})();
function setRing(el, pct) {
  el.querySelector('.val').style.strokeDashoffset = 251.3 * (1 - pct / 100);
  el.querySelector('b').textContent = Math.round(pct) + '%';
}

/* ---------- Notifications ---------- */
const notifs = $('#notifs');
notifs.addEventListener('click', e => { if (e.target.tagName === 'EM') e.target.closest('.note').classList.add('gone'); });
$('#resetNotifs').addEventListener('click', () => $$('.note', notifs).forEach((n, i) => setTimeout(() => n.classList.remove('gone'), i * 90)));

/* ---------- Liquid gauge ---------- */
(function () {
  const cv = $('#gauge'), ctx = cv.getContext('2d'), range = $('#gaugeRange'); const S = 220; let level = 64, shown = 0, t = 0, vis = true;
  new IntersectionObserver(e => vis = e[0].isIntersecting).observe(cv);
  function paintRange() { range.style.setProperty('--p', range.value + '%'); }
  range.addEventListener('input', () => { level = +range.value; paintRange(); }); paintRange();
  const dpr = Math.min(devicePixelRatio || 1, 2); cv.width = cv.height = S * dpr; ctx.scale(dpr, dpr);
  function draw() {
    requestAnimationFrame(draw); if (!vis) return;
    t += .045; shown += (level - shown) * .06;
    ctx.clearRect(0, 0, S, S); const c = S / 2, r = c - 10;
    ctx.save(); ctx.beginPath(); ctx.arc(c, c, r, 0, 7); ctx.fillStyle = C.bg; ctx.fill(); ctx.clip();
    const y = S - 10 - (S - 20) * shown / 100;
    for (let k = 0; k < 2; k++) {
      ctx.beginPath(); ctx.moveTo(0, S);
      for (let x = 0; x <= S; x += 4) ctx.lineTo(x, y + Math.sin(x / 28 + t * (k ? -1.3 : 1) + k * 2) * (k ? 5 : 7));
      ctx.lineTo(S, S); ctx.closePath();
      const g = ctx.createLinearGradient(0, y, 0, S); g.addColorStop(0, k ? 'rgba(42,211,190,.65)' : 'rgba(124,92,255,.9)'); g.addColorStop(1, k ? 'rgba(42,211,190,.25)' : 'rgba(80,50,200,.9)');
      ctx.fillStyle = g; ctx.fill();
    }
    ctx.restore();
    ctx.lineWidth = 5; ctx.strokeStyle = C.border; ctx.beginPath(); ctx.arc(c, c, r, 0, 7); ctx.stroke();
    ctx.fillStyle = C.fg; ctx.font = '700 40px "Segoe UI",system-ui,sans-serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle'; ctx.shadowColor = 'rgba(0,0,0,.6)'; ctx.shadowBlur = 10;
    ctx.fillText(Math.round(shown) + '%', c, c); ctx.shadowBlur = 0;
  }
  draw();
})();

/* ---------- Holo card ---------- */
(function () {
  const h = $('#holo');
  h.addEventListener('pointermove', e => {
    const r = h.getBoundingClientRect(), x = (e.clientX - r.left) / r.width, y = (e.clientY - r.top) / r.height;
    h.style.setProperty('--mx', x * 100 + '%'); h.style.setProperty('--my', y * 100 + '%');
    h.style.setProperty('--ry', ((x - .5) * 18).toFixed(1) + 'deg'); h.style.setProperty('--rx', ((.5 - y) * 18).toFixed(1) + 'deg');
  });
  h.addEventListener('pointerleave', () => { h.style.setProperty('--rx', '0deg'); h.style.setProperty('--ry', '0deg'); });
})();

/* ---------- Chart ---------- */
(function () {
  const svg = $('#chart'), W = 420, H = 200, L = 30, B = 24, T = 10, R = 8;
  const labs = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul'], s1 = [32, 48, 41, 66, 58, 80, 72], s2 = [20, 29, 38, 35, 49, 52, 63];
  const x = i => L + i * (W - L - R) / (labs.length - 1), y = v => H - B - v / 100 * (H - B - T);
  const grid = () => [0, 25, 50, 75, 100].map(v => `<line class="gl" x1="${L}" x2="${W - R}" y1="${y(v)}" y2="${y(v)}"/><text x="${L - 6}" y="${y(v) + 3}" text-anchor="end">${v}</text>`).join('') + labs.map((l, i) => `<text x="${x(i)}" y="${H - 6}" text-anchor="middle">${l}</text>`).join('');
  const smooth = s => s.map((v, i) => { if (!i) return `M${x(i)} ${y(v)}`; const px = x(i - 1), py = y(s[i - 1]), cx = (px + x(i)) / 2; return `C${cx} ${py} ${cx} ${y(v)} ${x(i)} ${y(v)}`; }).join('');
  const defs = `<defs><linearGradient id="ga" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${C.primary}" stop-opacity=".5"/><stop offset="1" stop-color="${C.primary}" stop-opacity="0"/></linearGradient><linearGradient id="gb" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${C.accent}" stop-opacity=".35"/><stop offset="1" stop-color="${C.accent}" stop-opacity="0"/></linearGradient></defs>`;
  const types = {
    line: () => `<path class="draw" d="${smooth(s1)}" fill="none" stroke="${C.primary}" stroke-width="3" stroke-linecap="round"/><path class="draw" d="${smooth(s2)}" fill="none" stroke="${C.accent}" stroke-width="3" stroke-linecap="round"/>` + s1.map((v, i) => `<circle class="fade" cx="${x(i)}" cy="${y(v)}" r="4" fill="${C.bg}" stroke="${C.primary}" stroke-width="2"/>`).join('') + s2.map((v, i) => `<circle class="fade" cx="${x(i)}" cy="${y(v)}" r="4" fill="${C.bg}" stroke="${C.accent}" stroke-width="2"/>`).join(''),
    column: () => labs.map((_, i) => { const bw = 15; return `<rect class="grow" style="animation-delay:${i * 60}ms" x="${x(i) - bw - 1}" y="${y(s1[i])}" width="${bw}" height="${H - B - y(s1[i])}" rx="4" fill="${C.primary}"/><rect class="grow" style="animation-delay:${i * 60 + 30}ms" x="${x(i) + 1}" y="${y(s2[i])}" width="${bw}" height="${H - B - y(s2[i])}" rx="4" fill="${C.accent}"/>`; }).join(''),
    area: () => `<path class="fade" d="${smooth(s1)}L${x(6)} ${y(0)}L${x(0)} ${y(0)}Z" fill="url(#ga)"/><path class="draw" d="${smooth(s1)}" fill="none" stroke="${C.primary}" stroke-width="3"/><path class="fade" d="${smooth(s2)}L${x(6)} ${y(0)}L${x(0)} ${y(0)}Z" fill="url(#gb)"/><path class="draw" d="${smooth(s2)}" fill="none" stroke="${C.accent}" stroke-width="3"/>`,
    donut: () => {
      const vals = [38, 27, 20, 15], cols = [C.primary, C.accent, C.warn, C.danger], names = ['Desktop', 'Web', 'Mobile', 'Other'], cx = 130, cy = 100, r = 66, circ = 2 * Math.PI * r; let off = 0;
      const arcs = vals.map((v, i) => { const len = v / 100 * circ, a = `<circle class="draw" style="stroke-dasharray:${len - 3} ${circ};stroke-dashoffset:${-off};animation:none" cx="${cx}" cy="${cy}" r="${r}" fill="none" stroke="${cols[i]}" stroke-width="26" transform="rotate(-90 ${cx} ${cy})"/>`; off += len; return a; }).join('');
      return `<g class="fade">${arcs}<text x="${cx}" y="${cy + 6}" text-anchor="middle" style="font-size:20px;font-weight:700;fill:${C.fg}">2,481</text></g>` + vals.map((v, i) => `<g class="fade"><rect x="250" y="${54 + i * 26}" width="12" height="12" rx="3" fill="${cols[i]}"/><text x="272" y="${64 + i * 26}" style="font-size:12px;fill:${C.fg}">${names[i]} · ${v}%</text></g>`).join('');
    }
  };
  function render(t) { svg.innerHTML = defs + (t === 'donut' ? '' : grid()) + types[t](); }
  $('#chartTabs').addEventListener('click', e => { const b = e.target.closest('button'); if (!b) return; $$('#chartTabs button').forEach(x => x.classList.toggle('on', x === b)); render(b.dataset.t); });
  render('line');
})();

/* ---------- Orbit menu ---------- */
(function () {
  const el = $('#orbit'), hint = $('#orbitHint'), items = ['New', 'Open', 'Save', 'Export', 'Share', 'Print'];
  const R = 80, c = 110;
  el.innerHTML = items.map((n, i) => { const a = -Math.PI / 2 + i * 2 * Math.PI / items.length; return `<button class="it" style="--x:${c + Math.cos(a) * R}px;--y:${c + Math.sin(a) * R}px">${n}</button>`; }).join('') + '<button class="core" aria-label="Toggle menu">Orbit</button>';
  el.addEventListener('click', e => {
    const it = e.target.closest('.it'), core = e.target.closest('.core');
    if (it) { $$('.it', el).forEach(x => x.classList.toggle('sel', x === it)); hint.innerHTML = `ItemSelected → <b>${it.textContent}</b>`; }
    else if (core) { el.classList.toggle('closed'); hint.textContent = el.classList.contains('closed') ? 'Collapsed — click the hub to expand.' : 'Click an item.'; }
  });
})();

/* ---------- Progress, ring, stars ---------- */
(function () {
  const bar = $('#pbar'), ring = $('#pring'); let p = 0, dir = 1, vis = false;
  new IntersectionObserver(e => vis = e[0].isIntersecting).observe(bar);
  setInterval(() => { if (!vis || reduce) return; p += dir * 12; if (p >= 100) { p = 100; dir = -1; } if (p <= 0) { p = 0; dir = 1; } bar.style.width = p + '%'; setRing(ring, p); }, 700);
  if (reduce) { bar.style.width = '70%'; setRing(ring, 70); }
  const st = $('#stars'); let val = 4;
  st.innerHTML = [1, 2, 3, 4, 5].map(n => `<button role="radio" aria-label="${n} star${n > 1 ? 's' : ''}" data-n="${n}">★</button>`).join('');
  const paint = n => $$('button', st).forEach((b, i) => b.classList.toggle('on', i < n));
  st.addEventListener('click', e => { const b = e.target.closest('button'); if (b) { val = +b.dataset.n; paint(val); } });
  st.addEventListener('pointerover', e => { const b = e.target.closest('button'); if (b) paint(+b.dataset.n); });
  st.addEventListener('pointerleave', () => paint(val)); paint(val);
})();

/* ---------- Heatmap calendar ---------- */
(function () {
  const h = $('#heat'); let s = 7; const rnd = () => (s = (s * 16807) % 2147483647) / 2147483647;
  const cols = innerWidth < 720 ? 26 : 40; h.style.gridTemplateRows = 'repeat(7,1fr)';
  let out = '';
  for (let i = 0; i < cols * 7; i++) {
    const wave = .5 + .5 * Math.sin(i / 13), v = rnd() * wave + (rnd() > .85 ? .3 : 0), l = v < .22 ? 0 : v < .42 ? 1 : v < .62 ? 2 : v < .82 ? 3 : 4;
    out += `<i class="l${l}" title="${l * 3 + Math.floor(rnd() * 3)} contributions"></i>`;
  }
  h.innerHTML = out;
})();

/* ---------- Command palette ---------- */
(function () {
  const cmds = [['Open project', 'Ctrl+O'], ['Save all', 'Ctrl+Shift+S'], ['Toggle theme', 'Ctrl+T'], ['Export as PDF', 'Ctrl+E'], ['Find in files', 'Ctrl+F'], ['Open settings', 'Ctrl+,'], ['Run build', 'F5'], ['Show log console', 'Ctrl+L']];
  const input = $('#pq'), list = $('#pl'); let sel = 0, cur = [];
  const hi = (s, q) => { if (!q) return s; const i = s.toLowerCase().indexOf(q.toLowerCase()); return i < 0 ? s : s.slice(0, i) + '<mark>' + s.slice(i, i + q.length) + '</mark>' + s.slice(i + q.length); };
  function render() {
    const q = input.value.trim().toLowerCase();
    cur = cmds.filter(c => !q || c[0].toLowerCase().includes(q) || c[1].toLowerCase().includes(q)).sort((a, b) => (a[0].toLowerCase().indexOf(q) + 1 || 99) - (b[0].toLowerCase().indexOf(q) + 1 || 99) || a[0].localeCompare(b[0]));
    sel = Math.min(sel, Math.max(0, cur.length - 1));
    list.innerHTML = cur.length ? cur.map((c, i) => `<li class="${i === sel ? 'on' : ''}" data-i="${i}"><span>${hi(c[0], q)}</span><kbd>${c[1]}</kbd></li>`).join('') : '<li>No matching commands</li>';
  }
  function run(i) { if (cur[i]) toast('CommandExecuted → ' + cur[i][0]); }
  input.addEventListener('input', () => { sel = 0; render(); });
  input.addEventListener('keydown', e => {
    if (e.key === 'ArrowDown') { sel = Math.min(cur.length - 1, sel + 1); render(); e.preventDefault(); }
    else if (e.key === 'ArrowUp') { sel = Math.max(0, sel - 1); render(); e.preventDefault(); }
    else if (e.key === 'Enter') run(sel);
  });
  list.addEventListener('click', e => { const li = e.target.closest('li[data-i]'); if (li) run(+li.dataset.i); });
  render();
})();

/* ---------- Code rain ---------- */
(function () {
  const cv = $('#rain'), ctx = cv.getContext('2d'); const chars = 'ZENFORMS01<>{}[]();=+*/アイウエオカキクケコ'.split(''); let drops = [], W, vis = true, fs = 16;
  const dpr = Math.min(devicePixelRatio || 1, 2);
  new IntersectionObserver(e => vis = e[0].isIntersecting).observe(cv);
  function size() { W = cv.clientWidth; cv.width = W * dpr; cv.height = 170 * dpr; ctx.setTransform(dpr, 0, 0, dpr, 0, 0); drops = Array.from({ length: Math.ceil(W / fs) }, () => Math.random() * -4); }
  addEventListener('resize', size); size(); let last = 0;
  function f(t) {
    requestAnimationFrame(f); if (!vis || t - last < 55) return; last = t;
    ctx.fillStyle = 'rgba(8,11,22,.16)'; ctx.fillRect(0, 0, W, 170); ctx.font = fs + 'px Consolas,monospace';
    drops.forEach((d, i) => {
      ctx.fillStyle = Math.random() > .93 ? '#fff' : (i % 5 ? C.accent : C.primary);
      ctx.fillText(chars[Math.floor(Math.random() * chars.length)], i * fs, d * fs);
      if (d * fs > 170 && Math.random() > .96) drops[i] = 0; else drops[i] += 1;
    });
  }
  requestAnimationFrame(f);
})();
})();
