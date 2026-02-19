using Marten;
using Marten.Events.Projections;
using NForza.Wolverine.ValueTypes.Sample.WebApi.Customers;
using NForza.Wolverine.ValueTypes.Sample.WebApi.Orders;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWolverineHttp();

builder.Services.AddMarten(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Marten")
        ?? "Host=localhost;Port=5432;Database=wolverine_valuetypes_sample;Username=postgres;Password=postgres";

    var opts = new StoreOptions();
    opts.Connection(connectionString);
    opts.DatabaseSchemaName = "sample";

    opts.Projections.Snapshot<Customer>(SnapshotLifecycle.Inline);
    opts.Projections.Snapshot<Order>(SnapshotLifecycle.Inline);

    return opts;
})
.IntegrateWithWolverine();

builder.Host.UseWolverine(opts =>
{
    opts.Policies.AutoApplyTransactions();
    opts.Include<NForza.Wolverine.ValueTypes.WolverineValueTypeExtension>();
});

var app = builder.Build();

app.MapWolverineEndpoints();

await app.RunAsync();

public partial class Program { }
