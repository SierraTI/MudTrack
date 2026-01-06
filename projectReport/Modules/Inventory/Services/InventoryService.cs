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

        public void CreateTicketReceived(Ticket ticket)
        {
            if (ticket.Type != TicketType.Received) throw new InvalidOperationException("Ticket type mismatch.");

            // Asegurar TicketId único si no se proporcionó (evita agrupaciones accidentales)
            if (string.IsNullOrWhiteSpace(ticket.TicketId))
            {
                ticket.TicketId = Guid.NewGuid().ToString();
            }

            try
            {
                ticket.Requisition = _repo.GetNextRequisition();
            }
            catch { }

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

            var mv = new InventoryMovement
            {
                TicketId = ticket.TicketId,
                Date = ticket.Date,
                ProductCode = p.Code,
                ProductName = p.Name,
                Type = TicketType.Returned,
                Quantity = qty,
                UnitPrice = line.UnitPrice > 0 ? line.UnitPrice : p.CurrentUnitCost,
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

        // Eliminar movimientos para un ticket (por TicketId) y recalcular stocks desde movimientos
        public void DeleteMovementsForTicket(string ticketId)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) return;

            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

                var toRemove = movements.Where(m => string.Equals(m.TicketId, ticketId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (toRemove.Count == 0) return;

            // Capturar requisiciones asociadas
            var removedRequisitions = toRemove
                .Select(m => m.Requisition)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Eliminar movimientos del ticket
            movements = movements.Except(toRemove).ToList();

            // Además eliminar movimientos vinculados por requisición (si existen)
            if (removedRequisitions.Count > 0)
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
    }
}
