using FaceApi.Services;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

// Habilita controladores
builder.Services.AddControllers();
builder.Services.AddSingleton<CameraService>();
builder.Services.AddSingleton<IStorageService, S3StorageService>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();


var app = builder.Build();

//Habilita Swagger solo en desarrollo 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Uso de controladores
app.UseAuthorization();

// Registrar los controladores
app.MapControllers();

app.Run();

