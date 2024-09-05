using Microsoft.EntityFrameworkCore;

namespace AzureFuncInK8s;

public class FuncDbContext : DbContext
{
  public FuncDbContext(DbContextOptions<FuncDbContext> options) : base(options)
  {
  }
  public DbSet<TESTDATA> TestData { get; set; }
}

[Keyless]
public class TESTDATA
{
    public string key { get; set; }
    public string value { get; set; }
}