---
type: Business Process
title: Order Fulfillment
description: End-to-end flow from a placed order to a shipped package.
tags: [orders, fulfillment]
status: stable
generated: { by: human:compendium-team, at: 2026-08-16T00:00:00Z }
---

# Steps

1. Customer places an order; [Inventory](/systems/inventory-service.md)
   reserves the stock.
2. [Billing Service](/systems/billing-service.md) collects payment.
3. On settlement, the
   [billing-to-inventory integration](/integrations/billing-to-inventory.md)
   releases the reservation.
4. Warehouse picks, packs, and ships the order.
