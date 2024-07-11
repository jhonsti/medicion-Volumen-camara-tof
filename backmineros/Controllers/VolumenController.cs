using BackMineros.Data;
using BackMineros.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Cryptography;
using System.Diagnostics;

namespace BackMineros.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VolumenController : Controller
    {
        private readonly ApplicationDBContext _context;

        public VolumenController(ApplicationDBContext context)
        {
            _context = context;
        }
        //Obtiene todos los volumenes.
        [HttpGet("volumen")]
        public IActionResult GetVolumens()
        {
            var volumenes = _context.Mediciones.ToList(); // Esto cargará todos los clientes en memoria

            // Devuelve los clientes como resultado de la solicitud
            return Ok(volumenes);
        }
        //Se usa para llenar la base de datos.
        [HttpPost]
        public async Task<IActionResult> AddVolumen(Medicion medicion)
        {
            _context.Mediciones.Add(medicion);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registro insertado correctamente" });
        }
        //Se usa para eliminar mediciones.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVolumen(int id)
        {
            var volumen = await _context.Mediciones.FindAsync(id);
            if (volumen == null)
            {
                return NotFound();
            }
            _context.Mediciones.Remove(volumen);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        //Se usa para obtener todas las mediciones entre dos fechas dadas. No es inclusivo.
        [HttpGet("consulta")]
        public async Task<IActionResult> ConsultaEntreFechas(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                // Realizar la consulta utilizando LINQ y Entity Framework
                List<Medicion> result = await _context.Mediciones.Where(e => e.Date >= fechaInicio && e.Date <= fechaFin).ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al realizar la consulta: {ex.Message}");
            }
        }
        //Entre una lista de todas las mediciones por turno.
        [HttpGet("VolumenPorTurno")]
        public async Task<IActionResult> ConsultaPorTurno(String turno)
        {
            try
            {
                // Realizar la consulta utilizando LINQ y Entity Framework
                var resultados = await _context.Mediciones.Where(e => e.Turno == turno).ToListAsync();

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al realizar la consulta: {ex.Message}");
            }
        }
        //Entrega la suma de los volumenes de cada turno.
        [HttpGet("VolumenPorTurnoResumen")]
        public IActionResult ConsultaPorTurnoResumen()
        {
            try
            {
                List<Medicion> datosList = _context.Mediciones.ToList();

                var turnosCount = datosList
                    .GroupBy(d => d.Turno)
                    .Select(g => new { Turno = g.Key, Sum = g.Sum(item => Convert.ToInt64(item.Volumen)) })
                    .Select(result => new { name = result.Turno, y = Math.Abs(result.Sum) })
                    .ToList();
                return Ok(turnosCount);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al realizar la consulta: {ex.Message}. VolumenPorTurno");
            }
        }
        //Entrega la ultima medicion guardada en la base de datos.
        [HttpGet("ultimo")]
        public async Task<IActionResult> UltimoVolumen()
        {
            try
            {
                // Ordenar los datos por algún criterio (por ejemplo, fecha de creación) de forma descendente
                var ultimoDato = await _context.Mediciones
                    .OrderByDescending(e => e.Date)
                    .FirstOrDefaultAsync();

                if (ultimoDato == null)
                {
                    return NotFound(); // Devolver 404 si no se encontraron datos
                }

                return Ok(ultimoDato); // Devolver el último dato encontrado
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener el último dato: {ex.Message}");
            }
        }
        //Actualiza el estado de la camara en base de datos, regresa un mensaje con el nuevo estado.
        [HttpPut("UpdateEstado")]
        public async Task<IActionResult> UpdateEstado()
        {
            var primerRegistro = await _context.ControlCamara.FirstOrDefaultAsync();
            String message;
            int status;
            if (primerRegistro != null)
            {
                Boolean estado = primerRegistro.estado;

                if (estado == false)
                {
                    // Iniciar la aplicación
                    primerRegistro.estado = true;
                    message = "Se inició la aplicación satisfactoriamente.";

                    ProcessStartInfo proc = new ProcessStartInfo();
                    proc.UseShellExecute = true;
                    proc.WorkingDirectory = "C:\\Users\\PC-MINEROS\\Despliegue IVIS\\Medicion de volumen\\Release";
                    proc.FileName = "C:\\Users\\PC-MINEROS\\Despliegue IVIS\\Medicion de volumen\\Release\\mineros.exe";
                    //proc.Verb = "runas";

                    Process proceso = Process.Start(proc);
                    //proceso.WaitForExit();
                }
                else
                {
                    // Detener la aplicación
                    primerRegistro.estado = false;
                    message = "Se detuvo la aplicación satisfactooriamente.";
                }

                status = 200;
                await _context.SaveChangesAsync();
            }
            else
            {
                status = 400;
                message = "¡Error! La tabla ControlCamara está vacia.";
            }

            return StatusCode(status, new { message = message });
        }
        //Envia el estado actual registrado en la base de datos de la aplicacion.
        [HttpGet("estado")]
        public async Task<IActionResult> GetState()
        {
            var primerRegistro = await _context.ControlCamara.FirstOrDefaultAsync();
            if (primerRegistro != null)
            {
                return Ok(primerRegistro.estado);
            }
            else
            {
                return StatusCode(500, new { message = "¡Error! La tabla ControlCamara está vacia." });
            }
        }
        //Devuelve la suma mensual de cada mes.
        [HttpGet("GetTurnosTotalMes")]
        public async Task<IActionResult> GetSumMonth()
        {
            try
            {
                var datosList = await _context.Mediciones.ToListAsync();
                DateTime fechaInicio = DateTime.Now.AddMonths(-1); // Fecha actual menos un mes
                DateTime fechaFin = DateTime.Now;
                var turnosCount = datosList
                    .Where(d => d.Date >= fechaInicio && d.Date <= fechaFin)
                    .GroupBy(d => d.Turno)
                    .Select(g => new { name = g.Key, y = g.Sum(item => Convert.ToInt64(item.Volumen)) })
                    .ToList();
                return StatusCode(200, turnosCount);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al realizar la consulta: {ex.Message}. GetTurnosTotalMes");
            }
        }
        [HttpGet("GetByMonth_Week")]
        public async Task<IActionResult> GetByMonth()
        {
            try
            {
                DateTime fechaInicio = new DateTime(DateTime.Now.Year, 1, 1);

                DateTime fechaFin = DateTime.Now;
                var datosList = await _context.Mediciones.ToListAsync();

                var resultadosPorMes = datosList
                                    .Where(d => d.Date >= fechaInicio && d.Date <= fechaFin)
                                    .GroupBy(d => new { Mes = d.Date.Month, Turno = d.Turno })
                                    .Select(g => new { Mes = g.Key.Mes, Turno = g.Key.Turno, Sum = g.Sum(item => Convert.ToInt64(item.Volumen)) })
                                    .ToList();

                var resultadosFormato = resultadosPorMes
                                    .GroupBy(r => r.Turno)
                                    .ToDictionary(
                                        g => g.Key.ToLower(),
                                        g => Enumerable.Range(1, 12)
                                            .Select(mes => g.Where(r => r.Mes == mes).Sum(r => r.Sum)).ToList());

                var jsonResultados = JsonConvert.SerializeObject(resultadosFormato, Formatting.Indented);

                return StatusCode(200, jsonResultados);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al realizar la consulta: {ex.Message}. GetByMonth_Week");
            }
        }

        [HttpGet("GetByWeek")]
        public async Task<IActionResult> GetByWeek()
        {
            try
            {
                // Obtener la fecha de inicio (hace 7 días)
                DateTime fechaInicio = DateTime.Now.AddDays(-6);

                // Obtener la fecha de fin (hoy)
                DateTime fechaFin = DateTime.Now;

                // Obtener los datos de la base de datos
                var datosList = await _context.Mediciones.ToListAsync();

                // Filtrar los datos por el período de los últimos 7 días y sumar el volumen por día
                var volumenesPorDia = datosList
                    .Where(d => d.Date >= fechaInicio && d.Date <= fechaFin)
                    .GroupBy(d => d.Date.Date) // Agrupar por día
                    .OrderBy(g => g.Key) // Ordenar por día
                    .Select(g => g.Sum(item => Convert.ToInt64(item.Volumen))) // Obtener la suma de volumen por día
                    .ToList();

                // Convertir la lista de volúmenes por día a un array y luego serializarlo a JSON
                var jsonResultados = JsonConvert.SerializeObject(volumenesPorDia.ToArray(), Formatting.Indented);

                // Devolver los resultados
                return StatusCode(200, jsonResultados);
            }
            catch (Exception ex)
            {
                // Manejar errores
                return StatusCode(500, $"Error al realizar la consulta: {ex.Message}. GetByWeek");
            }
        }
    }
}
