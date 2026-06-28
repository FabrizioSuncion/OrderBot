namespace OrderBot.Models
{
    public class Conversacion
    {
        public int Id { get; set; }
        public DateTime FechaInicio { get; set; } = DateTime.Now;
        public string HistorialJson { get; set; } = "[]";
        public string ClienteNombre { get; set; } = "";
        public int? PedidoId { get; set; }
        public Pedido? Pedido { get; set; }
    }
}