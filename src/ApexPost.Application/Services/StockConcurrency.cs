namespace ApexPost.Application.Services;

public static class StockConcurrency
{
    public static SemaphoreSlim Gate { get; } = new(1, 1);
}
