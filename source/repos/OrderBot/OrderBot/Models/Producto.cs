namespace OrderBot.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Precio { get; set; }
        public bool Disponible { get; set; } = true;
        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }
    }
}