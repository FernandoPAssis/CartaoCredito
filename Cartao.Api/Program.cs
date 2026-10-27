using Cartao.Api.Data;
using Cartao.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();




// banco SQLite
builder.Services.AddDbContext<CartaoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CartaoDb")));

// RabbitMQ: uma conexão para a aplicação inteira
ConnectionFactory fabrica = new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost"
};
IConnection conexaoRabbit = await fabrica.CreateConnectionAsync();

builder.Services.AddSingleton(conexaoRabbit);

// consumidor que roda em segundo plano escutando a fila
builder.Services.AddHostedService<PropostaGeradaConsumer>();





var app = builder.Build();

// cria o banco e a tabela na primeira execução
using (var scope = app.Services.CreateScope())
{
    CartaoDbContext db = scope.ServiceProvider.GetRequiredService<CartaoDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();