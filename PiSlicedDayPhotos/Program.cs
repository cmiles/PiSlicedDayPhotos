using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PiSlicedDayPhotos;
using PiSlicedDayPhotos.Utility;

LogTools.StandardStaticLoggerForProgramDirectory("PiSlicedDayPhotos");

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSystemd();
builder.Services.AddHostedService<PhotoWorker>();

var host = builder.Build();
host.Run();