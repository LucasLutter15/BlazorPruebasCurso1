using BlazorPeliculas.Shared.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorPeliculas.Shared.DTO
{
    public class PeliculaVisualizarDTO
    {
        public Pelicula Pelicula { get; set; } = null!;
        public List<Actor> Actores { get; set; }= new List<Actor>();
        public List<Genero> Generos { get; set; } = new List<Genero>();

        public int VotoUsuario { get; set; }
        public double VotoPromedio { get; set; }
    }
}
