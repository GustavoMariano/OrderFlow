using Bogus;
using FluentAssertions;
using Moq;
using OrderFlow.Application.Abstractions.Messaging;
using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Application.DTOs.Orders;
using OrderFlow.Application.UseCases.Orders;
using OrderFlow.Contracts.Abstractions;
using OrderFlow.Contracts.Orders.V1;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Tests.UseCases.Orders;

public sealed class CreateOrderUseCaseTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEventPublisher> _publisher = new();

    private readonly Faker _faker = new();

    private CreateOrderRequest ValidRequest()
        => new(
            Currency: "USD",
            Items: new List<CreateOrderItemRequest>
            {
                new(
                    Sku: _faker.Commerce.Ean13(),
                    Name: _faker.Commerce.ProductName(),
                    Quantity: 2,
                    UnitPrice: 50m
                ),
                new(
                    Sku: _faker.Commerce.Ean13(),
                    Name: _faker.Commerce.ProductName(),
                    Quantity: 1,
                    UnitPrice: 100m
                )
            });

    [Fact]
    public async Task Should_create_order_and_publish_event()
    {
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var request = ValidRequest();

        EventEnvelope<OrderCreatedV1>? capturedEnvelope = null;

        _publisher
            .Setup(x => x.PublishAsync(
                It.IsAny<EventEnvelope<OrderCreatedV1>>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventEnvelope<OrderCreatedV1>, CancellationToken>((env, _) => capturedEnvelope = env)
            .Returns(Task.CompletedTask);

        var useCase = new CreateOrderUseCase(
            _orders.Object,
            _uow.Object,
            _publisher.Object);

        var result = await useCase.ExecuteAsync(userId, request, correlationId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        _orders.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _publisher.Verify(x => x.PublishAsync(
            It.IsAny<EventEnvelope<OrderCreatedV1>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.EventType.Should().Be("OrderCreated.v1");
        capturedEnvelope.CorrelationId.Should().Be(correlationId);
        capturedEnvelope.Data.UserId.Should().Be(userId);
        capturedEnvelope.Data.Currency.Should().Be("USD");
        capturedEnvelope.Data.TotalAmount.Should().Be(200m);
        capturedEnvelope.Data.OrderId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Should_fail_when_user_is_empty()
    {
        var correlationId = Guid.NewGuid();
        var request = ValidRequest();

        var useCase = new CreateOrderUseCase(
            _orders.Object,
            _uow.Object,
            _publisher.Object);

        var result = await useCase.ExecuteAsync(Guid.Empty, request, correlationId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _orders.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(x => x.PublishAsync(
            It.IsAny<EventEnvelope<OrderCreatedV1>>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_fail_when_items_are_empty()
    {
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var request = new CreateOrderRequest(
            Currency: "USD",
            Items: new List<CreateOrderItemRequest>());

        var useCase = new CreateOrderUseCase(
            _orders.Object,
            _uow.Object,
            _publisher.Object);

        var result = await useCase.ExecuteAsync(userId, request, correlationId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _orders.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(x => x.PublishAsync(
            It.IsAny<EventEnvelope<OrderCreatedV1>>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_calculate_total_amount_correctly()
    {
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var request = new CreateOrderRequest(
            Currency: "USD",
            Items: new List<CreateOrderItemRequest>
            {
                new("A", "Item A", 3, 10m),
                new("B", "Item B", 2, 5m)
            });

        EventEnvelope<OrderCreatedV1>? capturedEnvelope = null;

        _publisher
            .Setup(x => x.PublishAsync(
                It.IsAny<EventEnvelope<OrderCreatedV1>>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventEnvelope<OrderCreatedV1>, CancellationToken>((env, _) => capturedEnvelope = env)
            .Returns(Task.CompletedTask);

        var useCase = new CreateOrderUseCase(
            _orders.Object,
            _uow.Object,
            _publisher.Object);

        await useCase.ExecuteAsync(userId, request, correlationId, CancellationToken.None);

        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.Data.TotalAmount.Should().Be(40m);
    }

    [Fact]
    public async Task Should_use_same_correlation_id_in_event()
    {
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var request = ValidRequest();

        EventEnvelope<OrderCreatedV1>? capturedEnvelope = null;

        _publisher
            .Setup(x => x.PublishAsync(
                It.IsAny<EventEnvelope<OrderCreatedV1>>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventEnvelope<OrderCreatedV1>, CancellationToken>((env, _) => capturedEnvelope = env)
            .Returns(Task.CompletedTask);

        var useCase = new CreateOrderUseCase(
            _orders.Object,
            _uow.Object,
            _publisher.Object);

        await useCase.ExecuteAsync(userId, request, correlationId, CancellationToken.None);

        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.CorrelationId.Should().Be(correlationId);
    }
}
