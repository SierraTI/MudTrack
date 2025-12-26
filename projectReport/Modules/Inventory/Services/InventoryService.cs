using System;
using System.Collections.Generic;
using System.Linq;
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
                // StockQty y CurrentUnitCost pueden iniciar en 0
                products.Add(product);
            }
            else
            {
                // OJO: aquí NO tocamos stock.
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Category = product.Category;
                existing.Unit = product.Unit;
                existing.Status = product.Status;
            }

            _repo.SaveProducts(products);
            InventoryUpdated?.Invoke();
        }

        public void CreateTicketReceived(Ticket ticket)
        {
            if (ticket.Type != TicketType.Received) throw new InvalidOperationException("Ticket type mismatch.");

            if (ticket.Lines != null && ticket.Lines.Count > 0)
            {
                foreach (var line in ticket.Lines)
                {
                    ProcessReceivedLine(ticket, line);
                }
            }
            else
            {
                ProcessReceivedLine(ticket, ticket.Line);
            }

            InventoryUpdated?.Invoke();
        }

        private void ProcessReceivedLine(Ticket ticket, TicketLine line)
        {
            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            var p = products.FirstOrDefault(x => x.Code.Equals(line.ProductCode, StringComparison.OrdinalIgnoreCase));

            // If product does not exist yet, create it so inventory is populated by tickets
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

            // “Último costo” como referencia (histórico queda en movimiento)
            if (line.UnitPrice > 0)
                p.CurrentUnitCost = line.UnitPrice;

            movements.Add(new InventoryMovement
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
                StockAfter = p.StockQty
            });

            _repo.SaveProducts(products);
            _repo.SaveMovements(movements);
        }

        public void CreateTicketReturned(Ticket ticket)
        {
            if (ticket.Type != TicketType.Returned) throw new InvalidOperationException("Ticket type mismatch.");

            if (ticket.Lines != null && ticket.Lines.Count > 0)
            {
                foreach (var line in ticket.Lines)
                {
                    ProcessReturnedLine(ticket, line);
                }
            }
            else
            {
                ProcessReturnedLine(ticket, ticket.Line);
            }

            InventoryUpdated?.Invoke();
        }

        private void ProcessReturnedLine(Ticket ticket, TicketLine line)
        {
            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            var p = products.FirstOrDefault(x => x.Code.Equals(line.ProductCode, StringComparison.OrdinalIgnoreCase));

            // If product does not exist yet, create it so inventory is populated by tickets
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

            // Returned increases stock
            p.StockQty += qty;

            // If a unit price is provided on return, optionally update current unit cost
            if (line.UnitPrice > 0)
                p.CurrentUnitCost = line.UnitPrice;

            movements.Add(new InventoryMovement
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
                StockAfter = p.StockQty
            });

            _repo.SaveProducts(products);
            _repo.SaveMovements(movements);
        }
    }
}
