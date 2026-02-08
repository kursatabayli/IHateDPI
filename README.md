<div align="center">
  <p>
    <a href="README-tr.md">Türkçe Versiyon</a>
  </p>
</div>

# IHateDPI

**IHateDPI** is developed for Windows operating systems to gain technical competence in network traffic analysis, packet manipulation, and cybersecurity, while simultaneously putting theoretical knowledge of memory management in the modern .NET infrastructure into practice.

This project is a **.NET 9** based, modern, performance-oriented, and open-source tool.

> **Note:** This project is inspired by the [GoodbyeDPI](https://github.com/ValdikSS/GoodbyeDPI) project; it has been built from scratch using modern C# techniques, Native AOT, and the latest .NET infrastructure.

## Features

* **DoH (DNS over HTTPS):** Encrypts your DNS queries and sends them to a 3rd party DNS server (default is Cloudflare).
* **TCP Window Clamping:** Manipulates the data flow coming from the server to complicate DPI analysis.
* **HTTPS Fragmentation:** Splits secure connection (TLS) packets to bypass SNI (Server Name Indication) analysis.
* **QUIC Blocking:** Forces browsers to use the TCP protocol, which is easier to manipulate.
* **Modern Infrastructure:** Fast, lightweight, and requires no installation thanks to .NET 9 performance and Native AOT technology.

---

## Technical Architecture & Case Analysis

A detailed report has been prepared regarding the technical data gathered during the development process, the working principles of DPI systems, and TLS handshake manipulations.

You can access this technical paper, covering ISP behavior analysis and protocol-level insights, below:

* 📄 **[Read the Technical Case Study](docs/TECHNICAL_REPORT_EN.md)**

---

## Configuration & Troubleshooting

For a detailed explanation of all engine settings, parameters, packet manipulation strategies, and troubleshooting steps, please refer to the dedicated configuration guide:

👉 **[Configuration Guide & Troubleshooting](docs/CONFIGURATION.md)**

> **Tip:** If you are experiencing connection issues or want to customize the settings for your ISP, this guide provides critical information (such as DoH setup, fragmentation strategies, and specific workarounds).

---

### System Requirements & Constraints

* **64-bit Only:** The project supports only **64-bit (x64)** Windows operating systems. It will not work on 32-bit (x86) systems.
* **IPv6 Support:** Currently, only **IPv4** traffic is supported and processed. If IPv6 is active on your system, IPv6 traffic will not be filtered or manipulated by this tool (it is passed through as-is). IPv6 support is **under development**.

---

## External Engine Support

IHateDPI Launcher has evolved into a **Universal Launcher**. You are no longer limited to the built-in engine; you can now integrate and manage any CLI-based DPI evasion tool (such as GoodbyeDPI, Zapret, Byedpi, etc.) directly through this modern interface.

Eliminate the need to deal with multiple console windows and manage everything from a single, unified dashboard.

### Configuration

Navigate to the **"External Engine"** tab in the Settings menu:

1.  **Select Executable (`.exe`):** Browse and select the core executable of the tool you wish to use (e.g., `goodbyedpi.exe`).
2.  **Script Integration (`.cmd`, `.bat`):** If you have a pre-configured script file (e.g., `1_russia_blacklist_dnsredir.cmd`), you can select it directly. The launcher will handle the execution automatically.
3.  **Manual Arguments:** Alternatively, you can input raw launch arguments (e.g., `-9 --dns-addr 1.1.1.1`) directly into the manual arguments field.

> **For GoodbyeDPI Users:**
> Simply download the original GoodbyeDPI files. In IHateDPI settings, point the "Executable" to `x86_64/goodbyedpi.exe` and select your preferred `.cmd` file as the "Script Path". That's it!

---

## Installation and Usage

This tool is designed as "Portable". It does not require any installation.

1.  Download either `IHateDPI-vx.x.x-win-x64.zip` (recommended) or `IHateDPI-Engine-vx.x.x-win-x64.zip` from the [Releases](https://github.com/kursatabayli/IHateDPI/releases) page.
2.  Extract the Zip file to a folder.
3.  Right-click on `IHateDPI Launcher.exe` (recommended) or `IHateDPI Engine.exe` and select **"Run as Administrator"**.
4.  Starting the Application:
    * **If using the Launcher:** Click the "Start" button on the interface.
    * **If using the bare Engine:** Once the console window opens, the program has started working.

> **Why Administrator Rights?**
> The program uses the **WinDivert** driver to capture, filter, and manipulate network packets. Due to Windows security architecture, administrator rights are mandatory to communicate with this driver.

## Development Status and Feedback

**This project is currently under active development.**

While the core features work stably, you may encounter unexpected behavior with different network providers or system configurations.

* **Bug Reporting:** If you find a non-working feature or a bug, please share it via the **[Issues](https://github.com/kursatabayli/IHateDPI/issues)** tab.

## Disclaimer

This software is developed for **educational and research purposes**. It aims to provide technical competence in network traffic analysis, packet manipulation, and cybersecurity.

Any legal liability arising from the use of the software belongs entirely to the user. The developer cannot be held responsible for the misuse of this tool.

## Credits and Resources

This project is built upon the following open-source projects:

* **[GoodbyeDPI](https://github.com/ValdikSS/GoodbyeDPI) by [ValdikSS](https://github.com/ValdikSS)**
* **[WinDivert](https://github.com/basil00/WinDivert) by [basil00](https://github.com/basil00)**

## License

This project is licensed under the **[GNU Affero General Public License v3.0 (AGPL-3.0)](https://github.com/kursatabayli/IHateDPI/blob/development/LICENSE)**.
