
using JWTDynamicRBAC.Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;


namespace JWTDynamicRBAC.API.Features.Product
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllProductsAsync();
        Task<ProductDto> CreateProductAsync(ProductDto productDto);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<bool> UpdateProductAsync(int id, ProductDto productDto);
        Task<bool> DeleteProductAsync(int id);

    }

    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. CREATE (အသစ်ထည့်သွင်းခြင်း)
        // ==========================================
        public async Task<ProductDto> CreateProductAsync(ProductDto productDto)
        {
            // DTO မှ Entity သို့ ပြောင်းခြင်း
            var product = new Database.AppDbContextModels.Product
            {
                Name = productDto.Name,
                Price = productDto.Price,
                Description = productDto.Description
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync(); // Database ထဲသို့ Save လုပ်ခြင်း

            // Save ပြီးပါက ထွက်လာမည့် ID ကို DTO ထဲပြန်ထည့်ပေးခြင်း
            productDto.Id = product.Id;
            return productDto;
        }

        // ==========================================
        // 2. READ ALL (အားလုံးကို ဆွဲထုတ်ခြင်း)
        // ==========================================
        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            // Database ထဲမှ Data များကို Select သုံး၍ DTO သို့ ပြောင်းပြီး ဆွဲထုတ်ခြင်း
            return await _context.Products
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description=p.Description
                })
                .ToListAsync();
        }

        // ==========================================
        // 3. READ BY ID (တစ်ခုတည်းကိုသာ ရှာဖွေခြင်း)
        // ==========================================
        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null) return null;

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description=product.Description
            };
        }

        // ==========================================
        // 4. UPDATE (ပြင်ဆင်ခြင်း)
        // ==========================================
        public async Task<bool> UpdateProductAsync(int id, ProductDto productDto)
        {
            // ပထမဦးစွာ ပြင်ဆင်လိုသော Product ရှိ/မရှိ ရှာပါမည်
            var existingProduct = await _context.Products.FindAsync(id);

            if (existingProduct == null)
                return false; // မရှိပါက False ပြန်ပေးမည်

            // Data အသစ်များ အစားထိုးခြင်း
            existingProduct.Name = productDto.Name;
            existingProduct.Price = productDto.Price;
            existingProduct.Description=productDto.Description;

            _context.Products.Update(existingProduct);
            await _context.SaveChangesAsync();

            return true;
        }

        // ==========================================
        // 5. DELETE (ဖျက်ပစ်ခြင်း)
        // ==========================================
        public async Task<bool> DeleteProductAsync(int id)
        {
            // ဖျက်လိုသော Product ရှိ/မရှိ ရှာပါမည်
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}