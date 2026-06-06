const tokenKey = "pocketmc.remote.deviceToken";
const instanceKey = "pocketmc.remote.instanceId";

const els = {
  connectionLabel: document.querySelector("#connectionLabel"),
  refreshButton: document.querySelector("#refreshButton"),
  notice: document.querySelector("#notice"),
  pairView: document.querySelector("#pairView"),
  pairTitle: document.querySelector("#pairTitle"),
  pairMessage: document.querySelector("#pairMessage"),
  pairButton: document.querySelector("#pairButton"),
  copyPairLinkButton: document.querySelector("#copyPairLinkButton"),
  appView: document.querySelector("#appView"),
  emptyView: document.querySelector("#emptyView"),
  emptyRefreshButton: document.querySelector("#emptyRefreshButton"),
  errorView: document.querySelector("#errorView"),
  errorMessage: document.querySelector("#errorMessage"),
  retryButton: document.querySelector("#retryButton"),
  clearTokenButton: document.querySelector("#clearTokenButton"),
  instanceSelect: document.querySelector("#instanceSelect"),
  serverName: document.querySelector("#serverName"),
  serverType: document.querySelector("#serverType"),
  statusPill: document.querySelector("#statusPill"),
  playerCount: document.querySelector("#playerCount"),
  ramUsage: document.querySelector("#ramUsage"),
  cpuUsage: document.querySelector("#cpuUsage"),
  uptime: document.querySelector("#uptime"),
  startButton: document.querySelector("#startButton"),
  stopButton: document.querySelector("#stopButton"),
  restartButton: document.querySelector("#restartButton"),
  consoleState: document.querySelector("#consoleState"),
  consoleOutput: document.querySelector("#consoleOutput"),
  commandForm: document.querySelector("#commandForm"),
  commandInput: document.querySelector("#commandInput"),
  commandDisabled: document.querySelector("#commandDisabled"),
  playersState: document.querySelector("#playersState"),
  playerList: document.querySelector("#playerList")
};

let deviceToken = localStorage.getItem(tokenKey);
let selectedInstanceId = localStorage.getItem(instanceKey);
let socket = null;
let statusTimer = null;
let historyLoadedForInstance = null;
let lastInstanceStatus = null;

function pairingTokenFromUrl() {
  return new URLSearchParams(window.location.search).get("token");
}

function setVisible(view) {
  for (const item of [els.pairView, els.appView, els.emptyView, els.errorView]) {
    item.hidden = item !== view;
  }
}

function showNotice(message, tone = "info") {
  els.notice.textContent = message;
  els.notice.dataset.tone = tone;
  els.notice.hidden = false;
  clearTimeout(showNotice.timer);
  showNotice.timer = setTimeout(() => {
    els.notice.hidden = true;
  }, 3600);
}

async function api(path, options = {}) {
  const headers = new Headers(options.headers || {});
  if (deviceToken) {
    headers.set("Authorization", `Bearer ${deviceToken}`);
  }
  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(path, { ...options, headers });
  if (response.status === 401) {
    localStorage.removeItem(tokenKey);
    deviceToken = null;
    throw new Error("This browser is not paired or the device was revoked.");
  }

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Request failed (${response.status})`);
  }

  const contentType = response.headers.get("Content-Type") || "";
  return contentType.includes("application/json") ? response.json() : response.text();
}

async function start() {
  clearInterval(statusTimer);
  closeSocket();

  if (deviceToken) {
    if (pairingTokenFromUrl()) {
      history.replaceState({}, "", "/remote/index.html");
    }

    await openDashboard();
    return;
  }

  showPairPrompt();
}

function showPairPrompt() {
  closeSocket();
  clearInterval(statusTimer);
  const token = pairingTokenFromUrl();
  els.connectionLabel.textContent = token ? "Ready to pair" : "Not paired";
  els.pairTitle.textContent = token ? "Pair this browser" : "Pairing link needed";
  els.pairMessage.textContent = token
    ? "Open this link in Safari or your preferred browser before pairing. The link can pair more than one browser until it expires."
    : "Create a Pair Device link in PocketMC Desktop, then open it here.";
  els.pairButton.disabled = !token;
  els.copyPairLinkButton.disabled = !token;
  setVisible(els.pairView);
}

function setStateClass(element, state) {
  element.classList.remove("live", "offline", "busy");
  if (state) {
    element.classList.add(state);
  }
}

function setButtonLabel(button, text) {
  const label = button.querySelector("span:last-child");
  if (label) {
    label.textContent = text;
    return;
  }

  button.textContent = text;
}

async function pairDevice() {
  const pairingToken = pairingTokenFromUrl();
  if (!pairingToken) {
    showPairPrompt();
    return;
  }

  els.pairButton.disabled = true;
  setButtonLabel(els.pairButton, "Pairing...");
  try {
    const response = await fetch("/api/pairing/exchange", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        pairingToken,
        deviceName: navigator.userAgentData?.platform || navigator.platform || "Browser"
      })
    });

    if (!response.ok) {
      throw new Error("Pairing link expired. Create a new one in PocketMC Desktop.");
    }

    const payload = await response.json();
    localStorage.setItem(tokenKey, payload.deviceToken);
    deviceToken = payload.deviceToken;
    history.replaceState({}, "", "/remote/index.html");
    showNotice("This browser is paired.");
    await openDashboard();
  } catch (error) {
    els.pairMessage.textContent = error.message;
  } finally {
    els.pairButton.disabled = false;
    setButtonLabel(els.pairButton, "Pair Browser");
  }
}

async function openDashboard() {
  try {
    await refreshEverything({ reconnectConsole: true });
    statusTimer = setInterval(() => refreshEverything({ reconnectConsole: false }), 3000);
  } catch (error) {
    if (!deviceToken) {
      showPairPrompt();
    } else {
      showError(error.message);
    }
  }
}

async function refreshEverything({ reconnectConsole = false } = {}) {
  const instances = await api("/api/instances");
  renderInstances(instances);

  if (instances.length === 0) {
    historyLoadedForInstance = null;
    lastInstanceStatus = null;
    closeSocket();
    els.connectionLabel.textContent = "Connected";
    setVisible(els.emptyView);
    return;
  }

  if (!selectedInstanceId || !instances.some((instance) => instance.id === selectedInstanceId)) {
    selectedInstanceId = instances[0].id;
    localStorage.setItem(instanceKey, selectedInstanceId);
  }

  els.instanceSelect.value = selectedInstanceId;
  setVisible(els.appView);

  const [remoteStatus, instanceStatus] = await Promise.all([
    api("/api/status"),
    api(`/api/instances/${selectedInstanceId}/status`)
  ]);

  lastInstanceStatus = instanceStatus;
  renderStatus(remoteStatus, instanceStatus);

  if (reconnectConsole) {
    historyLoadedForInstance = null;
    closeSocket();
  }

  await ensureConsoleConnection(instanceStatus);
}

function renderInstances(instances) {
  const previousValue = els.instanceSelect.value;
  els.instanceSelect.innerHTML = "";
  for (const instance of instances) {
    const option = document.createElement("option");
    option.value = instance.id;
    option.textContent = instance.name;
    els.instanceSelect.append(option);
  }

  if (previousValue && instances.some((instance) => instance.id === previousValue)) {
    els.instanceSelect.value = previousValue;
  }
}

function renderStatus(remoteStatus, instanceStatus) {
  els.connectionLabel.textContent = remoteStatus.publicUrl || remoteStatus.localUrls?.[0] || "Connected";
  els.serverName.textContent = instanceStatus.name;
  els.serverType.textContent = instanceStatus.serverType;
  setStatusPill(instanceStatus);
  els.playerCount.textContent = `${instanceStatus.playerCount} / ${instanceStatus.maxPlayers}`;
  els.ramUsage.textContent = `${(instanceStatus.ramUsageMb / 1024).toFixed(1)} GB`;
  els.cpuUsage.textContent = `${instanceStatus.cpuUsage.toFixed(0)}%`;
  els.uptime.textContent = formatUptime(instanceStatus.uptimeSeconds);
  els.startButton.disabled = instanceStatus.isRunning;
  els.stopButton.disabled = !instanceStatus.isRunning;
  els.restartButton.disabled = !instanceStatus.isRunning;
  const canSendCommands = remoteStatus.allowRemoteConsoleCommands && instanceStatus.isRunning;
  els.commandForm.hidden = !canSendCommands;
  els.commandDisabled.hidden = remoteStatus.allowRemoteConsoleCommands || !instanceStatus.isRunning;
  renderPlayers(instanceStatus.onlinePlayers || [], remoteStatus.allowRemotePlayerActions);
}

function setStatusPill(instanceStatus) {
  const normalizedState = `${instanceStatus.state || ""}`.toLowerCase();
  const isBusy = normalizedState.includes("start") ||
    normalizedState.includes("stop") ||
    normalizedState.includes("restart");

  els.statusPill.textContent = instanceStatus.state || (instanceStatus.isRunning ? "Online" : "Offline");
  els.statusPill.classList.remove("online", "offline", "busy");
  els.statusPill.classList.add(instanceStatus.isRunning ? "online" : (isBusy ? "busy" : "offline"));
}

async function ensureConsoleConnection(instanceStatus) {
  if (!instanceStatus.isRunning) {
    closeSocket();
    els.consoleState.textContent = "Offline";
    setStateClass(els.consoleState, "offline");
    if (!els.consoleOutput.textContent) {
      els.consoleOutput.textContent = "Server is offline.";
    }
    return;
  }

  if (historyLoadedForInstance !== selectedInstanceId) {
    await loadConsoleHistory();
  }

  if (!socket || socket.readyState === WebSocket.CLOSED || socket.readyState === WebSocket.CLOSING) {
    openConsoleSocket();
  }
}

async function loadConsoleHistory() {
  if (!selectedInstanceId) return;
  try {
    const lines = await api(`/api/instances/${selectedInstanceId}/console/history`);
    els.consoleOutput.textContent = lines.length > 0 ? lines.join("\n") : "Waiting for console output...";
    historyLoadedForInstance = selectedInstanceId;
    scrollConsole();
  } catch {
    els.consoleOutput.textContent = "Console history is not available yet.";
  }
}

function openConsoleSocket() {
  if (!selectedInstanceId || !deviceToken) return;

  closeSocket();
  const protocol = window.location.protocol === "https:" ? "wss:" : "ws:";
  socket = new WebSocket(`${protocol}//${window.location.host}/ws/instances/${selectedInstanceId}/console?token=${encodeURIComponent(deviceToken)}`);
  const openedSocket = socket;
  els.consoleState.textContent = "Connecting";
  setStateClass(els.consoleState, "busy");

  socket.addEventListener("open", () => {
    els.consoleState.textContent = "Live";
    setStateClass(els.consoleState, "live");
  });

  socket.addEventListener("message", (event) => {
    const payload = JSON.parse(event.data);
    if (payload.type === "history" && historyLoadedForInstance === selectedInstanceId) {
      return;
    }
    appendConsole(payload.line);
  });

  socket.addEventListener("close", () => {
    if (socket !== openedSocket) {
      return;
    }

    socket = null;
    if (lastInstanceStatus?.isRunning) {
      els.consoleState.textContent = "Reconnecting";
      setStateClass(els.consoleState, "busy");
      setTimeout(() => ensureConsoleConnection(lastInstanceStatus), 1200);
    } else {
      els.consoleState.textContent = "Offline";
      setStateClass(els.consoleState, "offline");
    }
  });
}

function closeSocket() {
  if (socket) {
    const closingSocket = socket;
    socket = null;
    closingSocket.close();
  }
}

function appendConsole(line) {
  const current = els.consoleOutput.textContent === "Waiting for console output..." ? "" : els.consoleOutput.textContent;
  const next = current ? `${current}\n${line}` : line;
  els.consoleOutput.textContent = next.split("\n").slice(-500).join("\n");
  scrollConsole();
}

function scrollConsole() {
  els.consoleOutput.scrollTop = els.consoleOutput.scrollHeight;
}

function renderPlayers(players, allowActions) {
  els.playersState.textContent = `${players.length} online`;
  setStateClass(els.playersState, players.length > 0 ? "live" : "");
  els.playerList.innerHTML = "";

  if (players.length === 0) {
    const empty = document.createElement("p");
    empty.className = "muted";
    empty.textContent = "No players online.";
    els.playerList.append(empty);
    return;
  }

  for (const player of players) {
    const row = document.createElement("div");
    row.className = "player-row";

    const name = document.createElement("span");
    name.className = "player-name";
    name.textContent = player;
    row.append(name);

    for (const action of ["kick", "ban", "op"]) {
      const button = document.createElement("button");
      button.className = action === "ban" ? "danger-button" : "secondary-button";
      button.type = "button";
      button.textContent = action.toUpperCase();
      button.disabled = !allowActions;
      button.addEventListener("click", () => playerAction(player, action));
      row.append(button);
    }

    els.playerList.append(row);
  }
}

async function playerAction(player, action) {
  try {
    await api(`/api/instances/${selectedInstanceId}/players/${encodeURIComponent(player)}/${action}`, {
      method: "POST",
      body: JSON.stringify({})
    });
    showNotice(`${action.toUpperCase()} sent for ${player}.`);
    await refreshEverything({ reconnectConsole: false });
  } catch (error) {
    showNotice(error.message, "error");
  }
}

async function instanceCommand(command) {
  setActionButtonsDisabled(true);
  try {
    await api(`/api/instances/${selectedInstanceId}/${command}`, { method: "POST" });
    showNotice(`${capitalize(command)} requested.`);
    await refreshEverything({ reconnectConsole: true });
  } catch (error) {
    showNotice(error.message, "error");
  } finally {
    setActionButtonsDisabled(false);
  }
}

function setActionButtonsDisabled(disabled) {
  els.startButton.disabled = disabled || lastInstanceStatus?.isRunning === true;
  els.stopButton.disabled = disabled || lastInstanceStatus?.isRunning !== true;
  els.restartButton.disabled = disabled || lastInstanceStatus?.isRunning !== true;
}

function showError(message) {
  closeSocket();
  clearInterval(statusTimer);
  els.connectionLabel.textContent = "Connection problem";
  els.errorMessage.textContent = message;
  setVisible(els.errorView);
}

function formatUptime(seconds) {
  if (!seconds) return "0m";
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
}

function capitalize(value) {
  return `${value.charAt(0).toUpperCase()}${value.slice(1)}`;
}

els.pairButton.addEventListener("click", pairDevice);
els.copyPairLinkButton.addEventListener("click", async () => {
  try {
    await navigator.clipboard.writeText(window.location.href);
    showNotice("Pairing link copied.");
  } catch {
    showNotice("Copy is unavailable in this browser.", "error");
  }
});
els.refreshButton.addEventListener("click", () => refreshEverything({ reconnectConsole: true }).catch((error) => showError(error.message)));
els.emptyRefreshButton.addEventListener("click", () => refreshEverything({ reconnectConsole: false }).catch((error) => showError(error.message)));
els.retryButton.addEventListener("click", start);
els.clearTokenButton.addEventListener("click", () => {
  localStorage.removeItem(tokenKey);
  deviceToken = null;
  history.replaceState({}, "", "/remote/index.html");
  showPairPrompt();
});
els.instanceSelect.addEventListener("change", async () => {
  selectedInstanceId = els.instanceSelect.value;
  localStorage.setItem(instanceKey, selectedInstanceId);
  await refreshEverything({ reconnectConsole: true });
});
els.startButton.addEventListener("click", () => instanceCommand("start"));
els.stopButton.addEventListener("click", () => instanceCommand("stop"));
els.restartButton.addEventListener("click", () => instanceCommand("restart"));
els.commandForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const command = els.commandInput.value.trim();
  if (!command) return;
  els.commandInput.value = "";
  try {
    await api(`/api/instances/${selectedInstanceId}/console/command`, {
      method: "POST",
      body: JSON.stringify({ command })
    });
  } catch (error) {
    showNotice(error.message, "error");
  }
});

start();
