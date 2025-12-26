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
        }

        public void CreateTicketReceived(Ticket ticket)
        {
            if (ticket.Type != TicketType.Received) throw new InvalidOperationException("Ticket type mismatch.");

            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            var p = products.FirstOrDefault(x => x.Code.Equals(ticket.Line.ProductCode, StringComparison.OrdinalIgnoreCase));
            if (p == null) throw new InvalidOperationException("Product not found.");

            var before = p.StockQty;
            var qty = ticket.Line.Quantity;
            if (qty <= 0) throw new InvalidOperationException("Quantity must be > 0.");

            p.StockQty += qty;

            // “Último costo” como referencia (histórico queda en movimiento)
            if (ticket.Line.UnitPrice > 0)
                p.CurrentUnitCost = ticket.Line.UnitPrice;

            movements.Add(new InventoryMovement
            {
                TicketId = ticket.TicketId,
                Date = ticket.Date,
                ProductCode = p.Code,
                ProductName = p.Name,
                Type = TicketType.Received,
                Quantity = qty,
                UnitPrice = ticket.Line.UnitPrice,
                OriginOrUse = ticket.Line.Context,
                User = ticket.User,
                Observations = ticket.Observations,
                StockBefore = before,
                StockAfter = p.StockQty
            });

            _repo.SaveProducts(products);
            _repo.SaveMovements(movements);
        }

        public void CreateTicketConsumed(Ticket ticket)
        {
            if (ticket.Type != TicketType.Consumed) throw new InvalidOperationException("Ticket type mismatch.");

            var products = _repo.LoadProducts();
            var movements = _repo.LoadMovements();

            var p = products.FirstOrDefault(x => x.Code.Equals(ticket.Line.ProductCode, StringComparison.OrdinalIgnoreCase));
            if (p == null) throw new InvalidOperationException("Product not found.");

            var before = p.StockQty;
            var qty = ticket.Line.Quantity;
            if (qty <= 0) throw new InvalidOperationException("Quantity must be > 0.");

            // VALIDACIÓN CLAVE
            if (p.StockQty < qty)
                throw new InvalidOperationException($"Insufficient stock. Available: {p.StockQty}, required: {qty}");

            p.StockQty -= qty;

            movements.Add(new InventoryMovement
            {
                TicketId = ticket.TicketId,
                Date = ticket.Date,
                ProductCode = p.Code,
                ProductName = p.Name,
                Type = TicketType.Consumed,
                Quantity = qty,
                UnitPrice = p.CurrentUnitCost, // o 0 si no quieres costeo en consumo
                OriginOrUse = ticket.Line.Context,
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
