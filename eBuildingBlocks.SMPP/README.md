# SmppLite.Server

A **lightweight, reusable SMPP server engine** for .NET, designed to be published as a NuGet package and embedded into multiple applications.

`SmppLite.Server` handles **SMPP protocol mechanics only** (TCP, PDUs, sessions, long SMS parsing) and exposes **clean extension points** so each consuming application can implement its own business logic, persistence, routing, and operator connectivity.

---

## Features

- SMPP 3.4 server-side implementation
- Supports:
  - `bind_transceiver`
  - `submit_sm`
  - `submit_sm_resp`
  - `enquire_link`
  - `unbind`
- Long SMS detection:
  - SAR TLVs
  - UDH concatenation (8-bit and 16-bit)
- Multi-IP and multi-port listening
- High-throughput friendly (500+ msg/s)
- Fully async / non-blocking
- Zero external dependencies
- Designed for NuGet reuse across multiple solutions

---

## What This Package Does (and Does NOT)

### ✅ Included
- TCP listener and connection handling
- SMPP framing and PDU parsing
- Session state management
- SMPP error handling
- Long SMS metadata extraction (SAR / UDH)
- Extension hooks for authentication, message handling, and policies

### ❌ Not Included (by design)
- Database access
- RabbitMQ / Kafka
- Operator SMPP connections
- Routing logic
- Configuration files
- Business rules

All of the above are implemented **by the consuming application**, not the NuGet.

---

## Installation

```bash
dotnet add package SmppLite.Server
