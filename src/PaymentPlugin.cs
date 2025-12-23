// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;

namespace Demo.Workflows.Declarative.ServicesPayment;

internal sealed class PaymentPlugin
{
    private readonly Dictionary<string, List<string>> _customerFavorites = new()
    {
        ["cust-1"] = ["SVC001", "SVC002", "SVC004"],
        ["cust-2"] = ["SVC003", "SVC005"],
        ["cust-3"] = ["SVC001", "SVC003", "SVC004", "SVC005"],
    };

    private readonly Dictionary<string, ServiceInfo> _services = new()
    {
        ["SVC001"] = new ServiceInfo { Id = "SVC001", Name = "Luz del Sur", Category = "Electricidad" },
        ["SVC002"] = new ServiceInfo { Id = "SVC002", Name = "Sedapal", Category = "Agua" },
        ["SVC003"] = new ServiceInfo { Id = "SVC003", Name = "Claro Móvil", Category = "Telefonía" },
        ["SVC004"] = new ServiceInfo { Id = "SVC004", Name = "Netflix", Category = "Streaming" },
        ["SVC005"] = new ServiceInfo { Id = "SVC005", Name = "Movistar Hogar", Category = "Internet" },
    };

    private readonly Dictionary<string, AccountInfo> _accounts = new()
    {
        ["acct-123"] = new AccountInfo { Id = "acct-123", Balance = 1000.50m, Currency = "S/." },
        ["acct-124"] = new AccountInfo { Id = "acct-124", Balance = 250.00m, Currency = "S/." },
    };

    private readonly Dictionary<string, BillInfo> _latestBills = new()
    {
        ["cust-1:SVC001"] = new BillInfo { BillId = "BILL-001", CustomerId = "cust-1", ServiceId = "SVC001", ServiceName = "Luz del Sur", Amount = 85.50m, Currency = "S/.", DueDate = DateTime.Now.AddDays(15), Period = "Noviembre 2025", Status = "Pendiente" },
        ["cust-1:SVC002"] = new BillInfo { BillId = "BILL-002", CustomerId = "cust-1", ServiceId = "SVC002", ServiceName = "Sedapal", Amount = 42.30m, Currency = "S/.", DueDate = DateTime.Now.AddDays(10), Period = "Noviembre 2025", Status = "Pendiente" },
        ["cust-1:SVC004"] = new BillInfo { BillId = "BILL-003", CustomerId = "cust-1", ServiceId = "SVC004", ServiceName = "Netflix", Amount = 44.90m, Currency = "S/.", DueDate = DateTime.Now.AddDays(5), Period = "Diciembre 2025", Status = "Pendiente" },
        ["cust-2:SVC003"] = new BillInfo { BillId = "BILL-004", CustomerId = "cust-2", ServiceId = "SVC003", ServiceName = "Claro Móvil", Amount = 59.90m, Currency = "S/.", DueDate = DateTime.Now.AddDays(8), Period = "Diciembre 2025", Status = "Pendiente" },
        ["cust-2:SVC005"] = new BillInfo { BillId = "BILL-005", CustomerId = "cust-2", ServiceId = "SVC005", ServiceName = "Movistar Hogar", Amount = 120.00m, Currency = "S/.", DueDate = DateTime.Now.AddDays(20), Period = "Diciembre 2025", Status = "Pendiente" },
        ["cust-3:SVC001"] = new BillInfo { BillId = "BILL-006", CustomerId = "cust-3", ServiceId = "SVC001", ServiceName = "Luz del Sur", Amount = 150.75m, Currency = "S/.", DueDate = DateTime.Now.AddDays(-2), Period = "Noviembre 2025", Status = "Vencido" },
        ["cust-3:SVC003"] = new BillInfo { BillId = "BILL-007", CustomerId = "cust-3", ServiceId = "SVC003", ServiceName = "Claro Móvil", Amount = 39.90m, Currency = "S/.", DueDate = DateTime.Now.AddDays(12), Period = "Diciembre 2025", Status = "Pendiente" },
    };

    private readonly Dictionary<string, ReceiptInfo> _receipts = [];

    [Description("List favorite services for a customer.")]
    public List<ServiceInfo> ListFavoriteServices(string customerId)
    {
        Trace(nameof(ListFavoriteServices));
        
        if (_customerFavorites.TryGetValue(customerId, out var favoriteIds))
        {
            return favoriteIds
                .Where(id => _services.ContainsKey(id))
                .Select(id => _services[id])
                .ToList();
        }
        
        // Return empty list if customer not found
        return [];
    }

    [Description("Get the balance of an account.")]
    public BalanceInfo GetBalance(string accountId)
    {
        Trace(nameof(GetBalance));
        
        if (_accounts.TryGetValue(accountId, out var account))
        {
            return new BalanceInfo 
            { 
                Balance = account.Balance, 
                Currency = account.Currency,
                ErrorMessage = string.Empty
            };
        }
        
        return new BalanceInfo 
        { 
            Balance = 0, 
            Currency = "S/.",
            ErrorMessage = $"Account {accountId} not found."
        };
    }

    [Description("Execute a payment for a service.")]
    public PaymentResult PayService(string accountId, string serviceId, decimal amount)
    {
        Trace(nameof(PayService));

        if (!_accounts.TryGetValue(accountId, out var account))
        {
            return new PaymentResult
            {
                ReceiptId = string.Empty,
                ReceiptDetails = string.Empty,
                ErrorMessage = $"Account {accountId} not found."
            };
        }

        if (account.Balance < amount)
        {
            return new PaymentResult
            {
                ReceiptId = string.Empty,
                ReceiptDetails = string.Empty,
                ErrorMessage = "Insufficient funds."
            };
        }

        if (!_services.TryGetValue(serviceId, out var service))
        {
            return new PaymentResult
            {
                ReceiptId = string.Empty,
                ReceiptDetails = string.Empty,
                ErrorMessage = $"Service {serviceId} not found."
            };
        }

        // Process payment
        account.Balance -= amount;
        var receiptId = $"RCP-{Guid.NewGuid():N}"[..12].ToUpper();
        
        var receipt = new ReceiptInfo
        {
            ReceiptId = receiptId,
            AccountId = accountId,
            ServiceId = serviceId,
            ServiceName = service.Name,
            Amount = amount,
            Currency = account.Currency,
            Timestamp = DateTime.UtcNow
        };
        
        _receipts[receiptId] = receipt;

        return new PaymentResult
        {
            ReceiptId = receiptId,
            ReceiptDetails = $"Pago de {amount:F2} {account.Currency} a {service.Name} realizado exitosamente. Fecha: {receipt.Timestamp:g}",
            ErrorMessage = string.Empty
        };
    }

    [Description("Get the latest bill for a customer and service.")]
    public BillInfo GetLatestBill(string customerId, string serviceId)
    {
        Trace(nameof(GetLatestBill));
        
        var key = $"{customerId}:{serviceId}";
        
        if (_latestBills.TryGetValue(key, out var bill))
        {
            return bill;
        }
        
        // Check if service exists
        if (!_services.TryGetValue(serviceId, out var service))
        {
            return new BillInfo
            {
                ErrorMessage = $"Servicio {serviceId} no encontrado."
            };
        }
        
        return new BillInfo
        {
            ServiceId = serviceId,
            ServiceName = service.Name,
            ErrorMessage = $"No se encontró factura pendiente para {service.Name}."
        };
    }

    private static void Trace(string functionName)
    {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        try
        {
            Console.WriteLine($"\nFUNCTION: {functionName}");
        }
        finally
        {
            Console.ResetColor();
        }
    }

    // DTOs
    public sealed class ServiceInfo
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
    }

    public sealed class AccountInfo
    {
        public string Id { get; init; } = string.Empty;
        public decimal Balance { get; set; }
        public string Currency { get; init; } = "S/.";
    }

    public sealed class BalanceInfo
    {
        public decimal Balance { get; init; }
        public string Currency { get; init; } = "S/.";
        public string ErrorMessage { get; init; } = string.Empty;
    }

    public sealed class PaymentResult
    {
        public string ReceiptId { get; init; } = string.Empty;
        public string ReceiptDetails { get; init; } = string.Empty;
        public string ErrorMessage { get; init; } = string.Empty;
    }

    public sealed class ReceiptInfo
    {
        public string ReceiptId { get; init; } = string.Empty;
        public string AccountId { get; init; } = string.Empty;
        public string ServiceId { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "S/.";
        public DateTime Timestamp { get; init; }
    }

    public sealed class BillInfo
    {
        public string BillId { get; init; } = string.Empty;
        public string CustomerId { get; init; } = string.Empty;
        public string ServiceId { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "S/.";
        public DateTime DueDate { get; init; }
        public string Period { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;  // "Pendiente", "Vencido", "Pagado"
        public string ErrorMessage { get; init; } = string.Empty;
    }
}