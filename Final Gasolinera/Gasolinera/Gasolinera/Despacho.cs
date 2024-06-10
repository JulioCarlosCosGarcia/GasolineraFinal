using System;

namespace Gasolinera
{
    internal class Despacho
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime Hora { get; set; }
        public Bomba NombreB { get; set; }
        public Cliente Nombre { get; set; }
        public string TipoDespacho { get; set; }
        public decimal PrecioDia { get; set; }
        public decimal CantidadLitro { get; set; }
        public decimal Total { get; set; }

        public Despacho()
        {
            Id = 0;
            Fecha = DateTime.Now;
            Hora = DateTime.Now;
            TipoDespacho = "";
            PrecioDia = 0;
            CantidadLitro = 0;
            Total = 0;
        }
    }
}
