using CustomizableOrders;
using CustomizableOrders.Data;
using CustomizableOrders.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var host = new HostBuilder()
.ConfigureFunctionsWorkerDefaults()
.ConfigureServices(service =>
{
    service.AddHttpClient();
    service.AddApiTemplateIoHttpClient();
    service.AddGoogleOAuthHttpClient();
    service.AddGoogleApiHttpClient();
    service.AddScoped<ICustomOrderRepository>(p => new CustomOrderRepository(Config.ConnectionString));
    service.AddScoped<GoogleService>();
    service.AddScoped<ImageSheetService>();
    service.AddScoped<LabelService>();
    service.AddScoped<CustomOrderService>();
})
.Build();

host.Run();