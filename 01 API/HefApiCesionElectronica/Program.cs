using HefApiCesionElectronica.Neg;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

////
//// Agregar el servicio de cesion de documentos de hefesto
builder.Services.AddTransient<IHefCeder, HefCeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();

app.Run();
