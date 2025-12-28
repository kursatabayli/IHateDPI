# IHateDPI Configuration Guide

This document explains the engine settings (`EngineConfig`) of the `IHateDPI` application and their roles in bypassing DPI (Deep Packet Inspection) systems. By modifying these settings, you can create configurations specific to your Internet Service Provider (ISP) and network infrastructure.

## ⚙️ Configuration Methods

You can choose one of the following two methods to change settings, depending on the version you are using:

### 1. GUI (Launcher) Users
If you are using the application with the **Launcher (GUI)**, you do not need to deal with the JSON file.
* You can perform all configurations using the **"Settings"** menu within the application.
* Changes you make are automatically saved to the `engineConfig.json` file by the application.

### 2. Engine-Only (Console Engine) Users
If you are using the console application (`IHateDPI.Engine`) directly without the interface:
* Open the **`engineConfig.json`** file in the program's folder with a text editor (Notepad, VS Code, etc.).
* Edit the relevant values according to the explanations in this document, save, and restart the application.

---

⚠️ **IMPORTANT STARTING NOTE:**
The blocking technologies (DPI) used by Internet Service Providers (ISPs) vary significantly. **There is no single "Magic Setting" that works for all users.**
* Default settings are just a starting point.
* A setting that works on your network might cut off someone else's internet.
* **Solution:** You must understand the parameters below and find the correct combination for your own connection through **trial and error**.

---

## 🌐 Network & Connection Settings

### `isDoHEnabled` (DNS over HTTPS)
* **Description:** Encapsulates DNS queries within HTTPS traffic (Encrypted DNS).
* **Default:** `true`
* **Recommendation:** This setting must be enabled if blocking is done at the DNS level rather than the IP level for banned sites.

### `dohProviderUrl`
* **Description:** Secure DNS server address.
* **⚠️ Important:** Do not write a domain name here; use a URL containing an **IP address**.
* **Default:** `https://1.1.1.1/dns-query`
* **Alternatives:**
  * Google: `https://8.8.8.8/dns-query`
  * Quad9: `https://9.9.9.9/dns-query`

### `blockQuic` (Block QUIC)
* **Description:** Prevents browsers from using the UDP-based QUIC/HTTP3 protocol and forces them to use TCP.
* **Default:** `false`
* **Why Enable?** IHateDPI performs packet manipulations on TCP. If your browser uses QUIC (UDP) when accessing YouTube or Google services, the DPI engine cannot manipulate these packets. In this case, you should set this to `true`.

### `maxPayloadSize` (TCP Window Clamping)
* **Description:** Limits the maximum data size of sent TCP packets (MSS Clamping).
* **Default:** `1200`
* **⚠️ Recommendation:** It is **not recommended to change** this setting.
* **Why?**
    * The value `1200` is the safest value that ensures packets are transmitted smoothly even if you use VPNs or different network tunnels.
    * **If Too Low:** (e.g., 500) Your internet speed drops significantly.
    * **If Too High:** (e.g., 1500) Packets may get lost in transit or get caught by DPI systems more easily.

---

## ✂️ Fragmentation Settings

Packet fragmentation is one of the most effective ways to bypass DPI systems. By splitting the Request into multiple small packets, it makes it difficult for the DPI device to reassemble and understand it as a "Request going to a banned site".

### `autoSplitSni` (Smart SNI Splitting)
* **Description:** The engine automatically detects the SNI (hostname) information inside the HTTPS packet and splits the packet exactly in the middle.
* **Default:** `false`
* **Advantage:** Instead of manually counting bytes with `fragmentHttps`, it ensures the engine splits the packet at the most critical point.

### `fragmentHttps` (Manual HTTPS Splitting)
* **Description:** Splits the HTTPS (TLS ClientHello) packet after the specified number of bytes.
* **Default:** `0` (Disabled)
* **Usage:** If `autoSplitSni` is off or doesn't work, you can perform manual splitting by entering a value between `1` and `5` here.

### `fragmentHttp` (HTTP Splitting)
* **Description:** Splits unencrypted HTTP requests after the specified number of bytes.
* **Default:** `0` (Disabled)

### `reverseFragmentation` (Send in Reverse Order)
* **Description:** Sends split packets in reverse order (2nd part first, then 1st part).
* **Default:** `false`
* **Note:** Very effective on Stateful DPI systems, but some modems/routers do not like this.

---

## ☠️ Buffer Poisoning

Aims to prevent the examination of the real packet by filling the DPI device's memory (buffer) with "junk" data.

### ⚠️ CRITICAL OPERATION REQUIREMENT
**It is MANDATORY for packets to be split for this feature to work.**
Therefore, if you are going to enable Buffer Poisoning, **at least one** of the following must be done:
1. ✅ `autoSplitSni`: MUST be **true**
2. ✅ OR `fragmentHttps`: MUST be **greater than 0**

If the packet is not split, a "gap" to inject toxic (junk) packets will not be created.

### `bufferPoisoning`
* **Description:** Squeezes fake "Junk" packets between fragmented real packets.
* **Default:** `false`

### Junk Packet Settings:
* **`junkPacketSize`**: Size of the junk packet (bytes). (Default: `1`)
* **`junkPacketCount`**: How many junk packets to send. (Default: `1`)
* **`junkPacketTTL`**: Time To Live of the junk packet. (Default: `5`)
    * *Logic:* This packet must pass through the DPI but die before reaching the real server.
* **`junkPacketBadChecksum`**: Corrupts the checksum of the junk packet. (Default: `false`)
* **`junkPacketBadSequence`**: Corrupts the sequence number of the junk packet. (Default: `false`)

---

## 🛠️ Header Manipulation (HTTP Only)

Bypasses filters by modifying the "Host" header on unencrypted HTTP sites.
* **`mixHost`**: Makes it `hOsT: example.com`. (Default: `false`)
* **`hostNoSpace`**: Makes it `Host:example.com` (removes space). (Default: `false`)
* **`additionalSpace`**: Adds extra space between Method and URI. (Default: `false`)

---

## 🎭 Fake Packet Settings

These settings squeeze "Fake" data packets in between to deceive the DPI system. The settings in this section are **advanced**; incorrect configuration may completely cut off your internet connection.

### `fakePacketTTL` (TTL and Auto-Tracking)
* **Description:** Determines how many hops the fake packet will travel on the network.
* **Auto-Learning:** There is a **TtlTracker** embedded in the application. This system automatically calculates the distance between you and the target server. The value you enter here is a **"Fallback"** value used when the system hasn't performed a calculation yet.
* **Default:** `5`
* **To Disable:** If you set this value to `0`, fake packet transmission is completely **disabled**.
* **Logic:** The packet must pass through the DPI device but perish before reaching the real server.

### `badSequence` (Bad Sequence Number)
* **Description:** Deliberately sends the fake packet's TCP Sequence Number incorrectly.
* **Risk:** While it allows bypassing DPI on some ISPs, it can **completely break the internet connection** on others.
* **Recommendation:** Keep disabled by default. If other methods fail, try enabling it; if your connection drops, disable it again.
* **Default:** `false`

### `badCheckSum` (Bad Checksum)
* **Description:** Sends the fake packet's validation code (Checksum) as corrupt.
* **Logic:** DPI systems generally skip this check to gain performance and accept the packet; however, real servers reject the packet (which is exactly what we want).
* **Risk:** Just like `badSequence`, some network hardware (modems, routers) may automatically block packets with bad checksums, causing connection issues. Requires trial and error.
* **Default:** `false`

### `fakeRequestResendCount`
* **Description:** Determines how many times the fake packet will be sent consecutively.
* **Default:** `1`
* **Recommended Range:** `1 - 3`
* **Warning:** Increasing this number too much (e.g., to 10) unnecessarily bloats your network traffic and may cause your modem to lock up. Generally, `1` or at most `2` is sufficient.
