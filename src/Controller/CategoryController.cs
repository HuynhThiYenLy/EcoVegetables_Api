using EcoVegetables_Api.src.Models;
using ecovegetables_api.src.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoVegetables_Api.src.Request.Category;

namespace EcoVegetables_Api.src.Controller
{
    [ApiController]
    [Route("category")]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        #region add
        [HttpPost("add")]
        public async Task<IActionResult> AddCategory([FromBody] Category category)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(category.Name))
                {
                    return BadRequest(new { message = "Tên danh mục không được để trống" });
                }

                // Thêm danh mục mới vào cơ sở dữ liệu
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Thêm danh mục thành công",
                    data = category
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình thêm danh mục", error = ex.Message });
            }
        }
        #endregion

        #region getById
        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            try
            {
                // Lấy thông tin danh mục theo id
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    return NotFound(new { message = "Danh mục không tồn tại" });
                }

                // Nếu danh mục có parentId, lấy thông tin tên của danh mục cha
                string parentCategoryName = null;
                if (category.ParentId.HasValue) // Kiểm tra nếu ParentId không null
                {
                    var parentCategory = await _context.Categories.FindAsync(category.ParentId.Value);
                    parentCategoryName = parentCategory?.Name; // Nếu parentCategory tồn tại, lấy tên
                }

                return Ok(new
                {
                    message = "Lấy thông tin danh mục thành công",
                    data = new
                    {
                        category.Id,
                        category.Name,
                        category.ParentId,
                        ParentName = parentCategoryName // Thêm tên danh mục cha vào response
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình lấy thông tin danh mục", error = ex.Message });
            }
        }
        #endregion

        #region update
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest updateRequest)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    return NotFound(new { message = "Danh mục không tồn tại" });
                }

                // Kiểm tra nếu cần cập nhật ParentId
                if (updateRequest.ParentId.HasValue)
                {
                    if (updateRequest.ParentId == id)
                    {
                        return BadRequest(new { message = "ParentId không được trùng với Id của chính danh mục." });
                    }

                    // Kiểm tra ParentId có tồn tại hay không
                    var parentCategory = await _context.Categories.FindAsync(updateRequest.ParentId.Value);
                    if (parentCategory == null)
                    {
                        return NotFound(new { message = "Danh mục cha không tồn tại." });
                    }

                    category.ParentId = updateRequest.ParentId; // Cập nhật ParentId nếu hợp lệ
                }

                // Cập nhật tên nếu có
                if (!string.IsNullOrEmpty(updateRequest.Name))
                {
                    category.Name = updateRequest.Name;
                }

                // Cập nhật trong cơ sở dữ liệu
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cập nhật thông tin danh mục thành công",
                    data = new
                    {
                        category.Id,
                        category.Name,
                        category.ParentId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình cập nhật thông tin", error = ex.Message });
            }
        }
        #endregion


        #region delete
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    return NotFound(new { message = "Danh mục không tồn tại" });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa danh mục thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình xóa thông tin",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
        #endregion

        #region getCateParent
        [HttpGet("getCateParent")]
        public async Task<IActionResult> GetCategoryParent()
        {
            try
            {
                // Lấy danh mục gốc và đếm số danh mục con
                var rootCategories = await _context.Categories
                                                   .Where(c => c.ParentId == null)
                                                   .Select(c => new
                                                   {
                                                       c.Id,
                                                       c.Name,
                                                       ChildrenCount = _context.Categories.Count(child => child.ParentId == c.Id)
                                                   })
                                                   .ToListAsync();

                if (!rootCategories.Any())
                {
                    return NotFound(new { message = "Không có danh mục gốc nào được tìm thấy" });
                }

                return Ok(new
                {
                    message = "Lấy danh sách danh mục gốc thành công",
                    data = rootCategories
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình lấy danh mục gốc", error = ex.Message });
            }
        }
        #endregion

    }
}
