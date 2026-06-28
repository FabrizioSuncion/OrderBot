namespace OrderBot.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public DateTime FechaPedido { get; set; } = DateTime.Now;
        public string Estado { get; set; } = "Pendiente";
        public decimal Total { get; set; }
        public string Observaciones { get; set; } = "";
        public string ClienteNombre { get; set; } = "";
        public List<DetallePedido> Detalles { get; set; } = new();
    }
}