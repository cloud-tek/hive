using Refit;

namespace Hive.HTTP.Tests;

public interface IProductApi
{
  [Get("/products")]
  Task<string[]> GetProducts();
}