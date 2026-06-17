using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCore.Interfaces;

internal interface IEFCoreEntityConfiguration
{
    void Configure(ModelBuilder modelBuilder);
}
