---
type: System
title: Inventory Service
description: Tracks stock levels and reserves inventory for pending orders.
tags: [inventory, warehouse]
status: stable
generated: { by: human:compendium-team, at: 2026-08-16T00:00:00Z }
---

# Overview

The Inventory Service tracks on-hand stock per SKU and holds a reservation
against pending orders so the same unit can't be sold twice. Reservations
are released either when payment settles (see
[billing-to-inventory](/integrations/billing-to-inventory.md)) or when an
order is cancelled.
