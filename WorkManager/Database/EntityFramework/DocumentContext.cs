using Microsoft.EntityFrameworkCore;
using WorkManager.Models;
using WorkManager.Models.S3;
using WorkManager.Models.Tree;

namespace WorkManager.Database.EntityFramework;

/// <summary>
///     The base DocumentContext which just defines that there is a DbSet with TDocument, and that TDocument has to inherit from Document<StringTreeNode, Metadata>
///     Just kind of messing around with abstracting away some of the implementation details.
/// </summary>
/// <param name="options"></param>
/// <typeparam name="TDocument"></typeparam>
public class DocumentContext<TDocument>(DbContextOptions options)
    : DbContext(options) where TDocument : Document<StringTreeNode, Metadata>
{
    public DbSet<TDocument> Documents { get; set; }
}