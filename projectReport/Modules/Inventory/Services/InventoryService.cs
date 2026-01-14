using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ProjectReport.Models.Inventory;

namespace ProjectReport.Services.Inventory
{
    public class InventoryService
    {
        private readonly IInventoryRepository _repo;

        public InventoryService(IInventoryRepository repo)
        {
            _repo = repo;
        }

        // Event fired when products or movements change so UI can refresh
        public event Action? InventoryUpdated;

        public List<Product> GetProducts() => _repo.LoadProducts();
        public List<InventoryMovement> GetMovements() => _repo.LoadMovements();

        public void UpsertProduct(Product product)
        {
            var products = _repo.LoadProducts();

            var existing = products.FirstOrDefault(p => p.Code.Equals(product.Code, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                products.Add(product);
            }
            else
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Category = product.Category;
                existing.Unit = product.Unit;
                existing.Status = product.Status;
            }

            _repo.SaveProducts(products);
            RaiseInventoryUpdated();
        }

        public void CreateTicketConsumed(Ticket ticket)
        {
            if (ticket.Type != TicketType.Consumed) throw new InvalidOperationException("Ticket type mismatch.");

            if (string.IsNullOrWhiteSpace(ticket.TicketId))
            {
                ticket.TicketId = Guid.NewGuid().ToString();
            }

            if (ticket.Lines != null && ticket.Lines.Count > 0)
            {
                foreach (var line in ticket.Lines) ProcessConsumedLine(ticket, line);
            }
            else
            {
                ProcessConsumedLine(ticket, ticket.Line);
            }

            RaiseInventoryUpdated();
        }

        private void ProcessConsumedLine(Ticket ticket, TicketLine line)
        {
            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            var p = products.FirstOrDefault(x => x.Code.Equals(line.ProductCode, StringComparison.OrdinalIgnoreCase));
            if (p == null)
            {
                // Optionally create product or throw error. Usually for consumption we want it to exist.
                p = new Product
                {
                    Code = line.ProductCode,
                    Name = string.IsNullOrWhiteSpace(line.ProductName) ? line.ProductCode : line.ProductName,
                    StockQty = 0,
                    Status = ProductStatus.Active
                };
                products.Add(p);
            }

            var before = p.StockQty;
            var qty = line.Quantity;
            if (qty <= 0) throw new InvalidOperationException("Quantity must be > 0.");

            p.StockQty -= qty; // Deduction

            var mv = new InventoryMovement
            {
                TicketId = ticket.TicketId,
                Date = ticket.Date,
                ProductCode = p.Code,
                ProductName = p.Name,
                Type = TicketType.Consumed,
                Quantity = qty,
                UnitPrice = p.CurrentUnitCost,
                OriginOrUse = line.Context,
                User = ticket.User,
                Observations = ticket.Observations,
                StockBefore = before,
                StockAfter = p.StockQty,
                Requisition = ticket.Requisition ?? ""
            };

            movements.Add(mv);

            _repo.SaveProducts(products);
            _repo.SaveMovements(movements);
        }

        public void CreateTicketReceived(Ticket ticket)
        {
            if (ticket.Type != TicketType.Received) throw new InvalidOperationException("Ticket type mismatch.");

            if (string.IsNullOrWhiteSpace(ticket.TicketId))
            {
                ticket.TicketId = Guid.NewGuid().ToString();
            }

            // Solo asignar nueva requisición si NO se proporcionó desde UI
            if (string.IsNullOrWhiteSpace(ticket.Requisition))
            {
                try
                {
                    ticket.Requisition = _repo.GetNextRequisition();
                }
                catch { }
            }

            if (ticket.Lines != null && ticket.Lines.Count > 0)
            {
                foreach (var line in ticket.Lines) ProcessReceivedLine(ticket, line);
            }
            else
            {
                ProcessReceivedLine(ticket, ticket.Line);
            }

            RaiseInventoryUpdated();
        }

        private void ProcessReceivedLine(Ticket ticket, TicketLine line)
        {
            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            var p = products.FirstOrDefault(x => x.Code.Equals(line.ProductCode, StringComparison.OrdinalIgnoreCase));
            if (p == null)
            {
                p = new Product
                {
                    Code = line.ProductCode,
                    Name = string.IsNullOrWhiteSpace(line.ProductName) ? line.ProductCode : line.ProductName,
                    StockQty = 0,
                    CurrentUnitCost = line.UnitPrice > 0 ? line.UnitPrice : 0,
                    Status = ProductStatus.Active
                };
                products.Add(p);
            }

            var before = p.StockQty;
            var qty = line.Quantity;
            if (qty <= 0) throw new InvalidOperationException("Quantity must be > 0.");

            p.StockQty += qty;

            if (line.UnitPrice > 0) p.CurrentUnitCost = line.UnitPrice;

            var mv = new InventoryMovement
            {
                TicketId = ticket.TicketId,
                Date = ticket.Date,
                ProductCode = p.Code,
                ProductName = p.Name,
                Type = TicketType.Received,
                Quantity = qty,
                UnitPrice = line.UnitPrice,
                OriginOrUse = line.Context,
                User = ticket.User,
                Observations = ticket.Observations,
                StockBefore = before,
                StockAfter = p.StockQty,
                Requisition = ticket.Requisition ?? ""
            };

            movements.Add(mv);

            _repo.SaveProducts(products);
            _repo.SaveMovements(movements);
        }

        public void CreateTicketReturned(Ticket ticket)
        {
            if (ticket.Type != TicketType.Returned) throw new InvalidOperationException("Ticket type mismatch.");

            // Asegurar TicketId único si no se proporcionó
            if (string.IsNullOrWhiteSpace(ticket.TicketId))
            {
                ticket.TicketId = Guid.NewGuid().ToString();
            }

            if (ticket.Lines != null && ticket.Lines.Count > 0)
            {
                foreach (var line in ticket.Lines) ProcessReturnedLine(ticket, line);
            }
            else
            {
                ProcessReturnedLine(ticket, ticket.Line);
            }

            RaiseInventoryUpdated();
        }

        private void ProcessReturnedLine(Ticket ticket, TicketLine line)
        {
            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            var p = products.FirstOrDefault(x => x.Code.Equals(line.ProductCode, StringComparison.OrdinalIgnoreCase));
            if (p == null)
            {
                p = new Product
                {
                    Code = line.ProductCode,
                    Name = string.IsNullOrWhiteSpace(line.ProductName) ? line.ProductCode : line.ProductName,
                    StockQty = 0,
                    CurrentUnitCost = line.UnitPrice > 0 ? line.UnitPrice : 0,
                    Status = ProductStatus.Active
                };
                products.Add(p);
            }

            var before = p.StockQty;
            var qty = line.Quantity;
            if (qty <= 0) throw new InvalidOperationException("Quantity must be > 0.");

            p.StockQty += qty;

            if (line.UnitPrice > 0) p.CurrentUnitCost = line.UnitPrice;

            // Determinar UnitPrice para el movimiento de devolución:
            // 1) Si la línea trae UnitPrice (>0) se usa.
            // 2) Si no, buscar el último movimiento Received para este producto y usar su UnitPrice (si >0).
            // 3) Si no hay Received con precio, usar p.CurrentUnitCost (fallback).
            double unitPriceToUse;
            if (line.UnitPrice > 0)
            {
                unitPriceToUse = line.UnitPrice;
            }
            else
            {
                var lastReceived = movements
                    .Where(m => string.Equals(m.ProductCode, line.ProductCode, StringComparison.OrdinalIgnoreCase)
                                && m.Type == TicketType.Received
                                && m.UnitPrice > 0)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefault();

                unitPriceToUse = lastReceived != null ? lastReceived.UnitPrice : p.CurrentUnitCost;
            }

            var mv = new InventoryMovement
            {
                TicketId = ticket.TicketId,
                Date = ticket.Date,
                ProductCode = p.Code,
                ProductName = p.Name,
                Type = TicketType.Returned,
                Quantity = qty,
                UnitPrice = unitPriceToUse,
                OriginOrUse = line.Context,
                User = ticket.User,
                Observations = ticket.Observations,
                StockBefore = before,
                StockAfter = p.StockQty,
                Requisition = ticket is { } ? (ticket.Requisition ?? "") : ""
            };

            movements.Add(mv);

            _repo.SaveProducts(products);
            _repo.SaveMovements(movements);
        }

        // Eliminar movimientos para un ticket (por TicketId).
        // Si removeLinkedByRequisition == true también elimina movimientos con la misma requisición (comportamiento legacy).
        public void DeleteMovementsForTicket(string ticketId, bool removeLinkedByRequisition = true)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) return;

            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            var toRemove = movements.Where(m => string.Equals(m.TicketId, ticketId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (toRemove.Count == 0) return;

            // Capturar requisiciones asociadas (solo si se va a eliminar vinculados)
            var removedRequisitions = new List<string>();
            if (removeLinkedByRequisition)
            {
                removedRequisitions = toRemove
                    .Select(m => m.Requisition)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            // Eliminar movimientos del ticket
            movements = movements.Except(toRemove).ToList();

            // Además eliminar movimientos vinculados por requisición (si removeLinkedByRequisition == true)
            if (removeLinkedByRequisition && removedRequisitions.Count > 0)
            {
                var linked = movements
                    .Where(m => !string.IsNullOrWhiteSpace(m.Requisition) && removedRequisitions.Contains(m.Requisition, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (linked.Count > 0)
                {
                    movements = movements.Except(linked).ToList();
                }
            }

            // Persistir movimientos actualizados
            _repo.SaveMovements(movements);

            // Recalcular y persistir stocks desde movimientos (evita desajustes)
            RecalculateAllProductStock();

            // Recompactar requisiciones y notificar
            try { _repo.CompactRequisitions(); } catch { }
            RaiseInventoryUpdated();
        }

        // Eliminar movimientos para una línea concreta de un ticket (TicketId + ProductCode)
        // No elimina otros movimientos con la misma requisición.
        public void DeleteMovementsForTicketLine(string ticketId, string productCode)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(productCode)) return;

            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            // Buscar movimientos que coincidan exactamente con ticketId + productCode
            var toRemove = movements.Where(m =>
                string.Equals(m.TicketId, ticketId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.ProductCode, productCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (toRemove.Count == 0) return;

            // Eliminar solo esos movimientos
            movements = movements.Except(toRemove).ToList();

            // Persistir movimientos actualizados
            _repo.SaveMovements(movements);

            // Recalcular stocks a partir de movimientos restantes
            RecalculateAllProductStock();

            // Recompactar requisiciones (si aplica) y notificar
            try { _repo.CompactRequisitions(); } catch { }
            RaiseInventoryUpdated();
        }

        // Recalcula StockQty de todos los productos a partir de los movimientos actuales
        public void RecalculateAllProductStock()
        {
            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            // Para cada producto, calcular net = sum(Received,Returned) - sum(otros)
            foreach (var p in products)
            {
                var mvFor = movements.Where(m => string.Equals(m.ProductCode, p.Code, StringComparison.OrdinalIgnoreCase));
                double received = mvFor.Where(m => m.Type == TicketType.Received).Sum(m => m.Quantity);
                double returned = mvFor.Where(m => m.Type == TicketType.Returned).Sum(m => m.Quantity);
                double others = mvFor.Where(m => m.Type != TicketType.Received && m.Type != TicketType.Returned).Sum(m => m.Quantity);

                // Interpretación: Received + Returned incrementan stock; "others" decrementan
                p.StockQty = received + returned - others;
            }

            _repo.SaveProducts(products);

            // Remove orphaned products: products with no movements and zero stock (clean-up)
            try
            {
                var orphaned = products
                    .Where(p => !movements.Any(m => string.Equals(m.ProductCode, p.Code, StringComparison.OrdinalIgnoreCase))
                                && p.StockQty == 0
                                && p.Status == ProductStatus.Active)
                    .ToList();

                if (orphaned.Count > 0)
                {
                    var remaining = products.Except(orphaned).ToList();
                    _repo.SaveProducts(remaining);
                    products = remaining;
                }
            }
            catch
            {
                // don't break recalculation on cleanup failure
            }

            // Notificar a la UI que los productos han cambiado
            try { RaiseInventoryUpdated(); } catch { }
        }

        // Helper: always raise event on UI thread so subscribers can update safely
        private void RaiseInventoryUpdated()
        {
            try
            {
                var app = Application.Current;
                if (app != null && app.Dispatcher != null)
                {
                    app.Dispatcher.Invoke(() => InventoryUpdated?.Invoke());
                }
                else
                {
                    InventoryUpdated?.Invoke();
                }
            }
            catch
            {
                InventoryUpdated?.Invoke();
            }
        }

        // Añadir este método a InventoryService (mantén el resto sin cambios)
        public void DeleteMovementById(string movementId)
        {
            if (string.IsNullOrWhiteSpace(movementId)) return;

            var movements = _repo.LoadMovements();
            var toRemove = movements.Where(m => string.Equals(m.MovementId, movementId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (toRemove.Count == 0) return;

            movements = movements.Except(toRemove).ToList();

            _repo.SaveMovements(movements);

            // Recalcular stocks desde movimientos restantes
            RecalculateAllProductStock();

            try { _repo.CompactRequisitions(); } catch { }
            RaiseInventoryUpdated();
        }

        // New: explicit remove product from catalog by code (used when user wants to delete product entirely)
        public void DeleteProductByCode(string productCode)
        {
            if (string.IsNullOrWhiteSpace(productCode)) return;

            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            // Prevent deleting if there are still movements referencing this product
            bool hasMovements = movements.Any(m => string.Equals(m.ProductCode, productCode, StringComparison.OrdinalIgnoreCase));
            if (hasMovements) return;

            var toRemove = products.Where(p => string.Equals(p.Code, productCode, StringComparison.OrdinalIgnoreCase)).ToList();
            if (toRemove.Count == 0) return;

            var remaining = products.Except(toRemove).ToList();
            _repo.SaveProducts(remaining);

            try { _repo.CompactRequisitions(); } catch { }
            RaiseInventoryUpdated();
        }

        // New: remove all movements that match a given requisition
        public void DeleteMovementsByRequisition(string requisition)
        {
            if (string.IsNullOrWhiteSpace(requisition)) return;

            var movements = _repo.LoadMovements();
            var toRemove = movements
                .Where(m => !string.IsNullOrWhiteSpace(m.Requisition) &&
                            string.Equals(m.Requisition, requisition, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (toRemove.Count == 0) return;

            movements = movements.Except(toRemove).ToList();

            _repo.SaveMovements(movements);

            // Recalculate stocks from remaining movements
            RecalculateAllProductStock();

            try { _repo.CompactRequisitions(); } catch { }
            RaiseInventoryUpdated();
        }
    }
}
