using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservasSaaS.Application.DTOs.Identity;
using ReservasSaaS.Application.Security;
using ReservasSaaS.Application.UseCases.Identity;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;
using Xunit;

namespace ReservasSaaS.UnitTests.UseCases;

public class CreateCompanyUseCaseTests
{
    private readonly Mock<ICurrentUserContext> _currentUserMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ICompanyRepository> _companyRepoMock;
    private readonly Mock<ICompanyMembershipRepository> _membershipRepoMock;
    private readonly CreateCompanyUseCase _sut;

    public CreateCompanyUseCaseTests()
    {
        _currentUserMock = new Mock<ICurrentUserContext>();
        _userRepoMock = new Mock<IUserRepository>();
        _companyRepoMock = new Mock<ICompanyRepository>();
        _membershipRepoMock = new Mock<ICompanyMembershipRepository>();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.FirebaseUid).Returns("fb-owner-1");
        _currentUserMock.Setup(c => c.Email).Returns("owner@center.com");

        var user = new User(Guid.NewGuid(), "fb-owner-1", "owner@center.com", "Owner User");
        _userRepoMock.Setup(r => r.GetByFirebaseUidAsync("fb-owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _sut = new CreateCompanyUseCase(
            _currentUserMock.Object,
            _userRepoMock.Object,
            _companyRepoMock.Object,
            _membershipRepoMock.Object,
            NullLogger<CreateCompanyUseCase>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesCompanyAndAssignsCreatorAsOwner()
    {
        // Arrange
        var command = new CreateCompanyCommand("CrossFit Arena", "crossfit-arena", "TAX-9001", "America/Bogota", "COP");

        _companyRepoMock.Setup(r => r.GetBySlugAsync("crossfit-arena", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        // Act
        var result = await _sut.ExecuteAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("CrossFit Arena");
        result.Slug.Should().Be("crossfit-arena");
        result.Status.Should().Be(CompanyStatus.Active);

        // Verify company added
        _companyRepoMock.Verify(r => r.AddAsync(It.Is<Company>(c => c.Name == "CrossFit Arena"), It.IsAny<CancellationToken>()), Times.Once);

        // Verify creator assigned as Owner membership
        _membershipRepoMock.Verify(r => r.AddAsync(
            It.Is<CompanyMembership>(m => m.Role == CompanyRole.Owner && m.Status == MembershipStatus.Active),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
