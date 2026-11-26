using System.ComponentModel.DataAnnotations.Schema;

namespace WpfApp1.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Article { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public string Supplier { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public int Discount { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; }
        public string Photo { get; set; }

        [NotMapped]
        public bool HasLargeDiscount => Discount > 15;

        [NotMapped]
        public decimal FinalPrice => Price * (100 - Discount) / 100m;

        [NotMapped]
        public string FullPhotoPath =>
            "pack://application:,,,/WpfApp1;component/Images/" + (string.IsNullOrWhiteSpace(Photo) ? "picture.png" : Photo);
    }
}


