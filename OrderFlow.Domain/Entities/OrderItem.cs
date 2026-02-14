using OrderFlow.Domain.Common;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    private OrderItem() { }

    public OrderItem(string sku, string name, int quantity, decimal unitPrice)
    {
        SetSku(sku);
        SetName(name);
        SetQuantity(quantity);
        SetUnitPrice(unitPrice);
    }

    public Guid OrderId { get; private set; }
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    public void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Item SKU is required.");

        Sku = sku.Trim();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Item name is required.");

        Name = name.Trim();
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Item quantity must be greater than zero.");

        Quantity = quantity;
    }

    public void SetUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0)
            throw new DomainException("Item unit price must be greater than zero.");

        UnitPrice = unitPrice;
    }
}
