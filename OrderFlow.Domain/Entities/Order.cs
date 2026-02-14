using OrderFlow.Domain.Common;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public sealed class Order : AuditableEntity
{
    private readonly List<OrderItem> _items = new();

    private Order() { }

    public Order(Guid userId, string currency = "USD")
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        UserId = userId;
        Currency = NormalizeCurrency(currency);
        Status = OrderStatus.Pending;
    }

    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; } = "USD";

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Sum(i => i.LineTotal);

    public void AddItem(string sku, string name, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("You can only add items when the order is Pending.");

        _items.Add(new OrderItem(sku, name, quantity, unitPrice));
        Touch();
    }

    public void MarkProcessing()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Only Pending orders can be marked as Processing.");

        Status = OrderStatus.Processing;
        Touch();
    }

    public void MarkCompleted()
    {
        if (Status != OrderStatus.Processing)
            throw new DomainException("Only Processing orders can be marked as Completed.");

        if (_items.Count == 0)
            throw new DomainException("Cannot complete an order with zero items.");

        Status = OrderStatus.Completed;
        Touch();
    }

    public void MarkFailed()
    {
        if (Status == OrderStatus.Completed)
            throw new DomainException("Completed orders cannot be marked as Failed.");

        Status = OrderStatus.Failed;
        Touch();
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return "USD";

        currency = currency.Trim().ToUpperInvariant();

        if (currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter code (e.g., USD).");

        return currency;
    }
}
