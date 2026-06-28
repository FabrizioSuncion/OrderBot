using Microsoft.AspNetCore.Mvc;
using OrderBot.Services;
using System.Text.Json;

namespace OrderBot.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatbotService _chatbotService;

        public ChatController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Enviar([FromBody] MensajeRequest request)
        {
            try
            {
                var clienteNombre = request.ClienteNombre ?? "Cliente";
                var historial = request.Historial ?? "[]";

                var respuestaJson = await _chatbotService.ProcesarMensajeAsync(
                    request.Mensaje ?? "",
                    historial,
                    clienteNombre
                );

                using var doc = JsonDocument.Parse(respuestaJson);
                var accion = doc.RootElement.GetProperty("accion").GetString();
                var respuesta = doc.RootElement.GetProperty("respuesta").GetString();

                if (accion == "crear_pedido")
                {
                    var itemsElement = doc.RootElement.GetProperty("items");
                    var items = JsonSerializer.Deserialize<List<ItemPedido>>(itemsElement.GetRawText());
                    if (items != null && items.Count > 0)
                    {
                        var pedido = await _chatbotService.CrearPedidoAsync(items, clienteNombre);
                        return Json(new
                        {
                            respuesta = respuesta + $" (Pedido #{pedido.Id} registrado ✓)",
                            accion = "crear_pedido",
                            pedidoId = pedido.Id
                        });
                    }
                }

                return Json(new { respuesta, accion = "responder" });
            }
            catch (Exception ex)
            {
                return Json(new { respuesta = "Lo siento, ocurrió un error. Por favor intenta de nuevo.", accion = "error" });
            }
        }
    }

    public class MensajeRequest
    {
        public string? Mensaje { get; set; }
        public string? ClienteNombre { get; set; }
        public string? Historial { get; set; }
    }
}