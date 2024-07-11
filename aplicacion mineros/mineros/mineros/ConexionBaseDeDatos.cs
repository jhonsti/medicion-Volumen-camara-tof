using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Collections;
using System.Configuration;

namespace mineros
{
    public class ConexionBaseDeDatos
    {
        //Conexion con la base de datos.
        //==================================================================================================================
        static string connectionString = "Server=DAVID\\SQLEXPRESS; Database=Mineros; Trusted_Connection=True; MultipleActiveResultSets=True; TrustServerCertificate=True"; // Obtén la cadena de conexión de tu archivo de configuración
        public static void ObtenerDatos() {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Conexión exitosa a la base de datos.");
                    // Aquí puedes ejecutar consultas SQL u otras operaciones en la base de datos
                    string query = "SELECT * FROM ControlCamara";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Leer los datos de la tabla controCamara
                            while (reader.Read())
                            {
                                // Acceder a las columnas por su nombre o índice
                                int id = reader.GetInt32(reader.GetOrdinal("id"));
                                camara.zmin = reader.GetInt32(reader.GetOrdinal("zmin"));
                                camara.zmax = reader.GetInt32(reader.GetOrdinal("zmax"));
                                camara.IniciarCaptura = reader.GetBoolean(reader.GetOrdinal("estado"));
                                camara.ActualizarDatosDereferencia = reader.GetBoolean(reader.GetOrdinal("actualizarImagenReferencia"));
                                Console.WriteLine($"ID: {id}, Zmin: {camara.zmin}, Zmax: {camara.zmax}, Estado: {camara.IniciarCaptura}");
                            }
                        }
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al conectar a la base de datos: " + ex.Message);
                }

            }  //// =================================================================================================================

        }

        public static void ActualizarDatosCamara()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    Console.WriteLine("Conexión exitosa a la base de datos.");

                    string updateQuery = "UPDATE ControlCamara " +
                                         "SET estado = @NuevoEstado";

                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@NuevoActualizarImagenReferencia", camara.ActualizarDatosDereferencia);

                        int rowsAffected = updateCommand.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Console.WriteLine("Registros modificados correctamente en la base de datos.");
                        }
                        else
                        {
                            Console.WriteLine("No se encontraron registros para modificar en la base de datos.");
                        }
                    }

                    connection.Close();
                    Console.WriteLine("Conexión terminada con la base de datos.");

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al conectar a la base de datos: " + ex.Message);
                }

            }  //// =================================================================================================================

        }

        public static void Medicion(double volumen)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                DateTime fechaActual = DateTime.Now;
                
                try
                {
                    connection.Open();
                    
                    Console.WriteLine("Conexión exitosa a la base de datos.");

                    string insertQuery = "INSERT INTO Mediciones (volumen, date, turno) " +
                                         "VALUES (@volumen, @date, @turno)";

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        string a = "tarde";
                        insertCommand.Parameters.AddWithValue("@volumen", (int)volumen);
                        insertCommand.Parameters.AddWithValue("@date", fechaActual);
                        insertCommand.Parameters.AddWithValue("@turno", a);

                        int rowsAffected = insertCommand.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Console.WriteLine("Registros modificados correctamente en la base de datos.");
                        }
                        else
                        {
                            Console.WriteLine("No se encontraron registros para modificar en la base de datos.");
                        }
                    }
                    Console.WriteLine("Entro a la escritura");
                    connection.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al conectar a la base de datos: " + ex.Message);
                }

            }  //// =================================================================================================================

        }

    }
}
