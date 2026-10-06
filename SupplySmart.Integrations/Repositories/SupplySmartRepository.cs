using SupplySmart.Integrations.DbContexts;

namespace SupplySmart.Integrations.Repositories;

public class SupplySmartRepository<T>(SupplySmartDbContext context) : BaseRepository<T, SupplySmartDbContext>(context), IBaseRepository<T> where T : class;
