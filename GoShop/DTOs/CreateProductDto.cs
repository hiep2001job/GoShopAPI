using System.ComponentModel.DataAnnotations;

namespace GoShop.DTOs
{
    public class CreateProductDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(100, double.PositiveInfinity)]
        public long Price { get; set; }

       
        public IFormFile? File { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string Brand { get; set; }


        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityInStock { get; set; }
    }
}
