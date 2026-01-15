// Nueva propiedad para controlar si se mantiene el borrador después de guardar
private bool _keepDraftAfterSave = false;
public bool KeepDraftAfterSave
{
    get => _keepDraftAfterSave;
    set => SetProperty(ref _keepDraftAfterSave, value);
}

private void Save()
{
    Error = "";

    if (Lines.Count == 0)
    {
        Error = "No hay líneas para guardar.";
        return;
    }

    // Validar cada línea antes de persistir
    for (int i = 0; i < Lines.Count; i++)
    {
        var ln = Lines[i];
        if (string.IsNullOrWhiteSpace(ln.ProductName) && string.IsNullOrWhiteSpace(ln.ProductCode))
        {
            Error = $"Línea {i + 1}: producto requerido.";
            return;
        }
        if (ln.Quantity <= 0)
        {
            Error = $"Línea {i + 1}: la cantidad debe ser mayor que 0.";
            return;
        }
    }

    // Ensure all products exist in catalog before creating ticket
    var currentProducts = _service.GetProducts();
    foreach (var ln in Lines)
    {
        var code = (ln.ProductCode ?? "").Trim();
        var name = (ln.ProductName ?? "").Trim();

        Product? existing = null;
        if (!string.IsNullOrEmpty(code))
            existing = currentProducts.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));

        if (existing == null && !string.IsNullOrEmpty(name))
            existing = currentProducts.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            // create new product and persist
            var newProd = new Product
            {
                Code = string.IsNullOrWhiteSpace(code) ? GenerateCodeFromName(name) : code,
                Name = string.IsNullOrWhiteSpace(name) ? newProdCodePlaceholder(code) : name,
                Description = string.Empty,
                Category = Category ?? string.Empty,
                Unit = string.IsNullOrWhiteSpace(Unit) ? "Each" : Unit,
                StockQty = 0,
                CurrentUnitCost = ln.UnitPrice,
                Status = ProductStatus.Active
            };

            _service.UpsertProduct(newProd);

            currentProducts = _service.GetProducts();
            ln.ProductCode = newProd.Code;
            ln.ProductName = newProd.Name;
        }
        else
        {
            ln.ProductCode = existing.Code;
            ln.ProductName = existing.Name;
        }
    }

    var ticket = new Ticket
    {
        Type = TicketType.Received,
        Date = DateTime.Now,
        User = User,
        Observations = Observations,
        Requisition = Requisition ?? string.Empty,
        Lines = Lines.ToList()
    };

    try
    {
        if (!string.IsNullOrWhiteSpace(EditingTicketId))
        {
            _service.DeleteMovementsForTicket(EditingTicketId, removeLinkedByRequisition: false);
            ticket.TicketId = EditingTicketId;
        }
        else if (!string.IsNullOrWhiteSpace(EditingRequisition))
        {
            _service.DeleteMovementsByRequisition(EditingRequisition);
            ticket.Requisition = EditingRequisition;
        }

        // Persistir
        _service.CreateTicketReceived(ticket);

        // Ahora recargamos desde repo para que la tabla muestre lo guardado
        var requisitionToLoad = ticket.Requisition;
        if (string.IsNullOrWhiteSpace(requisitionToLoad))
        {
            // si repo asignó la requisición internamente, buscar por TicketId
            var saved = _service.GetMovements().Where(m => m.TicketId == ticket.TicketId).ToList();
            if (saved.Count > 0)
                requisitionToLoad = saved.First().Requisition;
        }

        if (!string.IsNullOrWhiteSpace(requisitionToLoad))
        {
            LoadByRequisition(requisitionToLoad);
            Error = "Ticket guardado correctamente y recargado en la tabla.";
            EditingTicketId = ticket.TicketId;
            EditingRequisition = requisitionToLoad;
        }
        else
        {
            // fallback: conservar borrador tal cual y notificar
            Error = "Ticket guardado correctamente.";
            EditingTicketId = ticket.TicketId;
        }
    }
    catch (Exception ex)
    {
        Error = "Error al guardar: " + ex.Message;
    }
}