using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Metadata.Conventions;
using WorkManager.Configuration;
using WorkManager.Configuration.Database.External;
using WorkManager.Configuration.Interfaces;
using WorkManager.Database.Mongo;
using WorkManager.Models.S3;
using WorkManager.Models.Tree;

namespace WorkManager.Database.EntityFramework;

/// <summary>
///     Service builder for the MongoDocumentContext
/// </summary>
[ServiceConfiguration(ServiceName = "document_context", ServiceType = "mongo")]
public class MongoDocumentContextServiceBuilder : IExternalServiceBuilder
{
    public void ConfigureServices(IServiceCollection serviceCollection, IConfigurationSection configuration)
    {
        ServiceProvider sp = serviceCollection.BuildServiceProvider();
        IMongoClient mongoClient = sp.GetService<IMongoClient>();
        IOptions<MongoDocumentContextOptions> options = sp.GetService<IOptions<MongoDocumentContextOptions>>();
        
        serviceCollection.AddMongoDB<MongoDocumentContext>(mongoClient, options.Value.Database);
        serviceCollection.AddSingleton(typeof(DocumentContext<TreeDocument>), typeof(MongoDocumentContext));
        serviceCollection.AddSingleton(typeof(MongoDocumentContext));
    }
}

/// <summary>
///     MongoDocumentContext which inherits from the DocumentContext<TreeDocument>
/// </summary>
public class MongoDocumentContext : DocumentContext<TreeDocument>
{
    private readonly ILogger<MongoDocumentStore> _logger;
    private readonly string _collectionName;
    public MongoDocumentContext(DbContextOptions<MongoDocumentContext> options, IOptions<MongoDocumentContextOptions> mongoDocumentStoreOptions,
        ILogger<MongoDocumentStore> logger) : base(options)
    {
        _logger = logger;
        _collectionName = mongoDocumentStoreOptions.Value.Collection;
    }

    /// <summary>
    /// Defines the model for Mongo, sets up the mapping for DocumentId to _id and parsing from string to object id
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TreeDocument>(entity =>
        {
            entity.ToCollection(_collectionName);  // Maps the collection name to this entity
            entity.Property(t => t.DocumentId)
                .HasElementName("_id")
                .HasConversion(v => ObjectId.Parse(v), v => v.ToString());
        });
        
        base.OnModelCreating(modelBuilder);
    }
}