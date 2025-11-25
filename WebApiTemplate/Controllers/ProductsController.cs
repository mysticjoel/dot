using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly WenApiTemplateDbContext _db;

        public ProductsController(WenApiTemplateDbContext db)
        {
            _db = db;
        }

        // GET: api/products/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // POST: api/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            if (product == null) return BadRequest();

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, product);
        }
    }
}