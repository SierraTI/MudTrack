using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ProjectReport.Models.Inventory;
using ProjectReport.Services.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class UsageSpecificationItem : INotifyPropertyChanged
    {
        private string _usageType = "Fluid"; // "Fluid" or "Other Activity"
        public string UsageType
        {
            get => _usageType;
            set
            {
                if (_usageType != value)
                {
                    _usageType = value;
                    OnPropertyChanged(nameof(UsageType));
                    OnPropertyChanged(nameof(IsFluidType));
                    OnPropertyChanged(nameof(IsActivityType));
                    // Reset detail when type changes
                    Movement.OriginOrUse = "";
                    Movement.IsAddedToFluid = (value == "Fluid");
                }
            }
        }

        public bool IsFluidType => UsageType == "Fluid";
        public bool IsActivityType => UsageType == "Other Activity";

        public InventoryMovement Movement { get; }

        public UsageSpecificationItem(InventoryMovement movement)
        {
            Movement = movement;
            // Infer type from IsAddedToFluid if already set
            UsageType = movement.IsAddedToFluid ? "Fluid" : "Other Activity";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class UsageSpecificationViewModel : INotifyPropertyChanged
    {
        private readonly InventoryService _service;
        private readonly UsageBalanceItem _balanceItem;

        public string ProductName => _balanceItem.ProductName;
        public string Unit => _balanceItem.Unit;

        public ObservableCollection<UsageSpecificationItem> Specifications { get; } = new();
        public List<string> UsageTypes { get; } = new() { "Fluid", "Other Activity" };
        public List<string> ActivityOptions { get; } = new() 
        { 
            "Drilling", 
            "Completion/Filtration", 
            "Solids Control/Waste Management", 
            "Prod Fluids: Cementing", 
            "Lost/Damage", 
            "Other" 
        };
        public ObservableCollection<string> Fluidptions { get; } = new();

        public ICommand AddEntryCommand { get; }
        public ICommand RemoveEntryCommand { get; }

        public UsageSpecificationViewModel(InventoryService service, UsageBalanceItem balanceItem)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _balanceItem = balanceItem ?? throw new ArgumentNullException(nameof(balanceItem));

            AddEntryCommand = new RelayCommand(_ => AddEntry());
            RemoveEntryCommand = new RelayCommand(param => RemoveEntry(param as UsageSpecificationItem));

            LoadFluids();
            LoadSpecifications();
        }

        private void LoadFluids()
        {
            Fluidptions.Clear();
            try
            {
                var dataFile = Path.Combine(AppContext.BaseDirectory, "Data", "wholefluids.json");
                if (File.Exists(dataFile))
                {
                    var json = File.ReadAllText(dataFile);
                    var fluids = JsonSerializer.Deserialize<WholeFluidItem[]>(json);
                    if (fluids != null)
                    {
                        var names = fluids.Select(f => f.ProductName).Distinct().ToList();
                        foreach (var name in names) Fluidptions.Add(name);
                    }
                }
            }
            catch { /* Ignore errors loading fluids */ }

            if (Fluidptions.Count == 0)
            {
                Fluidptions.Add("Primary Fluid Set");
            }
        }

        private void LoadSpecifications()
        {
            Specifications.Clear();
            var movements = _service.GetMovements();
            var reportDate = DateTime.Today;

            var relevant = movements.Where(m => 
                string.Equals(m.ProductCode, _balanceItem.ProductCode, StringComparison.OrdinalIgnoreCase) &&
                m.Type == TicketType.Consumed &&
                m.Date.Date == reportDate).ToList();

            foreach (var m in relevant)
            {
                Specifications.Add(new UsageSpecificationItem(m));
            }
        }

        private void AddEntry()
        {
            var newMovement = new InventoryMovement
            {
                ProductCode = _balanceItem.ProductCode,
                ProductName = _balanceItem.ProductName,
                Type = TicketType.Consumed,
                Date = DateTime.Now,
                Quantity = 0,
                OriginOrUse = Fluidptions.FirstOrDefault() ?? "",
                UnitPrice = _balanceItem.UnitCost,
                IsAddedToFluid = true
            };
            Specifications.Add(new UsageSpecificationItem(newMovement));
        }

        private void RemoveEntry(UsageSpecificationItem? item)
        {
            if (item != null)
            {
                Specifications.Remove(item);
            }
        }

        public void Save()
        {
            var totalToUse = Specifications.Sum(s => Math.Max(0, s.Movement.Quantity));
            var available = _balanceItem.InitialQuantity + _balanceItem.ReceivedQuantity - _balanceItem.ReturnQuantity;
            if (totalToUse > available + 0.0001)
            {
                throw new InvalidOperationException(
                    $"Cannot use {totalToUse:N2} {_balanceItem.Unit}. Available is {available:N2} {_balanceItem.Unit}.");
            }

            var existingMovements = _service.GetMovements();
            var reportDate = DateTime.Today;
            
            var toDelete = existingMovements.Where(m => 
                string.Equals(m.ProductCode, _balanceItem.ProductCode, StringComparison.OrdinalIgnoreCase) &&
                m.Type == TicketType.Consumed &&
                m.Date.Date == reportDate).ToList();

            foreach (var m in toDelete)
            {
                _service.DeleteMovementById(m.MovementId);
            }

            if (Specifications.Count > 0)
            {
                var ticket = new Ticket
                {
                    TicketId = Guid.NewGuid().ToString(),
                    Date = DateTime.Now,
                    Type = TicketType.Consumed,
                    Status = TicketStatus.Posted,
                    Lines = Specifications.Select(s => new TicketLine
                    {
                        ProductCode = s.Movement.ProductCode,
                        ProductName = s.Movement.ProductName,
                        Quantity = s.Movement.Quantity,
                        Context = s.Movement.OriginOrUse,
                        IsAddedToFluid = s.Movement.IsAddedToFluid
                    }).ToList()
                };
                _service.CreateTicketConsumed(ticket);
            }

            _balanceItem.TotalUsedQuantity = Specifications.Sum(s => s.Movement.Quantity);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

