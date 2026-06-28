using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderBot.Data;
using OrderBot.Models;

namespace OrderBot.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Panel principal
        public async Task<IActionResult> Index()
        {
            var pedidos = await _context.Pedidos
                .OrderByDescending(p => p.FechaPedido)
                .Take(10)
                .ToListAsync();

            var totalPedidos = await _context.Pedidos.CountAsync();
            var totalProductos = await _context.Productos.CountAsync();
            var totalVentas = await _context.Pedidos.SumAsync(p => p.Total);

            ViewBag.TotalPedidos = totalPedidos;
            ViewBag.TotalProductos = totalProductos;
            ViewBag.TotalVentas = totalVentas;
            ViewBag.Pedidos = pedidos;

            return View();
        }

        // Lista de productos
        public async Task<IActionResult> Productos()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .ToListAsync();
            return View(productos);
        }

        // Crear producto - GET
        public async Task<IActionResult> CrearProducto()
        {
            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            return View();
        }

        // Crear producto - POST
        [HttpPost]
        public async Task<IActionResult> CrearProducto(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return RedirectToAction("Productos");
        }

        // Eliminar producto
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Productos");
        }

        // Cambiar estado de pedido
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                pedido.Estado = estado;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Cocina");
        }

        // Tablero de cocina
        public async Task<IActionResult> Cocina()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(p => p.Estado != "Entregado")
                .OrderBy(p => p.FechaPedido)
                .ToListAsync();
            return View(pedidos);
        }
    }
}