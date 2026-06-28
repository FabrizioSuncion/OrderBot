using System.Text;
using System.Text.Json;
using OrderBot.Data;
using OrderBot.Models;
using Microsoft.EntityFrameworkCore;

namespace OrderBot.Services
{
    public class ChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ApplicationDbContext _context;

        public ChatbotService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? "";
            _model = configuration["OpenAI:Model"] ?? "gpt-4o";
            _context = context;
        }

        public async Task<string> ProcesarMensajeAsync(string mensaje, string historialJson, string clienteNombre)
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Disponible)
                .ToListAsync();

            var menu = productos.Select(p => new
            {
                nombre = p.Nombre,
                precio = p.Precio,
                descripcion = p.Descripcion,
                categoria = p.Categoria != null ? p.Categoria.Nombre : ""
            });

            var menuJson = JsonSerializer.Serialize(menu);
            var formatoRespuesta = "{\"accion\": \"responder\", \"respuesta\": \"texto al cliente\", \"items\": []}";
            var formatoPedido = "{\"accion\": \"crear_pedido\", \"respuesta\": \"confirmacion\", \"items\": [{\"nombre\": \"Hamburguesa\", \"cantidad\": 2, \"nota\": \"sin cebolla\"}]}";

            var systemPrompt = "Eres el asistente de pedidos de OrderBot para restaurantes. " +
                "Hablas en español peruano de manera amigable y natural. " +
                "El cliente se llama " + clienteNombre + ". " +
                "MENU DISPONIBLE: " + menuJson + ". " +
                "INSTRUCCIONES: Ayuda al cliente a realizar su pedido conversacionalmente. " +
                "Responde SIEMPRE en formato JSON. " +
                "Para respuestas normales usa: " + formatoRespuesta + ". " +
                "Para crear un pedido confirmado usa: " + formatoPedido + ". " +
                "Siempre confirma el pedido antes de crearlo. " +
                "Si el cliente confirma, usa accion crear_pedido con los items.";

            var historial = new List<object>();
            try
            {
                historial = JsonSerializer.Deserialize<List<object>>(historialJson) ?? new List<object>();
            }
            catch { }

            var messages = new List<object>();
            messages.Add(new { role = "system", content = systemPrompt });
            messages.AddRange(historial);
            messages.Add(new { role = "user", content = mensaje });

            var requestBody = new
            {
                model = _model,
                messages = messages,
                max_tokens = 500,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);

            var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine("GROQ RESPONSE: " + responseBody);

            using var doc = JsonDocument.Parse(responseBody);
            var respuesta = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            // Limpiar si el modelo envuelve el JSON en markdown
            respuesta = respuesta.Trim();
            if (respuesta.StartsWith("```json"))
                respuesta = respuesta.Substring(7);
            if (respuesta.StartsWith("```"))
                respuesta = respuesta.Substring(3);
            if (respuesta.EndsWith("```"))
                respuesta = respuesta.Substring(0, respuesta.Length - 3);
            respuesta = respuesta.Trim();

            // Verificar que sea JSON válido, si no crear uno por defecto
            try
            {
                JsonDocument.Parse(respuesta);
            }
            catch
            {
                respuesta = "{\"accion\":\"responder\",\"respuesta\":\"" +
                    respuesta.Replace("\"", "'") + "\",\"items\":[]}";
            }

            return respuesta;
        }

        public async Task<Pedido> CrearPedidoAsync(List<ItemPedido> items, string clienteNombre)
        {
            var pedido = new Pedido
            {
                ClienteNombre = clienteNombre,
                FechaPedido = DateTime.Now,
                Estado = "Pendiente"
            };

            decimal total = 0;
            foreach (var item in items)
            {
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Nombre.ToLower().Contains(item.Nombre.ToLower()));

                if (producto != null)
                {
                    var subtotal = producto.Precio * item.Cantidad;
                    total += subtotal;
                    pedido.Detalles.Add(new DetallePedido
                    {
                        ProductoId = producto.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = producto.Precio,
                        Subtotal = subtotal,
                        Observaciones = item.Nota
                    });
                }
            }

            pedido.Total = total;
            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();
            return pedido;
        }
    }

    public class ItemPedido
    {
        public string Nombre { get; set; } = "";
        public int Cantidad { get; set; }
        public string Nota { get; set; } = "";
    }
}