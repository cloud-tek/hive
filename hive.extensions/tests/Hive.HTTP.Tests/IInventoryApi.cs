using Refit;

namespace Hive.HTTP.Tests;

public interface IInventoryApi
{
  [Get("/inventory")]
  Task<string[]> GetInventory();
}