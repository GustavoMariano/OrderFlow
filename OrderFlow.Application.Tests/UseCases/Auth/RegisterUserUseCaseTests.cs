using Bogus;
using FluentAssertions;
using Moq;
using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Application.Abstractions.Security;
using OrderFlow.Application.DTOs.Auth;
using OrderFlow.Application.UseCases.Auth;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Tests.UseCases.Auth;

public sealed class RegisterUserUseCaseTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private readonly Faker _faker = new();

    private RegisterRequest ValidRequest()
        => new(
            Email: _faker.Internet.Email().ToLowerInvariant(),
            Password: "StrongPass123!"
        );

    [Fact]
    public async Task Should_register_user_successfully()
    {
        var request = ValidRequest();

        _users
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasher
            .Setup(x => x.Hash(request.Password))
            .Returns("hashed-password");

        User? capturedUser = null;

        _users
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .Returns(Task.CompletedTask);

        var useCase = new RegisterUserUseCase(
            _users.Object,
            _passwordHasher.Object,
            _uow.Object);

        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        _users.Verify(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()), Times.Once);
        _passwordHasher.Verify(x => x.Hash(request.Password), Times.Once);
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be(request.Email);
        capturedUser.Id.Should().Be(result.Value);
    }

    [Fact]
    public async Task Should_fail_when_email_is_already_in_use()
    {
        var request = ValidRequest();

        _users
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(request.Email, "existing-hash"));

        var useCase = new RegisterUserUseCase(
            _users.Object,
            _passwordHasher.Object,
            _uow.Object);

        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _users.Verify(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()), Times.Once);
        _passwordHasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_fail_when_email_is_empty_or_whitespace()
    {
        var request = new RegisterRequest(
            Email: "   ",
            Password: "StrongPass123!"
        );

        var useCase = new RegisterUserUseCase(
            _users.Object,
            _passwordHasher.Object,
            _uow.Object);

        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _users.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_fail_when_password_is_too_short()
    {
        var request = new RegisterRequest(
            Email: _faker.Internet.Email().ToLowerInvariant(),
            Password: "1234567"
        );

        var useCase = new RegisterUserUseCase(
            _users.Object,
            _passwordHasher.Object,
            _uow.Object);

        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _users.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
