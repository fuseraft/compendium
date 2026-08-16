---
type: Integration
title: "Billing to Inventory: reservation release"
description: Billing Service notifies Inventory Service to release a reservation once payment settles.
tags: [billing, inventory]
status: stable
generated: { by: human:compendium-team, at: 2026-08-16T00:00:00Z }
sources:
  - id: integration-adr
    resource: https://wiki.example.com/architecture/adr-014-billing-inventory
    title: ADR-014 Billing/Inventory decoupling
---

# How it works

When the [Billing Service](/systems/billing-service.md) confirms payment, it
publishes a `payment.settled` event that the
[Inventory Service](/systems/inventory-service.md) consumes to release the
stock reservation held for that order.[^integration-adr]

[^integration-adr]: ADR-014 Billing/Inventory decoupling
