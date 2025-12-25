# IHateDPI Configuration Guide

This document explains the engine settings (`EngineConfig`) of the `IHateDPI` application and their roles in bypassing DPI (Deep Packet Inspection) systems. By modifying these settings, you can create configurations specific to your Internet Service Provider (ISP) and network infrastructure.

## ⚙️ Configuration Methods

Depending on the version you are using, you can choose one of the two methods below to change settings:

### 1. GUI (Launcher) Users
If you are using the application with the **Launcher (Interface)**, you do not need to deal with the JSON file directly.
* You can configure everything using the **"Settings"** menu within the application.
* Changes you make are automatically saved to the `engineConfig.json` file by the application.

### 2. Engine-Only (Console) Users
If you are using the console application (`IHateDPI.Engine`) directly without an interface:
* Open the **`engineConfig.json`** file located in the program's folder with a text editor (Notepad, VS Code, etc.).
* Edit the relevant values according to the explanations in this document, save the file, and restart the application.

---

⚠️ **IMPORTANT STARTING NOTE:**
The blocking technologies (DPI) used by Internet Service Providers (ISPs) vary greatly. **There is no single "Magic Setting" that works for all users.**
* The default settings are just a starting point.
* A setting that works perfectly on your network might cut off someone else's internet connection completely.
* **Solution:** You must understand the parameters below and find the correct combination for your connection through **trial and error**.

---

## 🌐 Network & Connection Settings

These settings determine general connection behaviors and protocol preferences.

### `isDoHEnabled` (DNS over HTTPS)
* **Description:** Toggles Secure DNS (DoH) on or off. This feature encrypts your DNS queries by hiding them inside HTTPS traffic.
* **Default:** `true` (On)
* **How Should It Be Set?**
    * **Try Turning Off First:** If you can access blocked sites while this setting is `false` (off), it is recommended to keep it off. This allows you to use your local DNS server, which may result in slightly lower connection latency (ping).
    * **When to Turn On?** If the site is unreachable while off, redirects to a different "Blocked" page, or the browser gives an "IP address not found" error; this means your ISP is listening to and manipulating your DNS traffic (UDP 53). In this case, you **must turn this setting on**.

### `dohProviderUrl`
* **Description:** The server address where encrypted DNS queries will be sent.
* **⚠️ CRITICAL WARNING:** NEVER write a URL containing a domain name in this field! (e.g., DO NOT WRITE `https://dns.google/dns-query`).
* **Correct Usage:** The URL must strictly contain an **IP address**.
    * ✅ Correct: `https://1.1.1.1/dns-query`
    * ❌ Incorrect: `https://cloudflare-dns.com/dns-query`
* **Why? (Chicken-and-Egg Problem):** If you write a domain name here, the program tries to resolve its IP address to connect to this server. However, since the DNS server is not running yet, the IP cannot be resolved, and the program locks up.
* **Default:** `https://1.1.1.1/dns-query` (Cloudflare)
* **Safe IP Addresses You Can Use:**
  * Google: `https://8.8.8.8/dns-query`
  * Quad9: `https://9.9.9.9/dns-query`

### `blockQuic` (QUIC/HTTP3 Blocking)
* **Description:** Blocks the UDP-based QUIC protocol used by browsers (especially Chrome).
* **Why is it Important?** `IHateDPI` performs packet manipulation over the TCP protocol. Since QUIC uses UDP, it can bypass these manipulations. When this setting is on, browsers are forced to fall back to TCP, allowing DPI evasion techniques to work.
* **Default:** `true` (On)

### `maxPayloadSize` (TCP Payload Size)
* **Description:** Limits the maximum data size of sent TCP packets (MSS Clamping).
* **Default:** `1200`
* **⚠️ Recommendation:** It is **not recommended** to change this setting.
* **Why?**
    * The value `1200` is the safest value that ensures packets are transmitted without issues even if you use VPNs or different network tunnels.
    * **If set too low:** (e.g., 500) Your internet speed will drop significantly.
    * **If set too high:** (e.g., 1500) Packets may get lost en route or be more easily caught by DPI systems.

---

## ✂️ Fragmentation Settings

Packet fragmentation is one of the most effective ways to bypass DPI systems. By splitting the Request into multiple small packets, it makes it difficult for the DPI device to reassemble them and understand that "This is a request going to a banned site."

### `fragmentHttps` (Critical Setting)
* **Description:** Splits the first packet (TLS ClientHello) in HTTPS connections (Port 443) after the specified number of bytes.
* **Default:** `2`
* **Value Range:**
  * `0`: **Disabled** (Turns off the feature).
  * `1 - 5`: **Recommended Range.** (These values are most effective at confusing DPI systems as they split the TLS header).
* **Why is it Important?** Even though HTTPS is encrypted, the name of the site being visited (SNI) is sent in plain text during connection establishment. Splitting this packet right at the beginning (e.g., at the 2nd byte) prevents the DPI from reading this header.

### `fragmentHttp`
* **Description:** Splits HTTP requests (Port 80) after the specified number of bytes.
* **Default:** `2`
* * **Value Range:**
  * `0`: **Disabled** (Turns off the feature).
* **Smart Detection:** This setting now includes **Persistent (Keep-Alive)** connection support. The engine intelligently detects every HTTP method (GET, POST, etc.) within the TCP stream and fragments them individually, ensuring bypass even for subsequent requests in the same connection.

### `reverseFragmentation` (Sending in Reverse Order)
* **Description:** Sends fragmented packets in reverse order (2nd fragment first, then the 1st fragment).
* **Logic:** The TCP protocol reassembles packets at the destination, so data is not corrupted. However, when the intermediate DPI device sees packets out of order, it gets confused and may allow them to pass without reassembling the content.
* **Default:** `true`

---

## 🛠️ Header Manipulation

These settings aim to break DPI signatures by changing the text format in the HTTP request. They are only effective on plain HTTP (unencrypted) sites.

### `mixHost`
* **Description:** Randomizes the case of the "Host" header.
* **Example:** `Host: example.com` -> `hOsT: example.com`
* **Logic:** Servers understand this, but DPI devices are sometimes sensitive only to the exact "Host" keyword.
* **Default:** `true`

### `hostNoSpace`
* **Description:** Removes the space after the colon in the header.
* **Example:** `Host: example.com` -> `Host:example.com`
* **Default:** `true`

### `additionalSpace`
* **Description:** Adds an extra space or tab character between the HTTP Method and the URI.
* **Default:** `false`

---

## 🎭 Fake Packet Settings

These settings inject "Fake" data packets into the stream to deceive the DPI system. These settings are **advanced**; incorrect configuration can completely cut off your internet connection.

### `fakePacketTTL` (Time To Live & Auto-Tracking)
* **Description:** Determines how many hops the fake packet will travel on the network.
* **Smart Distance Tracking (Auto-Learning):** The application has an embedded **TtlTracker**. This system automatically calculates the distance between you and the target server. The value entered here is a **"Fallback" (Starting)** value used when the system hasn't calculated the distance yet.
* **Default:** `5`
* **To Disable:** Setting this value to `0` completely **disables** fake packet transmission.
* **Logic:** The packet must pass through the DPI device but must expire before reaching the real server.

### `badSequence` (Bad Sequence Number)
* **Description:** Intentionally sends an incorrect TCP Sequence Number for the fake packet.
* **Risk:** While it enables DPI bypass on some ISPs, it can **completely break internet connectivity** on others.
* **Recommendation:** Keep it off by default. If other methods don't work, try turning it on; if your connection drops, turn it off again immediately.
* **Default:** `false`

### `badCheckSum` (Bad Checksum)
* **Description:** Sends an incorrect Checksum for the fake packet.
* **Logic:** DPI systems usually skip this check to save performance and process the packet; however, real servers reject the packet (which is exactly what we want).
* **Risk:** Just like `badSequence`, some network hardware (modems, routers) may automatically block packets with bad checksums, causing connection issues. Requires trial and error.
* **Default:** `false`

### `fakeRequestResendCount`
* **Description:** Determines how many times the fake packet is sent consecutively.
* **Default:** `1`
* **Recommended Range:** `1 - 3`
* **Warning:** Increasing this number too much (e.g., to 10) inflates your network traffic unnecessarily and may cause your modem to lock up or crash. Usually, `1` or at most `2` is sufficient.

---

## 🧪 How Should You Test? (Strategy Guide)

If the application is not working as expected, please try the following steps to troubleshoot:

1.  **Step 1 (Basic):** Only change the `fragmentHttps` value (`1`, `2`, `3`).
2.  **Step 2 (Ordering):** Set `reverseFragmentation` to `false`. Some networks do not like out-of-order packets.
3.  **Step 3 (Risk Zone):** If it still doesn't work, try setting `badCheckSum` or `badSequence` to `true`. **Caution:** If this cuts off your internet, turn it back off immediately.
4.  **Step 4 (DoH):** If you cannot ping the banned site at all or the IP address cannot be found, `isDoHEnabled` must be `true`.
