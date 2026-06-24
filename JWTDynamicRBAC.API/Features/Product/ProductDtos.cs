namespace JWTDynamicRBAC.API.Features.Product
{
    

    // UI သို့ Data သယ်ဆောင်ပေးမည့် DTO
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }
}