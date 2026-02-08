# Anatomy of Deep Packet Inspection (DPI) Systems: A Case Study on TLS Handshake Manipulation

*(Research Date: December 2025)*

---

## 1. Introduction
During the development of the `IHateDPI` project, extensive data collection and analysis were conducted on Deep Packet Inspection (DPI) systems used across various Internet Service Provider (ISP) infrastructures. Intensive field tests and stress tests revealed the operational logic and structural trade-offs of these systems. This technical report examines which protocol manipulation methods ensure network traffic continuity, which fail, and the underlying technical reasons for these outcomes.

## 2. Mechanics of DPI Systems
Field research indicates that current DPI mechanisms focus on capturing the **"Client Hello"** packet, the first step of the TLS (Transport Layer Security) handshake, rather than analyzing all network traffic. This packet is the sole distinguishing data set available to DPI systems.

### 2.1. The Role of the "Client Hello" Packet
Simply put, "Client Hello" is the client's (browser/device) request to initiate encrypted communication with the server. Within this packet lies the data critical for DPI systems: the **SNI (Server Name Indication)**.

### 2.2. SNI and Filtering Logic
The SNI field contains the target domain name (e.g., `google.com`) in plaintext. DPI devices filter "Client Hello" packets flowing through the network stream and read the SNI value. If this value matches a rule set (blacklist) defined in the system, the connection is terminated via **TCP Reset** or **Packet Drop**.

## 3. Modern Web Architecture: The Centralization Paradox
The current web ecosystem is highly centralized on massive Content Delivery Networks (CDNs) like Cloudflare, Google, and Amazon. While such monopolization might seem like a risk normally, in the context of DPI-based filtering mechanisms, it turns into a **technical deadlock** for controlling systems.

In past network topologies, IP addresses were like **detached houses**; typically, there was a single server behind each address. However, in modern cloud architecture, IP addresses have transformed into massive **skyscrapers** housing thousands of apartments (websites).

Today, blocking access to a single IP address means making thousands of unrelated sites, e-commerce platforms, and corporate systems inaccessible as well (Collateral Damage). This risk has rendered IP-based blocking impossible and forced DPI systems to filter traffic based on **SNI content** rather than IP addresses.

## 4. Protocol Obfuscation Methods
To avoid degrading network throughput, DPI systems cannot deeply inspect every packet. They typically look for a specific signature (e.g., is the first byte `0x16`?). If there is a match, the SNI is read; otherwise, the flow is released. Once the TLS Handshake is complete, traffic is encrypted, and DPI tracking technically ends.

In this context, the entire strategy revolves around bypassing the DPI's inspection mechanism and delivering the "Client Hello" packet to the server.

### 4.1. Method 1: Fake Client Hello Injection
Field tests show that the most stable method is presenting a deceptive packet to the system before the real packet.

* **Mechanism:** Immediately before the real request, a fake "Client Hello" packet containing a benign domain name (e.g., `google.com`) is injected.
* **Result:**
    1.  The DPI system analyzes the fake packet and marks the TCP flow as "Safe."
    2.  Since the system records the state, the real "Client Hello" packet following immediately after passes through without inspection.
    3.  The server rejects the fake packet but continues communication with the real packet.

### 4.2. Method 2: TCP Segmentation and "Out-of-Order" Manipulation
Standard TCP fragmentation methods have lost effectiveness due to the "Packet Reassembly" capabilities of modern DPI devices. In this research, a hybrid method targeting the DPI buffer was developed.

* **Mechanism:**
    1.  The "Client Hello" packet is fragmented. The first fragment (Header) is sent to the server.
    2.  Immediately after, **Garbage Data** possessing the TCP Sequence Number of the second fragment is sent.
    3.  **DPI Behavior:** The DPI combines the first fragment with this garbage data. The resulting data is meaningless and does not match rule sets.
    4.  **Server Behavior:** The server ignores this data as it is erroneous (checksum failure) or meaningless according to the TCP protocol.
    5.  When the real second fragment is sent, the server combines it with the first fragment, and the session is established.

## 5. Case Study: Type-B Network Infrastructure (Strict Protocol Enforcement)
Field tests identified that in a specific ISP infrastructure (referred to as **"Type-B"** in this report), the "Garbage Data" method mentioned above failed. A deep analysis process was initiated to decipher the system's behavior.

### 5.1. Stress Test and Anomaly Detection
In the Type-B network, standard-sized garbage data had no effect on the system. To measure the system's reaction and identify potential buffer overflow vulnerabilities, the size of the injected garbage data was incrementally increased (Payload Inflation).

The expectation was that once the data size exceeded a certain threshold, the system would be overwhelmed and cut the traffic (Fail-Closed). However, a surprising state of **"Silent Discard"** was observed in the Type-B infrastructure. No matter how much the data size was increased, the connection was not severed, but the manipulation was also unsuccessful. This proved that the system was not performing a simple size check.

### 5.2. Critical Discovery: Structural Integrity Check
Reverse engineering was applied to fake packets to understand the system's logic; starting with empty packets, RFC-compliant packets were injected step-by-step.

**Findings:**
The Type-B DPI system checks not only for size or simple signatures but also for **Content Consistency**.

When the TCP payload is fragmented and garbage data is injected, the DPI system detects that the resulting data does not comply with **TLS Record Layer** standards (Malformed Packet). The system classifies this data not as a "manipulation attempt," but directly as **"Noise/Corrupt Data."** The DPI does not process data labeled as "corrupt" and does not record it in the State Table.

**Conclusion:** The system only processes realistic "Client Hello" packets that strictly comply with RFC standards and have unbroken structural integrity. Therefore, random data injection is ineffective in Type-B networks; manipulation is only possible with valid protocol structures (Valid TLS Structure).

## 6. Conclusion and Future Projection: ECH (Encrypted Client Hello)
This research proves that DPI bypass methods are not universal; they vary according to the configuration of network devices (Stateless vs. Deep Stateful).

Currently, the most stable method is **Fake (Valid) Client Hello** injection. However, the ultimate protocol-level solution is the **Encrypted Client Hello (ECH)** standard developed by the IETF. ECH aims to make DPI analysis technically impossible by encrypting the SNI information. The `IHateDPI` project will continue to monitor the ECH adaptation process.

---

### Disclaimer
*This document and the associated GitHub repository are prepared for educational purposes to understand the logic of network protocols (TCP/IP, TLS) and conduct cybersecurity research. The information provided is intended for network administrators to test their infrastructures and analyze protocol behaviors.*
