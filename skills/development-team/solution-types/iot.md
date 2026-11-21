# Solution type: Internet of Things (IoT)

**When it applies:** the solution connects physical devices (machines, sensors, gateways,
controllers) that send data (telemetry, events, alarms) to a backend, or receive commands,
configuration or software updates from it.

These rules add to `RULES.md` and to each role's own rules, or make them stricter. They never
weaken an absolute rule. Rules for one organization's own IoT platform belong in that project's
own rules (CLAUDE.md, AGENTS.md), not here. Every role applies both "All roles" sections and its
own section.

## All roles

- **Devices are unreliable and live long.** Design and test for devices that go offline,
  reconnect, send late, duplicate or out-of-order messages, run old firmware for years, and have
  a wrong clock.
- **Scale is part of every decision.** State the number of devices, the messages per device per
  minute and the message size it is designed for, now and with the expected growth.
- **Units and time are explicit.** Every measured value has its unit, and every timestamp is in
  Coordinated Universal Time (UTC) with its source: the device's time or the time it was received.
- **One vocabulary:** device, gateway, telemetry, event, alarm, command, configuration (desired
  and reported state) and firmware mean the same everywhere; record them in the glossary.
- **No real device data outside production** when it reveals customers, locations or people;
  use simulated devices or masked data.

## All roles: lessons learned in practice

- **Convert units in one place only:** the platform that owns the telemetry. APIs and apps pass
  values on with their unit and ask for the user's measurement system; they never keep their own
  conversion tables. A missing conversion is a request to the platform's owner.
- **Read device and asset data only through the owner's published contracts** (a read API or
  shared views), never through its raw tables or JSON layout, not even as a temporary fallback.
  Define such contracts once per entity (machine, device, organization, license) and share them
  between consumers such as the app, the portal and a customer API; each consumer does its own
  mapping. A missing field is a request to the owner, not a workaround.
- **Keep the machine, its controller and its gateway apart.** Model the equipment, the
  controller that reports its data, and the gateway or communication device as separate
  entities, and never fill machine properties from the gateway.
- **Test fleet queries at production size.** Test data is often far smaller than production, so
  a query that is fast there can time out in production. Page fleet lists on an indexed column
  (for example the device-to-organization link), never on unindexed JSON fields, and count the
  total only for the first page.
- **Line up the timeouts.** Each inner timeout is shorter than the one around it (database,
  then API, then gateway or front door, then client), so the real error reaches the caller
  instead of a generic gateway timeout. Clients that poll wait for the previous answer before
  they ask again.
- **Make authorization fast at fleet scale.** Measure the lookup of a user's organizations and
  devices with the largest real organization trees. When that lookup times out, every endpoint
  fails for that user.
- **Caches of authorized data are per user.** When the source filters data per user, the cache
  key contains the user (or their exact authorization scope), and building a key without a user
  fails instead of falling back to a shared entry. Add tests where two users must not see each
  other's cached data.
- **"Not available yet" is not "no value" and not zero.** Decide per field how the contract shows
  a value that is expected but has no source yet, one that does not exist, and a real zero, and
  check how every client uses that field (for example clients that send it back).
- **Timestamps in contracts always carry their offset** (ISO 8601 with `Z` or `+hh:mm`).
  Configure generated clients to send it, and add a contract test for date parameters.
- **"Online" is not proof of fresh data.** A connection flag can stay online while no data
  arrives. Base freshness on the time of the last message, and alert when ingestion stops for a
  device or the fleet. This applies to test environments too, where data often stops unnoticed.
- **Configuration and secret names are identical in every environment and region.** Check for
  drift between environments, and never let a placeholder value (such as "TBD") reach a deployed
  environment.
- **Plan separate clouds and regions from the start.** A sovereign cloud (for example Azure in
  China) has its own tenant, identities, secrets and endpoints. An identity in one tenant cannot
  get permissions in another.
- **Trace end to end.** One W3C trace id (`traceparent`) goes from the app through the API to
  the platform; every service logs it and the API returns it in a response header. The backend
  decides about sampling, never the client.
- **A request the client cancelled is not an error.** Return 499, do not log it as an error, and
  stop the work that belonged to it.

## Architect

- The architecture diagram shows the devices and gateways, how they connect (protocols,
  networks), the ingestion endpoint (IoT hub or message broker), stream processing, storage, the
  API and the clients, with the trust boundary between devices and the cloud.
- **Device identity and provisioning:** one identity per device (certificates, stored in hardware
  such as a Trusted Platform Module where possible); how devices are registered in the factory or
  the field, moved to another customer, and taken out of service.
- **Communication:** the protocol per path (for example Message Queuing Telemetry Transport
  (MQTT), Advanced Message Queuing Protocol (AMQP) or HTTPS) and why, from device to cloud and
  from cloud to device, and what happens when the connection is lost: store and forward on the
  device or gateway, buffer size, and which data may be dropped first.
- **Message contract:** a versioned schema per message type, with the device and firmware version
  in every message. The backend accepts every version still in the field. A breaking change is a
  new version, never a new meaning for an existing field.
- **Duplicates, order and late data:** processing is idempotent (by message id), ordered per
  device where it matters, and handles late data on purpose (event time versus processing time).
- **Data:** time-series storage with retention per tier (raw, aggregated), aggregation and
  downsampling, and the main queries (last value, history per device, fleet-wide), with their
  volumes and cost. This is part of the database architecture.
- **Edge or cloud:** what is processed on the device or gateway (filtering, aggregation, local
  alarms) and why.
- **Commands and configuration:** desired and reported state, acknowledgement, timeouts, what
  happens for an offline device, and who may send which command, as part of the authorization
  matrix. A command to a physical machine can be a safety risk: design confirmation and limits.
- **Firmware updates over the air:** in waves, signed, with health checks and rollback, and who
  may start them.
- **Device ownership:** which customer or organization owns which device, how that changes, and
  how the authorization matrix uses it.
- **Monitoring:** connection status per device, message rates, ingestion delay and rejected
  messages, with alarms when a device or the whole fleet goes silent.
- **Cost per device per month** at the designed scale (messages, storage, compute), recorded as a
  decision with the numbers.

## Security officer

- Add the Open Web Application Security Project (OWASP) Internet of Things Top 10 to the
  baseline.
- Every device has its own credentials: no shared keys, no default passwords, no secrets in
  firmware images. Credentials can be rotated and revoked, for example for a stolen device.
- Communication is encrypted with Transport Layer Security (TLS), and the device validates the
  server certificate. A device can reach only the endpoints it needs.
- A device can only send its own data and receive its own commands; one compromised device can
  never read or affect other devices.
- Firmware is signed and checked on the device before it is installed; secure boot where the
  hardware allows it.
- Every command is authorized and logged: who sent which command to which device, and when.
- Data from devices is untrusted input: validate size, schema and value ranges, and limit the
  message rate per device so one device cannot flood the ingestion.
- Check physical access too: debug ports, local interfaces, and the data stored on the device.

## Backend developer

- Ingestion is idempotent: deduplicate by message id, never assume order, and handle late data as
  designed.
- Accept every message schema version still in the field. Send invalid messages to a
  dead-letter queue with the reason; never drop them silently.
- Handle peaks, such as the whole fleet reconnecting after an outage, with queues, batching and
  scaling limits. A message that was acknowledged is never lost.
- Track every command: sent, acknowledged, failed or expired, with a timeout, and handle offline
  devices as designed.
- Store raw values as received, with their unit and timestamp; store derived values separately.
- Log the device id and message id for tracing, without personal data.

## Frontend developer

- Show how fresh the data is: last seen and connection status per device. Never show an old value
  as if it were current.
- Show units and time zones explicitly; convert from UTC to the user's time zone only for display.
- A command from the user interface shows its status (pending, acknowledged, failed) and asks for
  confirmation when it affects a physical machine.
- Large fleets: paging, filtering and aggregated views; never load all devices at once.

## Tester

- Use a device simulator that sends every message version still in the field, including wrong
  clocks, duplicates, out-of-order and invalid messages.
- Connectivity: a device goes offline and comes back (buffered data arrives, nothing is lost or
  doubled); the backend has an outage and the whole fleet reconnects.
- Load: the designed fleet size and message rate, and the expected peak, in an approved
  environment. Measure the delay from device to API.
- Security: a device cannot send as another device or receive another device's commands, and
  revoked credentials are rejected.
- Commands: offline device, timeout, acknowledgement, and a user without permission.
- Firmware updates, when in scope: interrupted update, rollback, and a wrong signature that is
  rejected.

## Analyst (optional)

- Analyse the device fleet: device types, numbers, firmware versions in the field, connectivity,
  and who owns and who operates the devices.
- Find out which questions users ask about their devices: which data, how fresh, which alarms,
  which actions on the machine, and the business value of each.

## Designer (optional)

- Device status is visible at a glance: connected, offline, alarm, last seen.
- Designs work for one device and for thousands.
- Commands that affect a physical machine are hard to trigger by mistake.

## Sanity agent

- Check that the data collected is data the customer will use. Collecting everything "just in
  case" costs money and can harm privacy.
- Check that the cost per device per month fits the budget.
