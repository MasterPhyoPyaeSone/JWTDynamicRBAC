using Microsoft.AspNetCore.Mvc;
using JWTDynamicRBAC.API.Features.Product;
using JWTDynamicRBAC.API.Features.Filters;
using Microsoft.AspNetCore.Authorization; // 💡 Custom Filter နေရာ

namespace JWTDynamicRBAC.API.Features.Product
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

       // ==========================================
        // 1. CREATE (POST: api/Product)
        // ==========================================
        [Authorize]// ဒါလေး တပ်ထားမှသာ Token ကို Valid ဖြစ်မဖြစ် API က စစ်ပါမယ်
        [HttpPost]
        [Permission("Create_Product")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdProduct = await _productService.CreateProductAsync(request);
            
            return Ok(new 
            { 
                Message = "Product created successfully.", 
                Data = createdProduct 
            });
        }

        // ==========================================
        // 2. READ ALL (GET: api/Product)
        // ==========================================GetAllProductsAsync
        [Authorize]
        [HttpGet]
        [Permission("View_Product")]
        public async Task<IActionResult> GetAllProducts()
        {
            // Database ထဲမှ Product အားလုံးကို အမှန်တကယ် ဆွဲထုတ်မည်
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        // ==========================================
        // 3. READ BY ID (GET: api/Product/{id})
        // ==========================================
        [Authorize]
        [HttpGet("{id}")]
        [Permission("View_Product")] 
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            // ရှာမတွေ့ပါက 404 Not Found ပြန်ပေးမည်
            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found." });
            }

            return Ok(product);
        }

        // ==========================================
        // 4. UPDATE (PUT: api/Product/{id})
        // ==========================================
        [Authorize]
        [HttpPut("{id}")]
        [Permission("Edit_Product")] 
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto request)
        {
            if (id != request.Id)
            {
                return BadRequest(new { Message = "ID in URL does not match ID in request body." });
            }

            var isUpdated = await _productService.UpdateProductAsync(id, request);

            if (!isUpdated)
            {
                return NotFound(new { Message = $"Product with ID {id} not found." });
            }

            return Ok(new { Message = $"Product ID: {id} updated successfully." });
        }

        // ==========================================
        // 5. DELETE (DELETE: api/Product/{id})
        // ==========================================
        [Authorize]
        [HttpDelete("{id}")]
        [Permission("Delete_Product")] 
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var isDeleted = await _productService.DeleteProductAsync(id);

            if (!isDeleted)
            {
                return NotFound(new { Message = $"Product with ID {id} not found." });
            }

            return Ok(new { Message = $"Product ID: {id} deleted successfully." });
        }
    }
}