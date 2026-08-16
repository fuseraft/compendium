---
type: System
title: Billing Service
description: Owns invoicing and payment collection for all customer orders.
tags: [billing, finance]
status: stable
generated: { by: human:compendium-team, at: 2026-08-16T00:00:00Z }
---

# Overview

The Billing Service issues invoices, collects payment via the payment
processor, and is the system of record for whether an order has been paid.

# Integrations

Once payment settles, Billing publishes an event consumed by
[Inventory](inventory-service.md), described in the
[billing-to-inventory integration](../integrations/billing-to-inventory.md).
