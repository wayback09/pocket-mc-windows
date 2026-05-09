
<div align="center">

<table border="0" cellpadding="16">
  <tr>
    <td align="center" width="200">
      <img src="docs/assets/logo.png" alt="PocketMC" width="180" />
    </td>
    <td align="center">
      <h1 style="border: none; margin-bottom: 10px;">Pocket MC</h1>
      <p><b>Run Minecraft Java, Bedrock, and Cross-play servers on Windows.<br> No terminal. No Java headaches. No mess.</b></p>
      <a href="https://github.com/PocketMC/pocket-mc-windows/actions"><img src="https://img.shields.io/github/actions/workflow/status/PocketMC/pocket-mc-windows/production-build.yml?branch=master&style=flat-square&logo=github" alt="Build" /></a>
      <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET" /></a>
      <a href="https://www.microsoft.com/windows"><img src="https://img.shields.io/badge/Windows-10%2F11-0078D4?style=flat-square&logo=windows" alt="Platform" /></a>
      <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-22C55E?style=flat-square" alt="License" /></a>
      <a href="https://github.com/PocketMC/pocket-mc-windows/releases"><img src="https://img.shields.io/github/v/release/PocketMC/pocket-mc-windows?style=flat-square" alt="Release" /></a>
      <a href="https://discord.gg/h27uNCaxPH"><img src="https://img.shields.io/badge/Discord-Join-%235865F2?style=flat-square&logo=discord" alt="Discord" /></a>
      <a href="https://www.buymeacoffee.com/sahaj33"><img src="https://img.shields.io/badge/Donate-BMC-%23FFDD00?style=flat-square&logo=buy-me-a-coffee&logoColor=black" alt="Buy Me A Coffee" /></a>
    </td>
  </tr>
</table>

<img src="docs/assets/screenshot-dashboard.png" alt="PocketMC Dashboard" width="860" style="border-radius: 10px; margin-top: 16px;" />

</div>

---
## What PocketMC is NOT

- Not a hosting service — runs locally on your PC
- Not a Linux server panel — no Docker, no SSH, no sysadmin skills needed
- Not a Minecraft launcher — it manages servers, not the game
- Not a script pack — replaces manual setup entirely


---


## What it does

PocketMC is a Windows desktop app for creating and managing Minecraft server instances — Java and Bedrock — without touching a command line. Java is bundled automatically. Public sharing is one click via Playit.gg. Without dealing with Java installs, port forwarding, or command-line setup.

Supported server types: **Vanilla · Paper · Fabric · Forge · Bedrock (BDS) · PocketMine-MP**

---

## Features

### 🎮 Server Management

- **Multi-Protocol Support** — Run Minecraft Java, native Bedrock Edition (BDS), and PocketMine-MP instances side-by-side. Includes one-click installation for 45+ Bedrock versions via community manifests.
- **Managed Runtimes** — PocketMC automatically provisions isolated JRE and PHP 8.x runtimes. Nothing touches your system Java/PHP installation.
- **Add-on & Plugin Browser** — Native support for Bedrock `.mcpack`/`.mcaddon` and Poggit (PocketMine) integration. Browse and install Java plugins from Modrinth/CurseForge. *Note: Poggit plugins currently do not support automatic dependency resolution due to upstream API limitations.*
- **Public Tunneling** — Integrated Playit.gg with guided setup. Public addresses are shown as copyable links on the dashboard.
- **Instance Isolation** — Each server runs in its own folder with independent configs, runtimes, and world files.

### 🔭 Observability & Intel

- **Live Metrics** — Real-time CPU, RAM, and player count tracking per instance.
- **Dependency Health Dashboard** — Live status monitoring for Adoptium, Playit.gg, and Modrinth microservices directly in the settings.
- **Modern AI Console** — Colorized logs with multi-keyword search, Regex filtering, command history, intelligent command auto-suggestions and crashes and errors AI analysis.
- **AI Session Summaries** — Generate structured summaries of your server sessions using Google Gemini, OpenAI, Anthropic Claude, Mistral AI, or Groq.
- **Disaster Recovery** — Automated backups with optional off-site replication to Google Drive/Dropbox sync directories and one-click support bundle export.

### 🛡️ Technical Excellence

- **RCON Client Engine** — Standard I/O is deprecated in favor of a robust managed RCON client, eliminating synchronization deadlocks on high-load servers.
- **Artifact Integrity** — Deep SHA1/SHA256 verification for all downloads (Playit daemon, server binaries) ensures your local files are never corrupted or tampered with.
- **Graceful Lifecycle** — A custom 15-second shutdown loop ensures all players are kicked and worlds are saved correctly before the app exits.
- **PII Scrubbing** — Automated RegEx pipelines scrub personal data (IPv4, emails) from console logs before they are processed by AI or exported.
- **UWP Loopback Automation** — One-click "Fix Bedrock LAN" tool to handle UWP network isolation, allowing local connections to Bedrock servers.

---

## Comparison

| Tool | Type | Hosting | Cost | Java | Bedrock | Mods/Plugins | 1-Click Install | Backups | Live Metrics | No Port-Forward | Open Source |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **PocketMC** | Windows desktop app | Self-hosted / local PC | Free | ✅ Vanilla / Paper / Fabric / Forge / NeoForge beta | ✅ Native BDS + PocketMine-MP + Geyser/Floodgate cross-play | ✅ Modrinth / CurseForge / Poggit / Bedrock add-ons | ✅ Create + download instance | ✅ Manual + scheduled, optional sync-folder replication | ✅ CPU / RAM / players | ✅ Playit.gg built-in | ✅ MIT |
| SquidServers | Cross-platform desktop app | Self-hosted | Free | ✅ Vanilla / Paper / Fabric / Forge/modded platforms | ✅ BDS + Geyser cross-play | ✅ Modrinth modpacks + CurseForge server packs | ✅ Automatic server setup | ❓ Not clearly verified | ❓ Not clearly verified | ✅ Built-in playit.gg tunneling | ❌ Proprietary |
| auto-mcs | Desktop app + Docker option | Self-hosted | Free | ✅ Paper / Purpur / Fabric / Quilt / NeoForge / Forge / Spigot / CraftBukkit / Vanilla | ❌ No native Bedrock/Geyser evidence found | ✅ Modrinth mods/plugins/modpacks | ✅ Fast templates / auto install | ✅ Automatic backup management | ✅ Server status/crash/reporting; exact CPU/RAM depth unclear | ✅ Playit.gg integration | ✅ GPL-3.0 |
| MCSManager | Web panel | Self-hosted / distributed nodes | Free | ✅ Via marketplace/templates/custom commands | ✅ Possible via templates/images, not native-first UX | ✅ Marketplace + custom server files | ✅ Built-in app marketplace | ✅ Supported, exact workflow depends config | ✅ Web terminal/resource panel | ❌ Needs public IP/ports/tunnel separately | ✅ Apache-2.0 |
| Pterodactyl | Web panel | Self-hosted / Docker nodes | Free | ✅ Minecraft eggs: Paper, Sponge, Bungeecord, Waterfall, etc. | ⚠️ Via community eggs, not native-first UX | ✅ Eggs + manual mod/plugin/server files | ⚠️ Yes after egg/nest setup | ✅ Built-in backups/schedules | ✅ Server resource stats | ❌ Needs ports/reverse proxy/tunnel separately | ✅ MIT |
| fork.gg / Fork legacy | Windows GUI | Self-hosted | Free | ✅ Vanilla / Paper / Waterfall / related Java servers | ❌ No Bedrock support verified | ⚠️ Mods/plugins depend server type/manual setup | ✅ Launcher + GUI setup | ❓ Not verified as built-in | ⚠️ Player/status notifications; rich metrics not verified | ❌ No built-in tunnel verified | ✅ MIT |
| Apex Hosting | Managed host | Cloud | Paid; Java from ~$14.99/mo recurring, Bedrock from ~$3.99/mo recurring | ✅ Java Edition hosting | ✅ Bedrock Edition hosting | ✅ Mods/plugins/modpacks | ✅ Instant setup / game panel | ✅ Automated/offsite backups | ✅ Host dashboard graphs/panel | N/A | ❌ Proprietary |
| Aternos | Managed host | Cloud | Free with ads | ✅ Vanilla / Paper / Spigot / Purpur / Forge / NeoForge / Fabric / Quilt / modpacks | ✅ Bedrock + PocketMine | ⚠️ Catalog-based mods/plugins; no arbitrary upload | ✅ Easy setup | ✅ Google Drive backups + auto backup on stop | ⚠️ Real-time console, not rich CPU/RAM metrics | N/A | ❌ Proprietary |
| CubeCoders AMP | Web panel | Self-hosted / optionally hosted | Paid lifetime license | ✅ Minecraft support | ✅ Java/Bedrock via AMP ecosystem | ✅ Templates/modules/file management | ✅ Single-command install + templates | ✅ Manual/scheduled backups | ✅ Monitoring/resource/player stats | ❌ Needs ports/tunnel separately | ❌ Proprietary |
| e4mc | Minecraft Java tunnel mod | Local LAN world/server exposed by tunnel | Free | ✅ Java LAN/server tunneling | ❌ | N/A | ✅ Open to LAN flow | ❌ | ❌ | ✅ Built for no port-forward hosting | ✅ MIT |
| Essential Mod | Client mod / P2P-style world hosting | Host’s PC | Free | ✅ Java Edition world hosting | ❌ | ⚠️ Works with mods/modpacks only if everyone has same mods/versions | ✅ Host World button | ❌ | ❌ | ✅ P2P-style/no normal port-forward flow | ❌ Source-available, not open-source |
| Minehut | Managed host | Cloud | Free + paid credits; free plan has limits | ✅ Java servers | ✅ Bedrock cross-play beta/default | ✅ Mods/plugins; plan/file limits apply | ✅ Dashboard setup | ✅ Manual backups; slots scale by plan | ⚠️ Dashboard panel, rich ops metrics not verified | N/A | ❌ Proprietary |
| playit.gg | Tunnel service / agent | Tunnel only | Free + Premium around $3/mo | ✅ Minecraft Java tunnel | ✅ Minecraft Bedrock tunnel | N/A | N/A | ❌ | ✅ Tunnel status/stats, not server metrics | ✅ Core purpose | ⚠️ Public agent source, license unclear |

---

## Installation

Download `Setup.exe` from the [latest release](https://github.com/PocketMC/pocket-mc-windows/releases/latest) and run it.

- No admin rights required — installs per-user.
- .NET 8 Desktop Runtime is prompted automatically if missing.
- Java does **not** need to be pre-installed. PocketMC manages its own JRE stack.
- Updates are handled automatically via Velopack.

---

## Quick Start

**1. Pick a root folder** on first launch. Everything — servers, runtimes, tunnel — lives here.

**2. Create an instance.** Hit **New Instance**, choose a server type and version, accept the EULA, click **Create & Download**. The JAR fetches automatically.

**3. Start your server.** Hit **Start**. Metrics go live. Connect from Minecraft at `localhost` or your LAN IP.

**Optional: Enable public access.** Open the instance, enable Playit.gg tunneling, and follow the one-time account link flow. Your public address appears on the dashboard.

---
* ✔ Fully isolated — Java and PHP are managed internally
* ✔ No system setup — no environment variables, no conflicts
* ✔ Runs multiple servers safely side-by-side

---

## Screenshots

| Dashboard | Server Console |
|-----------|---------------|
| ![Dashboard](docs/assets/screenshot-dashboard.png) | ![Console](docs/assets/screenshot-console.png) |

| Server Settings | App Settings |
|-----------------|-----------------|
| ![ServerSettings](docs/assets/server-settings.png) | ![Settings](docs/assets/screenshot-settings.png) |

| Plugin Browser | Mod Marketplace |
|-----------------|----------------|
| ![Plugins](docs/assets/screenshot-plugins.png) | ![Marketplace](docs/assets/mod-marketplace.png) |

| Public Tunnels | Managed Runtimes | About |
|-----------------|----------------|-------|
| ![Tunnels](docs/assets/tunnels.png) | ![Runtimes](docs/assets/runtimes.png) | ![About](docs/assets/about.png) |


---

## System Requirements

| | Minimum |
|---|---|
| OS | Windows 10 1809 (build 17763) or Windows 11 |
| Architecture | x64 |
| RAM | 4 GB (8 GB+ recommended) |
| .NET | .NET 8 Desktop Runtime (auto-prompted on install) |
| Internet | Required for first-run JRE download and Playit.gg |

---

## Contributing

Fork the repo, branch off `main`, and open a PR with a clear description of what changed and why. For significant architecture changes, open an issue first.

When testing locally, cover process lifecycle edge cases — crash recovery, orphan process cleanup, tunnel teardown. The full build guide is in [`CONTRIBUTING.md`](CONTRIBUTING.md).

---

## Community

**Discord:** [discord.gg/h27uNCaxPH](https://discord.gg/h27uNCaxPH)

---

## License

MIT © 2024 PocketMC Contributors — see [LICENSE](LICENSE).

---

<div align="center">

<a href="https://www.buymeacoffee.com/sahaj33" target="_blank">
  <img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" height="41" />
</a>

</div>
