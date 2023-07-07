﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuizProject.Helpers;
using QuizProject.Models;

namespace QuizProject.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly QuizProjectContext _context;
        private readonly ICategoryHelper _categoryHelper;
        private readonly IMemoryCache _cache;
        private readonly DBHelper _dbHelper;
        public CategoriesController(QuizProjectContext context, ICategoryHelper categoryHelper, IMemoryCache cache)
        {
            _context = context;
            _categoryHelper = categoryHelper;
            _cache = cache;
            _dbHelper = new DBHelper(_context, _categoryHelper);
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<List<object>>> GetCategories()
        {
            var cacheKey = "categories";

            if (_cache.TryGetValue<List<object>>(cacheKey, out var cachedCategories))
            {
                // Trả về dữ liệu từ cache nếu đã tồn tại
                return cachedCategories!;
            }
            var ans = await _dbHelper.GetCategoriesFromDB();
            // Lưu dữ liệu vào cache 
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(3));
            _cache.Set(cacheKey, ans, cacheEntryOptions);
            return ans;
        }


        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            if (_context.Categories == null)
            {
                return NotFound();
            }
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return category;
        }

        // POST: api/Categories
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category, int? parentId)
        {
            _context.Categories.Add(category);
            try
            {
                await _context.SaveChangesAsync();
                parentId ??= category.CategoryId;
                _context.CategoryRelationships.Add(new CategoryRelationship { CategoryChildId = category.CategoryId, CategoryParentId = (int)parentId });
                await _context.SaveChangesAsync();
                _cache.Set("categories", await _dbHelper.GetCategoriesFromDB());
            }
            catch (DbUpdateException e)
            {
                return BadRequest(e.Message);
            }

            return CreatedAtAction("GetCategory", new { id = category.CategoryId }, category);
        }
    }
}