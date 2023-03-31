using AutoMapper;
using BlazorPeliculas.Server.Helpers;
using BlazorPeliculas.Shared.DTO;
using BlazorPeliculas.Shared.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlazorPeliculas.Server.Controllers
{
    [ApiController]
    [Route("api/peliculas")]
    public class PeliculasController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly IMapper mapper;
        private readonly string contenedor = "peliculas";

        public PeliculasController(ApplicationDbContext context, IAlmacenadorArchivos almacenadorArchivos,
            IMapper mapper)
        {
            this.context = context;
            this.almacenadorArchivos = almacenadorArchivos;
            this.mapper = mapper;
        }
        [HttpGet]
        public async Task<ActionResult<HomaPageDTO>> Get()
        {
            var limite = 6;

            var peliculasenCartelera = await context.Peliculas.Where(X => X.EnCartelera).Take(limite).OrderByDescending(x => x.Lanzamiento).ToListAsync();

            var fechaActual = DateTime.Today;

            var proximosEstrenos = await context.Peliculas.Where(x => x.Lanzamiento > fechaActual).OrderBy(x => x.Lanzamiento).Take(limite).ToListAsync();

            var resultado = new HomaPageDTO
            {
                PeliculasEnCartelera = peliculasenCartelera,
                ProximosEstrenos = proximosEstrenos
            };
            return resultado;
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PeliculaVisualizarDTO>> Get(int id)
        {
            var pelicula = await context.Peliculas.Where(x => x.Id == id)
                .Include(x => x.GenerosPelicula).ThenInclude(gp => gp.Genero)
                .Include(x => x.PeliculasActor.OrderBy(pa => pa.Orden)).ThenInclude(pa => pa.Actor).FirstOrDefaultAsync();
            if (pelicula is null)
            {
                return NotFound();
            }
            //TODO: sistema de votacion
            var promedioVoto = 4;
            var votoUsuario = 5;

            var modelo = new PeliculaVisualizarDTO();
            modelo.Pelicula = pelicula;
            modelo.Generos = pelicula.GenerosPelicula.Select(x => x.Genero!).ToList();
            modelo.Actores = pelicula.PeliculasActor.Select(x => new Actor { Nombre = x.Actor!.Nombre, Foto = x.Actor.Foto, Personaje = x.Personaje, Id = x.ActorId }).ToList();
            modelo.VotoPromedio = promedioVoto;
            modelo.VotoUsuario = votoUsuario;

            return modelo;

        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(Pelicula pelicula)
        {
            if (!string.IsNullOrEmpty(pelicula.Poster))
            {
                var poster = Convert.FromBase64String(pelicula.Poster);
                pelicula.Poster = await almacenadorArchivos.GuardarArchivo(poster, ".jpg", contenedor);
            }
            OrdenActores(pelicula);

            context.Add(pelicula);
            await context.SaveChangesAsync();
            return pelicula.Id;
        }

        private static void OrdenActores(Pelicula pelicula)
        {
            if (pelicula.PeliculasActor is not null)
            {
                for (int i = 0; i < pelicula.PeliculasActor.Count; i++)
                {
                    pelicula.PeliculasActor[i].Orden = i + 1;
                }
            }
        }

        [HttpGet("actualizar/{id:int}")]
        public async Task<ActionResult<PeliculaActualizacionDTO>> PutGet(int id)
        {
            var peliculaActionResult = await Get(id);
            if (peliculaActionResult.Result is NotFoundResult)
            {
                return NotFound();
            }
            var peliculaVisualizarDTO = peliculaActionResult.Value;
            var generosSeleccionadosIds = peliculaVisualizarDTO!.Generos.Select(x => x.Id).ToList();
            var generosNoSeleccionados = await context.Generos.Where(x => !generosSeleccionadosIds.Contains(x.Id)).ToListAsync();
            var modelo = new PeliculaActualizacionDTO();
            modelo.pelicula = peliculaVisualizarDTO.Pelicula;
            modelo.GenerosNoSeleccionados = generosNoSeleccionados;
            modelo.GenerosSeleccionados = peliculaVisualizarDTO.Generos;
            modelo.Actores = peliculaVisualizarDTO.Actores;
            return modelo;
        }

        [HttpPut]
        public async Task<ActionResult> Put(Pelicula pelicula)
        {
            var peliculaDB= await context.Peliculas.Include(x=>x.GenerosPelicula)
                .Include(x=>x.PeliculasActor)
                .FirstOrDefaultAsync(x=> x.Id==pelicula.Id);
            if(peliculaDB is null)
            {
                return NotFound();
            }
            peliculaDB = mapper.Map(pelicula, peliculaDB);

            if(!string.IsNullOrWhiteSpace(pelicula.Poster))
            {
                var posterImagen = Convert.FromBase64String(pelicula.Poster);
                peliculaDB.Poster = await almacenadorArchivos.EditarArchivo(posterImagen, ".jpg", contenedor, peliculaDB.Poster!);
            }
            OrdenActores(peliculaDB);
            await context.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var pelicula = await context.Peliculas.FirstOrDefaultAsync(x => x.Id == id);
            if(pelicula is null)
            {
                return NotFound();
            }
            context.Remove(pelicula);
            await context.SaveChangesAsync();
            await almacenadorArchivos.EliminarArchivo(pelicula.Poster!, contenedor);

            return NoContent();
        }
    }
}
