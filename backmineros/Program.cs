using BackMineros.Data;
using Microsoft.EntityFrameworkCore;


// Esta bandera sirve para elegir la base de datos adecuada.
// cpu: Jairo, Local-intecol.
// si se requiere una diferente contactar con jcortes@intecol.com.co o revisar la documentacion.
string cpu = "Local-intecol";

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5000");
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});


if (cpu == "Jairo")
{
    builder.Services.AddDbContext<ApplicationDBContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerJairo")));
}
else if(cpu == "Local-intecol")
{
    builder.Services.AddDbContext<ApplicationDBContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerLocal")));
}
else if (cpu == "Jhonnatan")
{
    builder.Services.AddDbContext<ApplicationDBContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerJhonnatan")));
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDBContext>();
        var entidad = dbContext.ControlCamara.FirstOrDefault();
        if (entidad != null)
        {
            // Cambiar el valor de la variable
            entidad.estado = false;

            // Guardar los cambios en la base de datos
            dbContext.SaveChanges();

            Console.WriteLine("�Se han actualizado los parametros!");
        }
    }
    catch (Exception ex)
    {
        // Manejar errores
        Console.WriteLine("Error al configurar la base de datos o al iniciar la aplicaci�n: " + ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
