using Microsoft.Extensions.DependencyInjection;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Application.Ports.Outgoing;
using ReservasSaaS.Application.UseCases;
using ReservasSaaS.Application.UseCases.Auth;

namespace ReservasSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 1. Diagnostics & Health
        services.AddScoped<IGetSystemHealthUseCase, GetSystemHealthUseCase>();

        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();


        // 2. Identity & Multi-tenancy Inbound Ports
        services.AddScoped<IGetUserProfileUseCase, UseCases.Identity.GetUserProfileUseCase>();
        services.AddScoped<IGetUserCompaniesUseCase, UseCases.Identity.GetUserCompaniesUseCase>();
        services.AddScoped<IAuthorizeCompanyAccessUseCase, UseCases.Identity.AuthorizeCompanyAccessUseCase>();
        services.AddScoped<ICreateCompanyUseCase, UseCases.Identity.CreateCompanyUseCase>();
        services.AddScoped<IGetCompanyMembersUseCase, UseCases.Identity.GetCompanyMembersUseCase>();

        // 3. Services, Professionals & Centralized Availability Engine
        services.AddScoped<IGetAvailableSlotsUseCase, UseCases.Agenda.GetAvailableSlotsUseCase>();
        services.AddScoped<IManageServicesUseCase, UseCases.Agenda.ManageServicesUseCase>();
        services.AddScoped<IManageProfessionalsUseCase, UseCases.Agenda.ManageProfessionalsUseCase>();
        services.AddScoped<IManageBookingSettingsUseCase, UseCases.Agenda.ManageBookingSettingsUseCase>();

        // 4. Bookings & Reservation Lifecycle
        services.AddScoped<ICreateBookingUseCase, UseCases.Bookings.CreateBookingUseCase>();
        services.AddScoped<IGetBookingUseCase, UseCases.Bookings.GetBookingUseCase>();
        services.AddScoped<ISearchBookingsUseCase, UseCases.Bookings.SearchBookingsUseCase>();
        services.AddScoped<IRescheduleBookingUseCase, UseCases.Bookings.RescheduleBookingUseCase>();
        services.AddScoped<IAssignProfessionalUseCase, UseCases.Bookings.AssignProfessionalUseCase>();
        services.AddScoped<IConfirmBookingUseCase, UseCases.Bookings.ConfirmBookingUseCase>();
        services.AddScoped<ICancelBookingUseCase, UseCases.Bookings.CancelBookingUseCase>();
        services.AddScoped<IFinalizeBookingUseCase, UseCases.Bookings.FinalizeBookingUseCase>();
        services.AddScoped<IExpireStaleBookingsUseCase, UseCases.Bookings.ExpireStaleBookingsUseCase>();
        services.AddScoped<IUpdateBookingPaymentUseCase, UseCases.Bookings.UpdateBookingPaymentUseCase>();

        return services;
    }
}
