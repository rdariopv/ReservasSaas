using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservasSaaS.Application.Security;
using ReservasSaaS.Application.UseCases.Identity;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.Security;
using Xunit;

namespace ReservasSaaS.UnitTests.Security;

public class TenantIsolationTests
{
    private readonly Mock<ICurrentUserContext> _currentUserMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ICompanyRepository> _companyRepoMock;
    private readonly Mock<ICompanyMembershipRepository> _membershipRepoMock;
    private readonly AuthorizeCompanyAccessUseCase _sut;

    private readonly Guid _userAId = Guid.NewGuid();
    private readonly string _firebaseUidA = "firebase-user-alpha";
    private readonly User _userA;

    private readonly Guid _companyAId = Guid.NewGuid();
    private readonly Company _companyA;

    private readonly Guid _companyBId = Guid.NewGuid();
    private readonly Company _companyB;

    public TenantIsolationTests()
    {
        _currentUserMock = new Mock<ICurrentUserContext>();
        _userRepoMock = new Mock<IUserRepository>();
        _companyRepoMock = new Mock<ICompanyRepository>();
        _membershipRepoMock = new Mock<ICompanyMembershipRepository>();

        _userA = new User(_userAId, _firebaseUidA, "alpha@company-a.com", "User Alpha");
        _companyA = new Company(_companyAId, "Padel Club Alpha", "padel-alpha");
        _companyB = new Company(_companyBId, "Tennis Club Beta", "tennis-beta");

        // Set current user context to User A
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.FirebaseUid).Returns(_firebaseUidA);

        _userRepoMock.Setup(r => r.GetByFirebaseUidAsync(_firebaseUidA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_userA);

        _companyRepoMock.Setup(r => r.GetByIdAsync(_companyAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_companyA);
        _companyRepoMock.Setup(r => r.GetByIdAsync(_companyBId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_companyB);

        _sut = new AuthorizeCompanyAccessUseCase(
            _currentUserMock.Object,
            _userRepoMock.Object,
            _companyRepoMock.Object,
            _membershipRepoMock.Object,
            NullLogger<AuthorizeCompanyAccessUseCase>.Instance);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserHasActiveMembershipInCompanyA_GrantsAccess()
    {
        // Arrange: User A is Owner of Company A
        var membershipA = new CompanyMembership(Guid.NewGuid(), _companyAId, _userAId, CompanyRole.Owner, MembershipStatus.Active);
        _membershipRepoMock.Setup(r => r.GetMembershipAsync(_companyAId, _userAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membershipA);

        // Act
        var result = await _sut.AuthorizeAsync(_companyAId, Permissions.CompanyRead, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.Company.Id.Should().Be(_companyAId);
        result.Role.Should().Be(CompanyRole.Owner);
        result.EffectivePermissions.Should().Contain(Permissions.CompanyRead);
        result.EffectivePermissions.Should().Contain(Permissions.MembersRead);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserAttemptsAccessToCompanyBWithoutMembership_ThrowsForbiddenException()
    {
        // Arrange: User A belongs to Company A, but has NO membership in Company B
        _membershipRepoMock.Setup(r => r.GetMembershipAsync(_companyBId, _userAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyMembership?)null);

        // Act & Assert
        // Client sending CompanyB ID is rejected on the server regardless of client claims
        var act = async () => await _sut.AuthorizeAsync(_companyBId, null, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ForbiddenException>();
        ex.Which.Code.Should().Be("FORBIDDEN");
        ex.Which.CompanyId.Should().Be(_companyBId.ToString());
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserMembershipIsSuspended_ThrowsForbiddenException()
    {
        // Arrange: Membership exists but is Suspended
        var suspendedMembership = new CompanyMembership(
            Guid.NewGuid(), _companyAId, _userAId, CompanyRole.Reception, MembershipStatus.Suspended);

        _membershipRepoMock.Setup(r => r.GetMembershipAsync(_companyAId, _userAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedMembership);

        // Act & Assert
        var act = async () => await _sut.AuthorizeAsync(_companyAId, null, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ForbiddenException>();
        ex.Which.Message.Should().Contain("Suspended");
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserLacksRequiredPermission_ThrowsForbiddenException()
    {
        // Arrange: User A is Professional in Company A, but endpoint requires BillingManage (Owner only)
        var professionalMembership = new CompanyMembership(
            Guid.NewGuid(), _companyAId, _userAId, CompanyRole.Professional, MembershipStatus.Active);

        _membershipRepoMock.Setup(r => r.GetMembershipAsync(_companyAId, _userAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(professionalMembership);

        // Act & Assert
        var act = async () => await _sut.AuthorizeAsync(_companyAId, Permissions.BillingManage, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ForbiddenException>();
        ex.Which.RequiredPermission.Should().Be(Permissions.BillingManage);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserIsSaaSAdministrator_GrantsCrossTenantAccessWithAllPermissions()
    {
        // Arrange: User is SaaS Administrator
        var adminUser = new User(Guid.NewGuid(), "saas-admin-uid", "admin@platform.saas", "Global Admin", isSaaSAdministrator: true);

        _currentUserMock.Setup(c => c.FirebaseUid).Returns("saas-admin-uid");
        _userRepoMock.Setup(r => r.GetByFirebaseUidAsync("saas-admin-uid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        // Act: Accessing Company B without a direct tenant membership record
        var result = await _sut.AuthorizeAsync(_companyBId, Permissions.CompanyDelete, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeTrue();
        result.Role.Should().Be(CompanyRole.SaaSAdministrator);
        result.EffectivePermissions.Should().Contain(Permissions.GlobalTenantManage);
        result.EffectivePermissions.Should().Contain(Permissions.CompanyDelete);
    }
}
