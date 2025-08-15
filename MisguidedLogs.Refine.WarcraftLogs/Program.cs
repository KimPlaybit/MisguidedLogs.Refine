using BunnyCDN.Net.Storage;
using MisguidedLogs.Refine.WarcraftLogs;
using MisguidedLogs.Refine.WarcraftLogs.Bunnycdn;
using MisguidedLogs.Refine.WarcraftLogs.Mappers;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseConsoleLifetime();

builder.Services.Configure<ClientConfig>(builder.Configuration);
var config = builder.Configuration.GetSection("ClientEndpoint").Get<ClientConfig>();

ArgumentNullException.ThrowIfNull(config);

builder.Services.AddSingleton(config);


builder.Services.AddSingleton(new BunnyCDNStorage(config.BunnyCdnStorage, config.BunnyAccessKey, "se"));
builder.Services.AddTransient<BunnyCdnStorageUploader>();
builder.Services.AddTransient<BunnyCdnStorageLoader>();
builder.Services.AddTransient<Mapper>();

builder.Services.AddHostedService<Runner>();

//Get HostapplicationBuilder, needed to bypass generivWebHostService
var field = builder.GetType().GetField("_hostApplicationBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
ArgumentNullException.ThrowIfNull(field);
var hostApplicatiobuilder = (HostApplicationBuilder?)field.GetValue(builder);
ArgumentNullException.ThrowIfNull(hostApplicatiobuilder);
//Continue after here

var builtApplication = hostApplicatiobuilder.Build();
HostingAbstractionsHostExtensions.Run(builtApplication);
