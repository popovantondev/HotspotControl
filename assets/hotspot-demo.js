/* Demo window of Hotspot Control for the guide: an HTML replica of the program's preview window
 * (main, devices, settings, details). Page memory only: no network, no storage. No JS = the screenshot stays. */
(function () {
  'use strict';
  var root = document.querySelector('[data-hotspot-demo]');
  if (!root) return;
  var fallback = document.querySelector('.hsd-static');
  var lang = (document.documentElement.lang || 'en').slice(0, 2);

  var T = window.HSD_STRINGS; /* assets/hotspot-demo/strings-<lang>.js, loaded before this file */
  if (!T) return;
  var motion = window.matchMedia ? window.matchMedia('(prefers-reduced-motion: reduce)') : null;
  function delay() { return motion && motion.matches ? 0 : 550; }

  var LANG_NAMES = ['Deutsch', 'Русский', 'English'];
  var LANG_INDEX = { de: 0, ru: 1, en: 2 };
  var DEVICES = [
    { number: 1, names: ['demo-phone', '192.0.2.10'], mac: '02:00:00:00:00:10' },
    { number: 2, names: ['demo-laptop', '192.0.2.11'], mac: '02:00:00:00:00:11' }
  ];
  
  var state = {
    on: true, busy: null, screen: 'main', msg: 'Ready', note: null, tab: 'simple',
    ssid: 'Demo-WLAN', band: 0, startWindows: false, autoStart: false, feedback: null, opener: null
  };
  var timer = 0;

  /* ---------- tiny DOM helper ---------- */
  function el(tag, cls, text, attrs) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    if (attrs) for (var k in attrs) if (attrs.hasOwnProperty(k)) n.setAttribute(k, attrs[k]);
    return n;
  }
  function add(parent) {
    for (var i = 1; i < arguments.length; i++) if (arguments[i]) parent.appendChild(arguments[i]);
    return parent;
  }
  function button(cls, text, id, onClick) {
    var b = el('button', cls, text, { type: 'button', 'data-id': id });
    b.addEventListener('click', onClick);
    return b;
  }
  function svg(markup) {
    var d = document.createElement('div');
    d.innerHTML = markup;
    return d.firstChild;
  }

  /* ---------- illustration: the program's Scene.xaml, redrawn as SVG ---------- */
  var SCENE =
    '<svg class="hsd-scene-svg" viewBox="0 0 600 470" focusable="false" aria-hidden="true">' +
    '<defs>' +
    '<linearGradient id="hsd-signal" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#2eeadc"/><stop offset=".55" stop-color="#2cc6d7"/><stop offset="1" stop-color="#5b9be8"/></linearGradient>' +
    '<linearGradient id="hsd-laptop" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#aac2d8"/><stop offset="1" stop-color="#7890b2"/></linearGradient>' +
    '</defs>' +
    '<path fill="#1e455c" d="M25,130 C20,112 34,98 53,101 C50,65 99,60 110,86 C123,40 187,54 187,94 C211,89 224,111 218,130 Z M414,70 C407,52 424,39 441,45 C444,9 496,10 503,43 C522,20 551,34 554,56 C577,54 586,65 583,78 Z"/>' +
    '<path fill="none" stroke="#f1e6b8" stroke-width="3" stroke-linecap="round" d="M54,41h1M223,25h1M344,43h1M540,135h1M63,236h1M488,226h1M155,181h1M394,107h1"/>' +
    '<g transform="translate(300 110) scale(.78) translate(-300 -110)"><g class="hsd-signal">' +
    '<path class="hsd-wave" fill="none" stroke-width="40" stroke-linecap="round" d="M121,183 Q300,9 479,183 M191,251 Q300,141 409,251"/>' +
    '<circle class="hsd-wave-dot" cx="300" cy="311" r="31"/></g></g>' +
    '<ellipse cx="300" cy="439" rx="141" ry="12" fill="#0b3448"/>' +
    '<g transform="translate(300 440) scale(1.35) translate(-300 -440)" stroke-linecap="round">' +
    '<path fill="#111B38" d="M265,365 L264,319 C261,268 334,267 337,318 L337,366 Z"/>' +
    '<path fill="#EDAE99" d="M280,318 Q300,290 321,318 L320,341 Q300,365 282,341 Z"/>' +
    '<path fill="#18223F" d="M273,319 Q270,289 302,290 Q335,291 328,324 Q316,319 309,306 Q289,320 273,319"/>' +
    '<path fill="#D1EBE5" d="M277,350 Q260,353 245,391 L264,410 L339,408 L355,391 Q338,353 322,350 Q301,374 277,350"/>' +
    '<path fill="none" stroke="#EDAE99" stroke-width="10" d="M255,377 L243,397 L272,412 M345,377 L356,397 L331,412"/>' +
    '<path fill="#353357" stroke="#181F3D" stroke-width="3" d="M300,415 C242,391 216,407 237,426 Q266,450 333,437 L352,426 C381,401 342,394 300,415 Z"/>' +
    '<path fill="none" stroke="#EDAE99" stroke-width="9" d="M240,436 L255,432 M345,435 L359,439"/>' +
    '<path fill="none" stroke="#FFF" stroke-width="2" d="M290,326h4M307,326h4M297,339 Q302,342 307,338"/>' +
    '<path fill="url(#hsd-laptop)" stroke="#A8D4E4" stroke-width="2" d="M257,372 L344,372 L336,415 L264,415 Z"/>' +
    '<path fill="none" stroke="#C8E3EB" stroke-width="4" d="M260,417h82"/>' +
    '<path fill="#D7D9BB" d="M394,405 L417,405 Q417,429 406,429 Q395,429 394,405"/>' +
    '<path fill="none" stroke="#D7D9BB" stroke-width="3" d="M417,409 C433,403 429,423 415,420"/>' +
    '<path fill="none" stroke="#78BBC7" stroke-width="4" opacity=".5" d="M404,396 C418,383 393,380 408,368"/>' +
    '</g></svg>';
  var POWER_ICON = '<svg viewBox="0 0 20 22" aria-hidden="true" focusable="false"><path d="M10,2 L10,11 M4.5,5.5 A8.4,8.4 0 1 0 15.5,5.5" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round"/></svg>';
  var APP_ICON = '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><rect width="24" height="24" rx="6" fill="#1b6f80"/><path d="M5,10 Q12,3.5 19,10 M8,13.4 Q12,9.8 16,13.4" fill="none" stroke="#9ff1e6" stroke-width="1.8" stroke-linecap="round"/><circle cx="12" cy="17" r="1.7" fill="#9ff1e6"/></svg>';

  /* ---------- shell ---------- */
  var win, body, backBtn, toggleBtn, closeBtn, minBtn, barBrand;
  function buildShell() {
    root.textContent = '';
    var head = add(el('div', 'hsd-head'), el('span', 'hsd-label', T.Demo));
    win = el('div', 'hsd-window', null, { role: 'group', 'aria-label': T.Window });
    var bar = el('div', 'hsd-bar');
    var icon = el('span', 'hsd-appicon'); icon.innerHTML = APP_ICON;
    barBrand = el('span', 'hsd-brand', 'HOTSPOT CONTROL / DEMO');
    var caps = el('span', 'hsd-caps');
    minBtn = button('hsd-cap', '', 'min', function () { state.note = T.PreviewHelp; announceNote(); });
    minBtn.setAttribute('aria-label', T.Minimize); minBtn.title = T.Minimize;
    minBtn.innerHTML = '<svg viewBox="0 0 12 12" aria-hidden="true"><path d="M0,6 L12,6" stroke="currentColor" stroke-width="1.5" fill="none"/></svg>';
    closeBtn = button('hsd-cap', '', 'close', onCloseButton);
    closeBtn.setAttribute('aria-label', T.Close); closeBtn.title = T.Close;
    closeBtn.innerHTML = '<svg viewBox="0 0 12 12" aria-hidden="true"><path d="M1,1 L11,11 M11,1 L1,11" stroke="currentColor" stroke-width="1.5" fill="none"/></svg>';
    add(caps, minBtn, closeBtn);
    add(bar, icon, barBrand, caps);
    body = el('div', 'hsd-body');
    add(win, bar, body);
    var tools = el('div', 'hsd-tools', null, { role: 'group', 'aria-label': T.Tools });
    backBtn = button('hsd-tool', '← ' + T.Back, 'back', goMain);
    backBtn.hidden = true;
    toggleBtn = button('hsd-tool hsd-tool-main', '', 'toggle', toggle);
    add(tools, backBtn, toggleBtn);
    var help = el('p', 'hsd-help', T.PreviewHelp);
    add(root, head, win, tools, help);
  }

  /* ---------- actions ---------- */
  function setBusy(kind) { state.busy = kind; }
  function later(fn) {
    clearTimeout(timer);
    var ms = delay();
    if (ms === 0) fn(); else timer = setTimeout(fn, ms);
  }
  function toggle() {
    if (state.busy) return;
    state.msg = 'Ready'; state.note = null;
    setBusy(state.on ? 'Stopping' : 'Starting');
    refreshView();
    later(function () {
      state.on = !state.on;
      state.msg = state.on ? 'Enabled' : 'Disabled';
      setBusy(null);
      refreshView();
    });
  }
  function refresh() {
    if (state.busy) return;
    state.msg = 'Ready'; state.note = null;
    setBusy('Reading');
    refreshView();
    later(function () { setBusy(null); refreshView(); });
  }
  function go(screen, opener) {
    if (state.busy && screen !== 'settings') return; /* devices and details wait for the running command */
    state.msg = 'Ready'; state.note = null; state.feedback = null;
    state.opener = opener || null;
    state.screen = screen;
    render(true);
  }
  function goMain() {
    if (state.screen === 'main') return;
    state.screen = 'main';
    render(true);
  }
  function onCloseButton() {
    if (state.screen === 'main') { state.note = T.PreviewHelp; announceNote(); } else goMain();
  }
  function announceNote() {
    var n = body.querySelector('.hsd-note');
    if (n) n.textContent = state.note || T[state.msg];
  }

  /* ---------- screens ---------- */
  function statusKey() {
    if (state.busy) return state.busy;
    return state.on ? 'On' : 'Off';
  }
  function powerLabel() { return state.on ? T.PowerOff : T.PowerOn; }

  function mainScreen() {
    var s = el('div', 'hsd-screen hsd-main');
    var title = el('div', 'hsd-title', 'Hotspot Control', { role: 'heading', 'aria-level': '3', tabindex: '-1' });
    var top = button('hsd-power hsd-power-top', '', 'power-top', toggle);
    top.innerHTML = POWER_ICON; top.setAttribute('aria-label', powerLabel()); top.title = powerLabel();
    var head = add(el('div', 'hsd-headrow'), add(el('div', 'hsd-headtext'), title, el('div', 'hsd-tagline', T.Tagline)), top);

    var box = el('div', 'hsd-scene-box');
    box.innerHTML = SCENE;
    var power = button('hsd-power hsd-power-laptop', '', 'power', toggle);
    power.innerHTML = POWER_ICON; power.setAttribute('aria-label', powerLabel()); power.title = powerLabel();
    add(box, power);
    var scene = add(el('div', 'hsd-scene'), box);

    var status = el('div', 'hsd-status' + (state.on && !state.busy ? '' : ' is-off'), T[statusKey()]);
    var count = el('div', 'hsd-count', (state.on ? 2 : 0) + ' / 8');
    var inline = button('hsd-secondary hsd-details-inline', T.Details + '  ↗', 'details-inline', function () { go('message', 'details-inline'); });
    var row = add(el('div', 'hsd-row'), el('span', 'hsd-muted', T.ConnectedDevices), add(el('span', 'hsd-right'), count, inline));
    var note = el('div', 'hsd-note', state.note || T[state.msg], { role: 'status', 'aria-live': 'polite' });
    var details = button('hsd-secondary hsd-details', T.Details + '  ↗', 'details', function () { go('message', 'details'); });
    var card = add(el('div', 'hsd-card'), el('div', 'hsd-caps-label', T.MobileHotspot), status, row, el('div', 'hsd-rule'), note, details);

    var actions = el('div', 'hsd-actions');
    var bRefresh = button('hsd-primary', T.Refresh, 'refresh', refresh);
    var bDevices = button('hsd-secondary', T.Devices, 'devices', function () { go('devices', 'devices'); });
    var bSettings = button('hsd-secondary', T.Settings, 'settings', function () { go('settings', 'settings'); });
    add(actions, bRefresh, bDevices, bSettings);
    if (state.busy) { /* like the program: no new commands while one runs (focus stays put, so aria-disabled) */
      [bRefresh, bDevices, power, top].forEach(function (b) { b.setAttribute('aria-disabled', 'true'); b.classList.add('is-disabled'); });
    }
    s.setAttribute('data-on', state.on ? '1' : '0');
    return add(s, head, scene, card, actions, el('div', 'hsd-foot', T.Demo));
  }

  function header(label, title) {
    return add(el('div', 'hsd-childhead'), el('div', 'hsd-caps-label', label), el('div', 'hsd-title hsd-title-child', title, { role: 'heading', 'aria-level': '3', tabindex: '-1' }));
  }

  function devicesScreen() {
    var s = el('div', 'hsd-screen hsd-devices');
    var list = el('div', 'hsd-list');
    function fill() {
      list.textContent = '';
      if (!state.on) {
        add(list, add(el('div', 'hsd-card hsd-empty'), el('div', 'hsd-empty-mark', '◌', { 'aria-hidden': 'true' }), el('div', 'hsd-empty-title', T.NoDevices), el('div', 'hsd-muted', T.NoDevicesHelp)));
        return;
      }
      DEVICES.forEach(function (d) {
        add(list, add(el('div', 'hsd-card hsd-device'),
          el('div', 'hsd-device-title', T.Device + ' ' + (d.number < 10 ? '0' : '') + d.number),
          el('div', 'hsd-device-names', d.names.join('\n')),
          el('div', 'hsd-mac', 'MAC  ' + d.mac)));
      });
    }
    fill();
    var again = button('hsd-secondary hsd-wide', T.Refresh, 'devices-refresh', function () {
      again.disabled = true;
      later(function () { fill(); again.disabled = false; });
    });
    var foot = add(el('div', 'hsd-childfoot'), again, el('div', 'hsd-muted hsd-small', T.Privacy));
    return add(s, header(T.Network, T.ConnectedDevices), list, foot);
  }

  function settingsScreen() {
    var s = el('div', 'hsd-screen hsd-settings');
    var feedback = el('div', 'hsd-feedback', '', { role: 'status', 'aria-live': 'polite' });
    feedback.hidden = true;
    function say(text, kind) {
      state.feedback = { text: text, kind: kind };
      feedback.textContent = text; feedback.hidden = false;
      feedback.className = 'hsd-feedback is-' + (kind || 'ok');
    }
    /* tabs */
    var tSimple = button('hsd-tab', T.Simple, 'tab-simple', function () { selectTab('simple'); });
    var tAdv = button('hsd-tab', T.Advanced, 'tab-advanced', function () { selectTab('advanced'); });
    tSimple.setAttribute('aria-pressed', 'true'); tAdv.setAttribute('aria-pressed', 'false');
    var tabs = add(el('div', 'hsd-tabs'), tSimple, tAdv);
    var nameId = 'hsd-ssid', pwId = 'hsd-pw', bandId = 'hsd-band', langId = 'hsd-lang';
    var ssid = el('input', 'hsd-input', null, { id: nameId, type: 'text', maxlength: '32', autocomplete: 'off', spellcheck: 'false' });
    ssid.value = state.ssid;
    var pw = el('input', 'hsd-input', null, { id: pwId, type: 'password', maxlength: '63', autocomplete: 'new-password' });
    var simple = add(el('div', 'hsd-pane'),
      el('label', 'hsd-field', T.NetworkName, { for: nameId }), ssid,
      el('label', 'hsd-field', T.NewPassword, { for: pwId }), pw,
      el('div', 'hsd-muted hsd-small', T.PasswordHelp));
    var band = el('select', 'hsd-input hsd-select', null, { id: bandId });
    [[0, T.Automatic], [1, '2,4 GHz'], [2, '5 GHz']].forEach(function (o) {
      var op = el('option', null, o[1], { value: String(o[0]) });
      if (o[0] === state.band) op.selected = true;
      band.appendChild(op);
    });
    var adv = add(el('div', 'hsd-pane'), el('label', 'hsd-field', T.Frequency, { for: bandId }), band, el('div', 'hsd-muted hsd-small', T.BandHelp));
    function selectTab(which) {
      state.tab = which;
      simple.hidden = which !== 'simple'; adv.hidden = which !== 'advanced';
      tSimple.setAttribute('aria-pressed', which === 'simple' ? 'true' : 'false');
      tAdv.setAttribute('aria-pressed', which === 'advanced' ? 'true' : 'false');
      tSimple.title = which === 'simple' ? T.Selected : ''; tAdv.title = which === 'advanced' ? T.Selected : '';
    }
    var saveBtn = button('hsd-primary hsd-wide', T.SaveNetwork, 'save', function () {
      if (state.busy) return;
      var name = ssid.value, pass = pw.value, code = null;
      if (name.length < 1 || name.length > 32 || name.charAt(0) === ' ' || name.charAt(name.length - 1) === ' ' || /[^\x20-\x7e]/.test(name)) code = 'InvalidSsid';
      else if (pass.length && (pass.length < 8 || pass.length > 63 || /[^\x20-\x7e]/.test(pass))) code = 'InvalidPassword';
      else if (state.on) code = 'MustTurnOff';
      if (code) { say(T[code], 'warn'); return; }
      setBusy('Saving'); saveBtn.disabled = true; say(T.Saving, 'info');
      later(function () {
        state.ssid = name; state.band = parseInt(band.value, 10) || 0; pw.value = '';
        setBusy(null); saveBtn.disabled = false;
        say(T.SettingsSaved, 'ok');
      });
    });
    var network = add(el('div', 'hsd-card'), el('div', 'hsd-caps-label', T.Network), tabs, simple, adv,
      el('div', 'hsd-muted hsd-small hsd-gap', T.NetworkHelp), saveBtn, feedback);
    selectTab(state.tab);

    function check(text, key, id) {
      var input = el('input', 'hsd-check-input', null, { type: 'checkbox', id: id });
      input.checked = !!state[key];
      input.addEventListener('change', function () { state[key] = input.checked; say(T.MemorySaved, 'ok'); });
      var box = el('span', 'hsd-check-box', null, { 'aria-hidden': 'true' });
      var label = el('label', 'hsd-check', null, { for: id });
      add(label, input, box, el('span', 'hsd-check-text', text));
      return label;
    }
    var langSel = el('select', 'hsd-input hsd-select', null, { id: langId });
    LANG_NAMES.forEach(function (n, i) { var op = el('option', null, n, { value: String(i) }); if (i === LANG_INDEX[lang]) op.selected = true; langSel.appendChild(op); });
    langSel.addEventListener('change', function () { say(T.LanguageSaved, 'ok'); });
    var app = add(el('div', 'hsd-card hsd-gap-top'), el('div', 'hsd-caps-label', T.Application),
      check(T.StartWindows, 'startWindows', 'hsd-c1'), check(T.StartHotspot, 'autoStart', 'hsd-c2'),
      el('div', 'hsd-muted hsd-small hsd-gap', T.ImmediateHelp),
      el('label', 'hsd-field', T.Language, { for: langId }), langSel);
    if (state.feedback) say(state.feedback.text, state.feedback.kind);
    return add(s, header(T.Application, T.Settings), network, app);
  }

  function messageScreen() {
    var s = el('div', 'hsd-screen hsd-message');
    var close = button('hsd-secondary hsd-wide', T.Close, 'message-close', goMain);
    return add(s, header(T.MobileHotspot, T.Message), add(el('div', 'hsd-card hsd-message-card'), el('div', 'hsd-message-text', T[state.msg])), close);
  }

  /* ---------- render ---------- */
  function updateChrome() {
    var main = state.screen === 'main';
    backBtn.hidden = main;
    minBtn.hidden = !main;
    toggleBtn.textContent = powerLabel();
    toggleBtn.setAttribute('aria-disabled', state.busy ? 'true' : 'false');
    toggleBtn.classList.toggle('is-disabled', !!state.busy);
    win.setAttribute('data-screen', state.screen);
    win.classList.toggle('is-on', state.on);
  }
  function render(moveFocus) {
    var active = document.activeElement, keep = (!moveFocus && active && body.contains(active)) ? active.getAttribute('data-id') : null;
    body.textContent = '';
    var screens = { main: mainScreen, devices: devicesScreen, settings: settingsScreen, message: messageScreen };
    var node = screens[state.screen]();
    body.appendChild(node);
    body.scrollTop = 0;
    updateChrome();
    fit();
    if (moveFocus) {
      var target;
      if (state.screen === 'main' && state.opener) target = body.querySelector('[data-id="' + state.opener + '"]');
      if (!target) target = body.querySelector('.hsd-title');
      if (target) target.focus({ preventScroll: true });
    } else if (keep) {
      var again = body.querySelector('[data-id="' + keep + '"]');
      if (again && !again.disabled) again.focus({ preventScroll: true });
    }
  }
  /* typed text on the settings screen must survive a toggle from the demo toolbar */
  function refreshView() {
    if (state.screen === 'settings' || state.screen === 'message') { updateChrome(); return; }
    render(false);
  }

  /* the program switches to a compact layout below 650 px of window height */
  function fit() { if (win) win.classList.toggle('is-compact', win.clientHeight < 650); }

  buildShell();
  render(false);
  win.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && state.screen !== 'main') { e.preventDefault(); goMain(); }
  });
  if (window.ResizeObserver) new ResizeObserver(fit).observe(win); else window.addEventListener('resize', fit);
  root.hidden = false;
  if (fallback) fallback.hidden = true;
})();
