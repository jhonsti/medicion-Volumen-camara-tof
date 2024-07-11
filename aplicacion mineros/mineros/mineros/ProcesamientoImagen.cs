using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;
using Size = OpenCvSharp.Size;

namespace mineros
{
    public class ProcesamientoImagen
    {
        public static Mat ImagenReferencia;
        public static List<double> PuntosFondox=new List<double>();
        public static List<double> PuntosFondoy = new List<double>();
        public static List<double> PuntosFondoz = new List<double>();
        public static int ContadorCapturaImagen=0;
        private static int ContadorGuardadoImagen = 0;
        /// <summary>
        /// Este metodo se usa para obtener la imagen que sera procesada pues la camara esta en streaming
        /// </summary>
        /// <param name="ImagenACapturar"></param>
        /// <returns>Retorna un true o false lo cual permitirá decisidir si se calculara el volumeno no
        /// a la imagen</returns>
        public static bool CapturarImagen(Mat ImagenACapturar)
        {
          
            bool imagenCapturada = false;

            if (ContadorCapturaImagen==2)
            {
                imagenCapturada = true;
                ContadorCapturaImagen = 0;
            }
            ContadorCapturaImagen++;
            return imagenCapturada;
        }

        public  static void CargasDatosDeReferencia()
        {
            ImagenReferencia = Cv2.ImRead("prueba0.jpg");
            PuntosFondox = valoresguardados("ListaX.txt");
            PuntosFondoy = valoresguardados("ListaY.txt");
            PuntosFondoz = valoresguardados("ListaZ.txt");

        }
        public static List<double> valoresguardados(string lista)
        {
            List<double> lista1;
            using (StreamReader sr = new StreamReader(lista))
            {
                string linea;
                lista1 = new List<double>();
                while ((linea = sr.ReadLine()) != null)
                {
                    if (double.TryParse(linea, out double valor))
                    {
                        lista1.Add(valor);
                    }
                }
            }
            return lista1;
        }


        // Función para obtener la lista de píxeles del objeto de interés
        static List<(int x, int y)> ObtenerPixelesObjetoInteres(Mat imagen)
        {
            List<(int x, int y)> pixelesObjetoInteres = new List<(int x, int y)>();

            // Recorrer todos los píxeles de la imagen
            for (int y = 0; y < imagen.Rows; y++)
            {
                for (int x = 0; x < imagen.Cols; x++)
                {
                    // Obtener el valor de intensidad del píxel en la imagen procesada
                    Vec3b color = imagen.At<Vec3b>(y, x);

                    // Si el píxel pertenece al objeto de interés (por ejemplo, si no es negro), agregarlo a la lista
                    if (color.Item0 != 0 || color.Item1 != 0 || color.Item2 != 0)
                    {
                        pixelesObjetoInteres.Add((x, y));
                    }
                }
            }

            return pixelesObjetoInteres;
        }
        static Mat SegmentarPorIntensidadDeGris(Mat imagen, int umbralInferior, int umbralSuperior)
        {
            // Crear una máscara para las regiones dentro del rango de intensidades de gris
            Mat mascara = new Mat();
            Cv2.InRange(imagen, new Scalar(umbralInferior), new Scalar(umbralSuperior), mascara);

            // Aplicar la máscara a la imagen original
            Mat imagenSegmentada = new Mat();
            Cv2.BitwiseAnd(imagen, imagen, imagenSegmentada, mascara);

            return imagenSegmentada;
        }
        static double VolumeOfTetrahedron(Point3D p1, Point3D p2, Point3D p3, double scale)
        {
            var v321 = p3.X * p2.Y * p1.Z;
            var v231 = p2.X * p3.Y * p1.Z;
            var v312 = p3.X * p1.Y * p2.Z;
            var v132 = p1.X * p3.Y * p2.Z;
            var v213 = p2.X * p1.Y * p3.Z;
            var v123 = p1.X * p2.Y * p3.Z;
            return (1.0 / 6.0) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }
        static void SaveMeshAsObj(MeshGeometry3D mesh, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var vertex in mesh.Positions)
                {
                    writer.WriteLine($"v {vertex.X} {vertex.Y} {vertex.Z}");
                }

                for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
                {
                    int index1 = mesh.TriangleIndices[i] + 1;
                    int index2 = mesh.TriangleIndices[i + 1] + 1;
                    int index3 = mesh.TriangleIndices[i + 2] + 1;
                    writer.WriteLine($"f {index1} {index2} {index3}");
                }
            }
        }
        public static double CalcularVolumen(List<(double, double, double)> CoordenadasObjeto)
        {
            var minvalorx = CoordenadasObjeto.Min(tupla => tupla.Item1);
            var minvalory = CoordenadasObjeto.Min(tupla => tupla.Item2);
            var minvalorz = CoordenadasObjeto.Min(tupla => tupla.Item3);
            var vertices = new Point3DCollection();
            foreach (var coordenada in CoordenadasObjeto)
            {
                vertices.Add(new Point3D((coordenada.Item1- minvalorx), (coordenada.Item2- minvalory), (coordenada.Item3)- minvalorz));
            }

            // Crear un mesh geometry
            var mesh = new MeshGeometry3D();
            mesh.Positions = vertices;

            // Generar los triángulos manualmente
            for (int i = 0; i < vertices.Count - 2; i++)
            {
                mesh.TriangleIndices.Add(i);
                mesh.TriangleIndices.Add(i + 1);
                mesh.TriangleIndices.Add(i + 2);
            }

            // Calcular el volumen sumando los volúmenes de todos los tetraedros
            double volumen = 0.0;
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                var p1 = mesh.Positions[mesh.TriangleIndices[i]];
                var p2 = mesh.Positions[mesh.TriangleIndices[i + 1]];
                var p3 = mesh.Positions[mesh.TriangleIndices[i + 2]];

                volumen += VolumeOfTetrahedron(p1, p2, p3, mesh.Bounds.SizeX);
            }
            SaveMeshAsObj(mesh, "C:\\Users\\dcnav\\Desktop\\Documentos\\GitHub\\FrontendMineros\\src\\assets\\images\\obj\\mesh" + ContadorGuardadoImagen++.ToString()+".obj");
            if (ContadorGuardadoImagen == 5)
            {
                ContadorGuardadoImagen = 0;
            }
            // Imprimir el resultado
            Console.WriteLine($"El volumen calculado es: {volumen/ mesh.TriangleIndices.Count}");
        

            return volumen;
           

        }
       
        //Funcion de segmentacion para calcular el volumen del objeto 
        public static List<(double, double, double)> segmentacionImagen(OpenCvSharp.Mat imagen, ArenaNET.IImage imagenArena, List<double> valoresx, List<double> valoresy,List<double>valoresz)
        {
            List<(double, double, double)> ObjetoInteres = new List<(double, double, double)>();
            try
            {

                Mat ImagenReferenciaGris = new Mat();
                Mat ImagenStreaming = new Mat();
                Cv2.CvtColor(imagen, ImagenStreaming, ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(ImagenReferencia, ImagenReferenciaGris, ColorConversionCodes.RGB2GRAY);

                Mat RestaImagenes = new Mat();
                Cv2.Subtract(ImagenStreaming, ImagenReferenciaGris, RestaImagenes);
                //Cv2.ImShow("resta", RestaImagenes);
                Mat imagenSegmentada = SegmentarPorIntensidadDeGris(RestaImagenes, 60, 130);

                Mat openedImage = new Mat();
                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(8, 8)); // tamaño del kernel
                Cv2.MorphologyEx(imagenSegmentada, openedImage, MorphTypes.Open, kernel);
                Cv2.Threshold(openedImage, openedImage, 0, 255, ThresholdTypes.Binary);


                Mat coloredMask = new Mat();
                Cv2.CvtColor(openedImage, openedImage, ColorConversionCodes.GRAY2BGR);


                Mat resultImage = new Mat();
                Cv2.BitwiseAnd(imagen, openedImage, resultImage);

                var pixelesObjetoInteres = ObtenerPixelesObjetoInteres(openedImage);


                Cv2.ImShow("imagen de resta", imagenSegmentada);
                Cv2.ImShow("streamin", resultImage);
                Cv2.WaitKey(1);
                foreach (var pixel in pixelesObjetoInteres)
                {
                    // Encuentra la posición correspondiente en la imagen original
                    int indice = pixel.y * imagen.Width + pixel.x;
                    ObjetoInteres.Add((Convert.ToUInt16(valoresx[indice]), Convert.ToUInt16(valoresy[indice]), Convert.ToUInt16(valoresz[indice])));
                    ObjetoInteres.Add((Convert.ToUInt16(PuntosFondox[indice]), Convert.ToUInt16(PuntosFondoy[indice]), Convert.ToUInt16(PuntosFondoz[indice])));

                }
                
                //var maximoValorZ = ObjetoInteres.Max(tupla => tupla.Item3);
                //var minvalorz = ObjetoInteres.Min(tupla => tupla.Item3);
                //var altura = maximoValorZ - minvalorz;
                //Console.WriteLine(altura);
                //string json = JsonConvert.SerializeObject(ObjetoInteres);
                //string filePath = "C:\\Users\\Usuario\\Documents\\jhonnatan\\intecol\\Demo_ToF\\imagen.json";

                //// Guardar los datos JSON en un archivo
                //File.WriteAllText(filePath, json);
           
                //Cv2.ImShow("apertura", openedImage);

            
              


            }
            catch { }
            
            return ObjetoInteres;
        }
    


    }
}
