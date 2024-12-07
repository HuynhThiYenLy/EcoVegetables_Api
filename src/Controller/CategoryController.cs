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
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    return NotFound(new { message = "Danh mục không tồn tại" });
                }

                return Ok(new
                {
                    message = "Lấy thông tin danh mục thành công",
                    data = category
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

                // Cập nhật chỉ các trường được gửi
                if (!string.IsNullOrEmpty(updateRequest.Name))
                {
                    category.Name = updateRequest.Name; // Cập nhật tên nếu có
                }

                if (updateRequest.ParentId.HasValue && updateRequest.ParentId != id)
                {
                    category.ParentId = updateRequest.ParentId; // Cập nhật ParentId nếu khác null và không phải là chính nó
                }

                _context.Categories.Update(category); // Lưu thay đổi
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cập nhật thông tin danh mục thành công",
                    data = category
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình cập nhật thông tin", error = ex.Message });
            }
        }

        #endregion

        #region Xóa danh mục
        /// <summary>
        /// Xóa danh mục
        /// </summary>
        [HttpDelete("delete-category/{id}")]
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
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình xóa thông tin", error = ex.Message });
            }
        }
        #endregion
    }
}
