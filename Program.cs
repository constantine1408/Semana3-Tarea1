using ClientesWeb.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

var app = builder.Build();

// Cualquier error no controlado se responde como JSON (ProblemDetails) sin exponer detalles internos.
app.UseExceptionHandler();

// Sirve el frontend (carpeta wwwroot): index.html es la página de inicio.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
