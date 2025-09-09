using Cuidemoslos.BLL.Interfaces;
using Cuidemoslos.Services.Email;
using Cuidemoslos.DAL.Repositories;
using Cuidemoslos.DAL.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.Services.DependencyInjection;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCuidemoslosServices(this IServiceCollection services)
    {
        // Infra / DAL
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IMoodEntryRepository, MoodEntryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // BLL (handlers  por convención más adelante)

        // Email / Notificaciones
        //services.AddSingleton<IEmailSender, MailKitEmailSender>();//
        services.AddSingleton<IEmailSender, FileEmailSender>();
        return services;
    }
}
