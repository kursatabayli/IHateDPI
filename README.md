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

## ⚙️ Configuration & Troubleshooting

For a detailed explanation of all engine settings, parameters, packet manipulation strategies, and troubleshooting steps, please refer to the dedicated configuration guide:

👉 **[Configuration Guide & Troubleshooting](docs/CONFIGURATION.md)**

> **Tip:** If you are experiencing connection issues or want to customize the settings for your ISP, this guide provides critical information (such as DoH setup, fragmentation strategies, and specific workarounds).

---

### System Requirements & Constraints

* **64-bit Only:** The project supports only **64-bit (x64)** Windows operating systems. It will not work on 32-bit (x86) systems.
* **IPv6 Support:** Currently, only **IPv4** traffic is supported and processed. If IPv6 is active on your system, IPv6 traffic will not be filtered or manipulated by this tool (it is passed through as-is). IPv6 support is **under development**.

---

## GoodbyeDPI Integration

IHateDPI Launcher allows you to use the original **GoodbyeDPI** software as the engine if desired. This allows you to easily switch between the two engines.

**How to Use:**
1.  Download the original GoodbyeDPI files.
2.  Copy the downloaded files (including the `x86_64` folder and `.cmd` files) into the `Engines/GoodbyeDPI` folder located in the application's directory.
3.  Open IHateDPI Launcher and go to the **Settings** menu.
4.  Select **GoodbyeDPI** from the engine selection screen and choose the `.cmd` file you wish to run from the list.

---

## Installation and Usage

This tool is designed as "Portable". It does not require any installation.

1.  Download either `IHateDPI-v1.0.0-beta.1-win-x64.zip` (recommended) or `IHateDPI-Engine-v1.0.0-beta.1-win-x64.zip` from the [Releases](https://github.com/kursatabayli/IHateDPI/releases) page.
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
