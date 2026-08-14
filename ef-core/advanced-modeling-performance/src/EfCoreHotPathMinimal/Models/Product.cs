namespace EfCoreHotPathMinimal.Models;

public sealed class Product
{
    public int Id
    {
        get;
        set;
    }

    public string Sku
    {
        get;
        set;
    } = "";

    public string Name
    {
        get;
        set;
    } = "";

    public int PriceCents
    {
        get;
        set;
    }

    public int StockQuantity
    {
        get;
        set;
    }

    public bool IsDeleted
    {
        get;
        set;
    }
}