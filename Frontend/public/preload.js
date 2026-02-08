// Electron preload script — bridges IPC between isolated renderer and main process.
// Also intercepts console.log/warn/error → routes to .NET Logger via IPC.
const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electron', {
  ipcRenderer: {
    send(channel, ...args) {
      ipcRenderer.send(channel, ...args);
    },
    on(channel, listener) {
      const wrapped = (_event, ...args) => listener(...args);
      ipcRenderer.on(channel, wrapped);
      return () => ipcRenderer.removeListener(channel, wrapped);
    },
    invoke(channel, ...args) {
      return ipcRenderer.invoke(channel, ...args);
    },
  },
});

// ─── Console interception → .NET Logger ────────────────────────
const originalLog = console.log;
const originalWarn = console.warn;
const originalError = console.error;

console.log = (...args) => {
  originalLog.apply(console, args);
  try { ipcRenderer.send('hyprism:console:log', args.map(String).join(' ')); } catch {}
};

console.warn = (...args) => {
  originalWarn.apply(console, args);
  try { ipcRenderer.send('hyprism:console:warn', args.map(String).join(' ')); } catch {}
};

console.error = (...args) => {
  originalError.apply(console, args);
  try { ipcRenderer.send('hyprism:console:error', args.map(String).join(' ')); } catch {}
};
