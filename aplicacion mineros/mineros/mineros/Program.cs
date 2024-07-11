/***************************************************************************************
 ***                                                                                 ***
 ***  Copyright (c) 2023, Lucid Vision Labs, Inc.                                    ***
 ***                                                                                 ***
 ***  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR     ***
 ***  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,       ***
 ***  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE    ***
 ***  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER         ***
 ***  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  ***
 ***  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE  ***
 ***  SOFTWARE.                                                                      ***
 ***                                                                                 ***
 ***************************************************************************************/
using ArenaNET;
using Newtonsoft.Json;
using NumSharp;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
using System.Collections;
using System.Configuration;
using OpenCvSharp;
using mineros;

namespace mineros
{
    class camara
    {
        const String TAB1 = "  ";

        public static List<NDArray> xyzList = new List<NDArray>();
        const String FILE_NAME = "Cs_Save_Ply/Cs_Save_Ply.ply";
        private static Form form;
        private static PlotView plotView;
        const ArenaNET.EPfncFormat PIXEL_FORMAT = ArenaNET.EPfncFormat.BGR8;
        const UInt32 LUCID_PolarizedDolpAolp_Mono8 = 0x8210009F;
        public static int zmin;
        public static int zmax;
        public static bool IniciarCaptura;
        public static bool ActualizarDatosDereferencia;


        // =-=-=-=-=-=-=-=-=-
        // =-=- EXAMPLE -=-=-
        // =-=-=-=-=-=-=-=-=-        

        static bool ValidateDevice(ArenaNET.IDevice device)
        {
            try
            {
                // validate if Scan3dCoordinateSelector node exists. If not -
                // probaly not Helios camera used running the example
                var checkScan3dCoordinateSelectorNode = (ArenaNET.IEnumeration)device.NodeMap.GetNode("Scan3dCoordinateSelector");
            }
            catch (Exception)
            {
                Console.WriteLine("{0}Scan3dCoordinateSelector node is not found. Please make sure that Helios device is used for the example.\n", TAB1);
                return false;
            }

            try
            {
                // validate if Scan3dCoordinateOffset node exists. If not -
                // probaly Helios has an old firmware
                var checkScan3dCoordinateOffset = (ArenaNET.IFloat)device.NodeMap.GetNode("Scan3dCoordinateOffset");
            }
            catch (Exception)
            {
                Console.WriteLine("{0}Scan3dCoordinateOffset node is not found. Please update Helios firmware.\n", TAB1);
                return false;
            }

            return true;
        }

        // demonstrates saving an image
        // (1) converts image to a displayable pixel format
        // (2) prepares image parameters
        // (3) prepares image writer
        // (4) saves image
        // (5) destroys converted image
        static void SaveImage(ArenaNET.IImage image, String filename)
        {

            bool isSignedPixelFormat = false;

            if (image.PixelFormat == ArenaNET.EPfncFormat.Coord3D_ABC16s || image.PixelFormat == ArenaNET.EPfncFormat.Coord3D_ABCY16s)
            {
                isSignedPixelFormat = true;
            }

            Console.WriteLine("{0}Prepare image parameters", TAB1);

            SaveNET.ImageParams parameters = new SaveNET.ImageParams(
                image.Width,
                image.Height,
                image.BitsPerPixel,
                true);


            Console.WriteLine("{0}Prepare image writer", TAB1);

            SaveNET.ImageWriter writer = new SaveNET.ImageWriter(parameters, filename);

            // set default parameters for SetPly()
            bool filterPoints = true;
            float scale = 0.25f;
            float offsetA = 0.0f;
            float offsetB = 0.0f;
            float offsetC = 0.0f;

            // set the output file format of the image writer to .ply
            writer.SetPly(".ply", filterPoints, isSignedPixelFormat, scale, offsetA, offsetB, offsetC);

            // Save
            //    Passing image data into the image writer using the Save()
            //    function triggers a save. todo - creates a dir if not already
            //    existing
            Console.WriteLine("{0}Save image", TAB1);

            writer.Save(image.DataArray, true);
        }

        // =-=-=-=-=-=-=-=-=-
        // =- PREPARATION -=-
        // =- & CLEAN UP =-=-
        // =-=-=-=-=-=-=-=-=-

        static void Main(string[] args)
        {
            try
            {
                // prepare example
                ArenaNET.ISystem system = ArenaNET.Arena.OpenSystem();
                system.UpdateDevices(100);
                if (system.Devices.Count == 0)
                {
                    Console.WriteLine("\nNo camera connected\nPress enter to complete");
                    Console.Read();
                    return;
                }
                ArenaNET.IDevice device = system.CreateDevice(system.Devices[0]);

                // enable stream auto negotiate packet size
                var streamAutoNegotiatePacketSizeNode = (ArenaNET.IBoolean)device.TLStreamNodeMap.GetNode("StreamAutoNegotiatePacketSize");
                streamAutoNegotiatePacketSizeNode.Value = true;

                // enable stream packet resend
                var streamPacketResendEnableNode = (ArenaNET.IBoolean)device.TLStreamNodeMap.GetNode("StreamPacketResendEnable");
                streamPacketResendEnableNode.Value = true;

                var Scan3dCoordinateSelectorNode = (ArenaNET.IEnumeration)device.NodeMap.GetNode("Scan3dCoordinateSelector");
                var Scan3dCoordinateScaleNode = (ArenaNET.IFloat)device.NodeMap.GetNode("Scan3dCoordinateScale");
                float scale = (float)Scan3dCoordinateScaleNode.Value;
                var pixelFormatNode = (ArenaNET.IEnumeration)device.NodeMap.GetNode("PixelFormat");
                pixelFormatNode.FromString("Coord3D_ABCY16");//Coord3D_ABCY16   Coord3D_C16
                var operatingModeNode = (ArenaNET.IEnumeration)device.NodeMap.GetNode("Scan3dOperatingMode");
                String operatingModeInitial = operatingModeNode.Entry.Symbolic;
                operatingModeNode.FromString("Distance1250mmSingleFreq");

                bool isHelios2 = false;
                var deviceModelNameNode = (ArenaNET.IString)device.NodeMap.GetNode("DeviceModelName");
                String deviceModelName = deviceModelNameNode.Value;
                if (deviceModelName.StartsWith("HLT") || deviceModelName.StartsWith("HTP"))
                {
                    isHelios2 = true;
                }

                //Application.Run(form);
                //Thread appThread = new Thread(() =>
                //{
                //    // Crear el formulario y configurar el PlotView
                //    form = new Form();
                //    form.Size = new System.Drawing.Size(800, 600);

                //    plotView = new PlotView();
                //    plotView.Dock = DockStyle.Fill;
                //    form.Controls.Add(plotView);

                //    // Ejecutar la aplicación sin detener el hilo principal
                //    Application.Run(form);
                //});
                //appThread.Start();
                ///*********************************************************************************************************
                device.StartStream();
                bool PrimerEncendidoAplicacion = true;
                bool CalcularVolumen = false;
                //bool InstanciaPython = true;
                ProcesamientoImagen.CargasDatosDeReferencia();
                //IniciarCaptura ESCRIBIR EN BASE DE DATOS EL TRUE
                ConexionBaseDeDatos.ObtenerDatos();
                

                    while (IniciarCaptura || PrimerEncendidoAplicacion)
                    //for (UInt32 i = 0; i < 5; i++)
                    {
                        try
                        {
                            Console.WriteLine("Jjajajajajaja");

                            ArenaNET.IImage image = device.GetImage(2000);

                            bool isDeviceValid = ValidateDevice(device);

                            if (isDeviceValid == true)
                            {
                                if (image.PixelFormat == ArenaNET.EPfncFormat.Coord3D_ABC16 || image.PixelFormat == ArenaNET.EPfncFormat.Coord3D_ABCY16 || image.PixelFormat == ArenaNET.EPfncFormat.Coord3D_ABC16s || image.PixelFormat == ArenaNET.EPfncFormat.Coord3D_ABCY16s)
                                {


                                    (ArenaNET.IImage imagen, var x, var y, var z) = convertirHSV(image, scale, isHelios2);
                                    if (imagen != null)
                                    {
                                        using (System.Drawing.Bitmap bitmap = imagen.Bitmap)
                                        {

                                            byte[] imageData;
                                            using (MemoryStream stream = new MemoryStream())
                                            {
                                                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                                                imageData = stream.ToArray();
                                            }
                                            OpenCvSharp.Mat im = OpenCvSharp.Cv2.ImDecode(imageData, OpenCvSharp.ImreadModes.Color);
                                            OpenCvSharp.Cv2.CvtColor(im, im, OpenCvSharp.ColorConversionCodes.BGR2RGB);

                                            if (ActualizarDatosDereferencia)
                                            {
                                                //string FILE_NAME1 = "C:\\Users\\Usuario\\Documents\\jhonnatan\\Github\\VolumenMineros\\aplicacion mineros\\mineros\\prueba" + ".jpg";
                                                Cv2.ImWrite("prueba0" + ".jpg", im);
                                                File.WriteAllLines("ListaX.txt", x.Select(d => d.ToString()));
                                                File.WriteAllLines("ListaY.txt", y.Select(d => d.ToString()));
                                                File.WriteAllLines("ListaZ.txt", z.Select(d => d.ToString()));
                                                ProcesamientoImagen.CargasDatosDeReferencia();
                                                ActualizarDatosDereferencia = false;
                                                ConexionBaseDeDatos.ActualizarDatosCamara();
                                            }

                                            CalcularVolumen = ProcesamientoImagen.CapturarImagen(im);
                                          

                                            if (CalcularVolumen)
                                            {
                                                var Puntos = ProcesamientoImagen.segmentacionImagen(im, imagen, x, y, z);
                                                var VolumenCalculado = ProcesamientoImagen.CalcularVolumen(Puntos);
                                                ConexionBaseDeDatos.Medicion(VolumenCalculado);
                                                //string json = JsonConvert.SerializeObject(Puntos);
                                                //string filePath = "C:\\Users\\Usuario\\Documents\\jhonnatan\\intecol\\Demo_ToF\\imagen.json";

                                                //// Guardar los datos JSON en un archivo
                                                //File.WriteAllText(filePath, json);

                                            }
                                            //NDArray Arraysalida = Puntos.astype(NPTypeCode.Double);

                                            //Visualizar(Arraysalida / 10500);

                                            //OpenCvSharp.Cv2.ImShow("streaming camara tof", im);
                                            //OpenCvSharp.Cv2.WaitKey(1);
                                            bitmap.Dispose();
                                            im.Dispose();
                                            ArenaNET.ImageFactory.Destroy(imagen);
                                        }
                                    }

                                }
                            }
                            else
                            {
                                Console.WriteLine("This example requires camera to be in a 3D image format like Coord3D_ABC16, Coord3D_ABCY16, Coord3D_ABC16s or Coord3D_ABCY16s\n");

                            }


                            // clean up example
                            device.RequeueBuffer(image);


                        }
                        catch (Exception ex) { }

                        ConexionBaseDeDatos.ObtenerDatos();
                        if (!IniciarCaptura)
                        { PrimerEncendidoAplicacion = false; }
                    }
                    device.StopStream();
                    system.DestroyDevice(device);
                    ArenaNET.Arena.CloseSystem(system);

                
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nException thrown: {0}", ex.Message);
            }

            Console.WriteLine("Press enter to complete");
            Console.Read();
       
        }

 
        //public static NDArray reducirImagen(IImage image)
        //{


        //    byte[] data = image.DataArray;

        //    // Crear un array de UInt16 para almacenar los datos convertidos
        //    UInt16[] pdata_as_uint16 = new UInt16[data.Length / 2]; // Cada elemento en el array es de 2 bytes (UInt16)

        //    // Copiar los bytes del array de bytes al array de UInt16 utilizando Marshal.Copy
        //    Buffer.BlockCopy(data, 0, pdata_as_uint16, 0, data.Length);

        //    // Ajustar el orden de bytes si es necesario (dependiendo del formato de los datos)
        //    if (BitConverter.IsLittleEndian)
        //    {
        //        // Si la arquitectura es little-endian, no es necesario realizar ningún ajuste
        //        // ya que BitConverter interpreta los datos en little-endian por defecto.
        //    }
        //    else
        //    {
        //        // Si la arquitectura es big-endian, es necesario invertir el orden de bytes
        //        for (int i = 0; i < pdata_as_uint16.Length; i++)
        //        {
        //            pdata_as_uint16[i] = (UInt16)((pdata_as_uint16[i] << 8) | (pdata_as_uint16[i] >> 8));
        //        }
        //    }
        //    int total_number_of_channels = pdata_as_uint16.Length / 3;

        //    // Iterar sobre los canales en grupos de 3
        //    for (int i = 0; i < total_number_of_channels; i++)
        //    {
        //        // Acceder a las coordenadas x, y, z
        //        int x = pdata_as_uint16[i * 3];
        //        int y = pdata_as_uint16[i * 3 + 1];
        //        int z = pdata_as_uint16[i * 3 + 2];
        //        if (z > 2000 && z < 3900 && x > 32100 && x < 33200 && y > 31900 && y < 32900)
        //        {
        //            // Almacenar las coordenadas que cumplen las condiciones
        //            int[] coordinates = new int[] { x, y, z };
        //            xyzList.Add(coordinates);
        //        }
        //    }

        //    NDArray xyz = np.vstack(xyzList.ToArray());
        //    //Console.WriteLine(xyz.shape);
        //    xyzList.Clear();


        //    return xyz;
        //}
        static void Visualizar(NDArray array)
        {
            try
            {
                // Verificar si el array tiene al menos 3 columnas
                var shape = array.shape;
                int rows = shape[0];
                int cols = shape[1];
                if (cols < 3)
                {
                    throw new ArgumentException("El array de entrada debe tener al menos 3 columnas");
                }

                // Crear un modelo de gráfico
                var modelo = new PlotModel { Title = "Nube de Puntos 3D" };

                // Crear una serie de puntos
                var scatterSeries = new ScatterSeries { MarkerType = MarkerType.Circle };
                for (int i = 0; i < rows; i++)
                {
                    double x = array[i, 0];
                    double y = array[i, 1];
                    double z = array[i, 2];
                    scatterSeries.Points.Add(new ScatterPoint(x, y, z));
                }
                // Agregar la serie al modelo
                modelo.Series.Add(scatterSeries);

                // Asignar el modelo al PlotView
                plotView.Invoke((MethodInvoker)delegate
                {
                    plotView.Model = modelo;
                    plotView.Invalidate();
                });
            }
            catch (Exception ex) { }

        }
        public static void CallPythonScript(List<(double, double, double)> array)
        {
            bool InstanciaPython;

            //string json = JsonConvert.SerializeObject(array);
            //string filePath = "C:\\Users\\Usuario\\Documents\\jhonnatan\\intecol\\Demo_ToF\\imagen.json";

            //// Guardar los datos JSON en un archivo
            //File.WriteAllText(filePath, json);

            // Ruta al script de Python
            string scriptPath = "C:\\Users\\Usuario\\Documents\\jhonnatan\\intecol\\Demo_ToF\\graficar_array.py";

            // Crear un proceso para ejecutar el script de Python
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"C:\Users\Usuario\.conda\envs\demovolumen\python.exe";
            start.Arguments = $"{scriptPath}"; // Pasar los datos JSON como argumento al script Python
            start.RedirectStandardOutput = true;
            start.UseShellExecute = false;

            // Iniciar el proceso
            using (Process process = new Process())
            {
                process.StartInfo = start;
                process.Start();

                // Leer y mostrar la salida estándar del proceso (si es necesario)
                string output = process.StandardOutput.ReadToEnd();
                //System.Console.WriteLine(output);

                // Convertir la salida en el valor retornado
                int resultado = int.Parse(output);

                // Utilizar el valor retornado en C#
                Console.WriteLine($"El resultado calculado en Python es: {resultado}");

                // Espera a que el proceso de Python termine
                process.WaitForExit(); //se puede comentar y poner un timer hahaha
            }

        }
        public static (IImage,List<double>, List<double>, List<double>) convertirHSV(IImage image, float scale, bool isHelios2)
        {

            ArenaNET.IImage newImage = null;
            List<double> xlist = new List<double>();
            List<double> ylist = new List<double>();
            List<double> zlist = new List<double>();
            try
            {
                // prepare info from input buffer
                UInt32 width = image.Width;
                UInt32 height = image.Height;
                UInt32 size = width * height;
                UInt32 srcBpp = image.BitsPerPixel;
                UInt32 srcPixelSize = srcBpp / 8; // divide by the number of bits in a byte
                byte[] input = image.DataArray;

                // prepare memory output buffer
                UInt32 dstBpp = ArenaNET.Arena.GetBitsPerPixel((UInt32)PIXEL_FORMAT);
                UInt32 dstPixelSize = dstBpp / 8; // divide by the number of bits in a byte
                UInt32 dstDataSize = width * height * dstBpp / 8; // divide by the number of bits in a byte
                byte[] output = new byte[dstDataSize];
                Array.Clear(output, 0, (Int32)dstDataSize);

                // Prepare coloring buffer for ply image
                //    Saving ply with color takes RGB coloring compared to the BGR
                //    coloring the jpg image uses, therefore we need a separate
                //    buffer for this data.

                byte[] color = new byte[dstDataSize];
                
                // manually convert to BGR image

                byte[] dataIn = input;
                byte[] dataOut = output;

                UInt32 indexIn = 0;
                UInt32 indexOut = 0;

                const double RGBmin = 0;
                const double RGBmax = 255;

                double redColorBorder;
                double yellowColorBorder;
                double greenColorBorder;
                double cyanColorBorder;
                double blueColorBorder;

                if (isHelios2)
                {
                    redColorBorder = 0;
                    yellowColorBorder = 2000;
                    greenColorBorder = 100;
                    cyanColorBorder = 10;
                    blueColorBorder = 1500;  //  = Scan3dOperatingMode  // finish - maximum distance
                }
                else
                {
                    redColorBorder = 0;
                    yellowColorBorder = 500;
                    greenColorBorder = 0;
                    cyanColorBorder = 0;
                    blueColorBorder = 10000;
                    //redColorBorder = 0;
                    //yellowColorBorder = 375;
                    //greenColorBorder = 750;
                    //cyanColorBorder = 1125;
                    //blueColorBorder = 1500;
                }

                // distance
                for (int i = 0; i < size; i++)
                {

                    double z = (UInt16)BitConverter.ToInt16(input, (int)indexIn + 4);
                    double x = (UInt16) BitConverter.ToInt16(input, (int)indexIn);
                    double y = (UInt16)BitConverter.ToInt16(input, (int)indexIn + 2);

                    double coordinateColorBlue = 0.0;
                    double coordinateColorGreen = 0.0;
                    double coordinateColorRed = 0.0;
                    if (z > zmin && z < zmax)
                    //if (z > 2500 && z < 3850)
                    { 
                      
                        xlist.Add(/*Math.Abs*/(x) * scale);
                        ylist.Add(y * scale);
                        zlist.Add(z * scale);
                        z = (double)(z * scale);
                    }
                    else
                    {
                       
                        xlist.Add(/*Math.Abs*/(x));
                        ylist.Add(y);
                        zlist.Add(z);
                        coordinateColorBlue = RGBmin;
                        coordinateColorGreen = RGBmin;
                        coordinateColorRed = RGBmin;
                        dataOut[indexOut] = (byte)coordinateColorBlue;
                        dataOut[indexOut + 1] = (byte)coordinateColorGreen;
                        dataOut[indexOut + 2] = (byte)coordinateColorRed;

                        // set RGB pixel coloring for ply
                        color[indexOut] = (byte)coordinateColorRed;
                        color[indexOut + 1] = (byte)coordinateColorGreen;
                        color[indexOut + 2] = (byte)coordinateColorBlue;

                        indexIn += srcPixelSize;
                        indexOut += dstPixelSize;

                        continue;
                    }
                    // colors between red and yellow
                    if ((z >= redColorBorder) && (z <= yellowColorBorder))
                    {
                        double yellowColorPercentage = z / yellowColorBorder;

                        coordinateColorBlue = RGBmin;
                        coordinateColorGreen = RGBmax * yellowColorPercentage;
                        coordinateColorRed = RGBmax;
                    }

                    // colors between yellow and green
                    else if ((z > yellowColorBorder) && (z <= greenColorBorder))
                    {
                        double greenColorPercentage = (z - yellowColorBorder) / yellowColorBorder;

                        coordinateColorBlue = RGBmin;
                        coordinateColorGreen = RGBmax;
                        coordinateColorRed = RGBmax - RGBmax * greenColorPercentage;
                    }

                    // colors between green and cyan
                    else if ((z > greenColorBorder) && (z <= cyanColorBorder))
                    {
                        double cyanColorPercentage = (z - greenColorBorder) / yellowColorBorder;

                        coordinateColorBlue = RGBmax * cyanColorPercentage;
                        coordinateColorGreen = RGBmax;
                        coordinateColorRed = RGBmin;
                    }

                    // colors between cyan and blue
                    else if ((z > cyanColorBorder) && (z <= blueColorBorder))
                    {
                        double blueColorPercentage = (z - cyanColorBorder) / yellowColorBorder;

                        coordinateColorBlue = RGBmax;
                        coordinateColorGreen = RGBmax - RGBmax * blueColorPercentage;
                        coordinateColorRed = RGBmin;
                    }

                    else
                    {
                        coordinateColorBlue = RGBmin;
                        coordinateColorGreen = RGBmin;
                        coordinateColorRed = RGBmin;
                    }


                    // set pixel format values and move to next pixel
                    dataOut[indexOut] = (byte)coordinateColorBlue;
                    dataOut[indexOut + 1] = (byte)coordinateColorGreen;
                    dataOut[indexOut + 2] = (byte)coordinateColorRed;

                    // set RGB pixel coloring for ply
                    color[indexOut] = (byte)coordinateColorRed;
                    color[indexOut + 1] = (byte)coordinateColorGreen;
                    color[indexOut + 2] = (byte)coordinateColorBlue;

                    indexIn += srcPixelSize;
                    indexOut += dstPixelSize;
                }



                newImage = ArenaNET.ImageFactory.Create(output, width, height, PIXEL_FORMAT);
                
            }
            catch { }
            return (newImage,xlist,ylist,zlist);
        }

    }
}
