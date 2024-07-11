using System.ComponentModel.DataAnnotations;

namespace BackMineros.Models
{
    public class Medicion
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int Volumen { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public required String Turno { get; set; }
    }
    public class ControlCamara
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int zmin { get; set; }
        [Required]
        public int zmax { get; set; }
        public int xmin { get; set; }
        public int xmax { get; set; }
        public int ymin { get; set; }
        public int ymax { get; set; }
        [Required]
        public Boolean estado { get; set; }
        [Required]
        public Boolean actualizarImagenReferencia { get; set; }
        [Required]
        public String pathImgReferencia { get; set;}
        [Required]
        public String pathExe { get; set; }

    }
}
