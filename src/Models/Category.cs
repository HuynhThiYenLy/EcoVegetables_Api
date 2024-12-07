namespace EcoVegetables_Api.src.Models
{
    public class Category
    {
        public int Id { get; set; } 
        public string Name { get; set; } // Tên danh mục

        // Khóa ngoại chỉ đến Parent
        public int? ParentId { get; set; }

        public Category? Parent { get; set; } // Tham chiếu đến danh mục cha
        public ICollection<Category> Children { get; set; } = new List<Category>(); // Danh sách các danh mục con
    }


}