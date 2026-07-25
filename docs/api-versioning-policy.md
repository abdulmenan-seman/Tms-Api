# TMS API Versioning Policy

This document defines the strict versioning rules for the Training Management System (TMS) API. All engineering teams must adhere to these policies to ensure absolute client uptime and zero friction during system updates.

## 1. Definition of Changes

### Breaking Changes (Requires a Major Version Bump)
Any change that forces existing clients to modify their code or causes deployment failures is a breaking change:
* Removing or renaming a JSON field/property in a request body or response payload.
* Changing an HTTP status code returned under specific conditions (e.g., changing 404 to 400).
* Tightening validation constraints (e.g., adding a new required field or shrinking string character limits).
* Changing the default sort order, data grouping, or pagination structures of list endpoints.

### Additive / Non-Breaking Changes (Allowed in Current Version)
Changes that can be deployed safely without breaking backward compatibility:
* Adding a new optional field to a response payload.
* Exposing a completely new API endpoint or resource route.
* Introducing a new optional query parameter with a fallback default value.

## 2. Deprecation and Sunset Strategy

* **Version Lifespan:** Once a new major version (e.g., V2) ships, the previous version (V1) enters a mandatory **6-month minimum sunset window**. This guarantees that rural training centers on quarterly maintenance cycles have ample time to update.
* **Protocol-Level Signaling:** From day one of a new version release, the old version will emit three HTTP response headers on every single request:
  * `Deprecation: true`
  * `Sunset: <RFC 7231 Date>` (Target shutdown deadline)
  * `Link: <Successor-URL>; rel="successor-version"`
* **Communication Matrix:** Every deprecation lifecycle requires a CHANGELOG update, an automated email notification to all active API key holders, and a calendar invite sent to internal engineering stakeholders for the V1 shutdown day.

## 3. Client Migration Freedom
* **Skipping Versions:** Clients are explicitly permitted to migrate directly from V1 to V3. They are not forced to integrate or deploy intermediate versions (like V2) if their operational pipeline allows a direct leap.