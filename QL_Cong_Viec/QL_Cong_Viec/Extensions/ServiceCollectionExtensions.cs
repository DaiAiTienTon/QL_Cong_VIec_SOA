using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using QL_Cong_Viec.ESB.Implementation;
using QL_Cong_Viec.ESB.Interface;

using QL_Cong_Viec.ESB.Services;
using QL_Cong_Viec.Service;
using System.Collections.Concurrent;


namespace QL_Cong_Viec.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddESB(this IServiceCollection services)
        {
            // Register ESB core components
            services.AddSingleton<IServiceRegistry, ServiceRegistry>();
            services.AddSingleton<IServiceBus, ServiceBus>();

            // Register service adapters with configuration (Scoped!)
            services.AddScoped<FlightServiceAdapter>();
         
            services.AddScoped<HotelServiceAdapter>();
            services.AddScoped<WikiServiceAdapter>();
            services.AddSingleton<CountryServiceAdapter>();
            services.AddSingleton<TimeServiceAdapter>();
            services.AddSingleton<WeatherServiceAdapter>();
            services.AddSingleton<CurrencyServiceAdapter>();
            // Register additional ESB services
            services.AddScoped<HealthCheckService>();
            services.AddScoped<FlightAggregatorService>();
            services.AddScoped<ServiceMonitoringService>();
            services.AddScoped<ServiceCacheService>();

            return services;
        }

        public static void ConfigureESB(this IServiceProvider serviceProvider)
        {
            var serviceRegistry = serviceProvider.GetRequiredService<IServiceRegistry>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            using var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            var serviceSettings = configuration.GetSection("ServiceSettings");

            if (IsServiceEnabled(serviceSettings, "FlightService"))
                serviceRegistry.RegisterService("FlightService", scopedProvider.GetRequiredService<FlightServiceAdapter>());

            ;

            if (IsServiceEnabled(serviceSettings, "HotelService"))
                serviceRegistry.RegisterService("HotelService", scopedProvider.GetRequiredService<HotelServiceAdapter>());

            if (IsServiceEnabled(serviceSettings, "WikiService"))
                serviceRegistry.RegisterService("WikiService", scopedProvider.GetRequiredService<WikiServiceAdapter>());
            if (IsServiceEnabled(serviceSettings, "CountryService"))
                serviceRegistry.RegisterService("CountryService", serviceProvider.GetRequiredService<CountryServiceAdapter>());
            if (IsServiceEnabled(serviceSettings, "TimeService"))
                serviceRegistry.RegisterService("TimeService", serviceProvider.GetRequiredService<TimeServiceAdapter>());
            if (IsServiceEnabled(serviceSettings, "WeatherService"))
                serviceRegistry.RegisterService("WeatherService", serviceProvider.GetRequiredService<WeatherServiceAdapter>());
            if (IsServiceEnabled(serviceSettings, "CurrencyService"))
                serviceRegistry.RegisterService("CurrencyService", serviceProvider.GetRequiredService<CurrencyServiceAdapter>());
        }

        private static bool IsServiceEnabled(IConfigurationSection serviceSettings, string serviceName)
        {
            return serviceSettings.GetSection(serviceName).GetValue<bool>("Enabled", true);
        }
    }

}


