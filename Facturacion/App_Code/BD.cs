using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;


public class BD
{
    public static MySqlConnection dbConn = new MySqlConnection();
    public static MySqlCommand cmd = new MySqlCommand();

    public static void Open()//Medoto encargado de establecer la conexion con la base de datos.
    {
        dbConn.Close();
        dbConn.ConnectionString = "server=localhost;database=sistematrasvase_bd;uid=root;password=";
    }

    public static List<string> SeleccionDatos(int id)// Metodo encargado de extraer los datos de la base de datos y almacenarlo en una lista.
    {
        List<string> datos = null;
        dbConn.Open();
        cmd.CommandType = System.Data.CommandType.Text;
        cmd.CommandText = "SELECT * FROM facturas_tb INNER JOIN cfdi_tb ON facturas_tb.CLAVE_CFDI = cfdi_tb.CLAVE INNER JOIN datos_emisor_tb ON facturas_tb.RFC_EMI = datos_emisor_tb.RFC_EMI INNER JOIN clientes_tb ON facturas_tb.RFC_CLI = clientes_tb.RFC_CLI INNER JOIN  productos_tb ON facturas_tb.CLAVEPRODSER = productos_tb.CLAVEPRODSER INNER JOIN impuesto_tb ON facturas_tb.NO_FACTURA = impuesto_tb.NO_IMPUESTO where NO_FACTURA =1";
        cmd.Connection = dbConn;
        MySqlDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            datos = new List<string>//Lista contenedora de datos. Tabla cfdi_tb, clientes_tb, datos_emisor_tb, facturas_tb, impuesto_tb, productos_tb
            {
                //ID
                reader["NO_FACTURA"].ToString(),// posicion 0
                //Dataos de CFDI 
                reader["serie"].ToString(),// posicion 1
                reader["folio"].ToString(),// posicion 2
                reader["fh_emision"].ToString(),// posicion 3
                reader["formapago"].ToString(), // posicion 4
                reader["condiciones_pago"].ToString(),// posicion 5
                reader["subtotal"].ToString(),// posicion 6
                reader["descuento"].ToString(),// posicion 7
                reader["moneda"].ToString(),// posicion 8
                reader["tipo_cambio"].ToString(),// posicion 9
                reader["total"].ToString(),// posicion 10
                reader["tipo_comprobante"].ToString(),// posicion 11
                reader["metodo_pago"].ToString(),// posicion 12
                reader["lugar_exp"].ToString(),// posicion 13
                reader["leyenda_folio"].ToString(),// posicion 14
                //Datos emisor 
                reader["RFC_EMI"].ToString(),// pociocion 15
                reader["nombre_e"].ToString(),// posicion 16
                reader["regimen_fiscal_e"].ToString(),// posicion 17
                //Datos receptor o cliente
                reader["RFC_CLI"].ToString(),// posicion 18
                reader["nombre"].ToString(),// posicion 19
                reader["residencia_fiscal"].ToString(),// posicion 20
                reader["NumRegIdTrib"].ToString(),// posicion 21
                reader["uso_cfdi"].ToString(),// posicion 22
                reader["calle"].ToString(),// posicion 23
                reader["num_ext"].ToString(),// posicion 24
                reader["num_int"].ToString(),// posicion 25
                reader["colonia"].ToString(),// posicion 26
                reader["localidad"].ToString(),// posicion 27
                reader["referencia"].ToString(),// posicion 28
                reader["municipio"].ToString(),// posicion 29
                reader["estado"].ToString(),// posicion 30
                reader["pais"].ToString(),// posicion 31
                reader["codigo_postal"].ToString(), //posicion 32
                //Concepto o producto 
                reader["CLAVEPRODSER"].ToString(),// posicion 33
                reader["no_identificacion"].ToString(),// posicion 34
                reader["cantidad"].ToString(),// posicion 35
                reader["clave_unidad"].ToString(),// posicion 36
                reader["unidad"].ToString(),// posicion 37
                reader["descripcion"].ToString(),// posicion 38
                reader["valor_unitario"].ToString(),// posicion 39
                reader["importe"].ToString(),// posicion 40
                //Impuestos
                reader["base"].ToString(),// posicion 41
                reader["impuesto"].ToString(),// posicion 42
                reader["tipo_factor"].ToString(),// posicion 43
                reader["tasa_cuota"].ToString(),// posicion 44
                reader["importe_i"].ToString(),// posicion 45

                reader["total_imp_tras"].ToString(),// posicion 46
                reader["total_imp_ret"].ToString(),// posicion 47

                reader["email"].ToString(),// posicion 48
                reader["email_e"].ToString()// posicion 49
            };
        }
        else
        {
        }

        dbConn.Close();
        return datos;
    }

    public static void Actualizar(int id, int b_tim, int b_corr)//Metodo encargado de la actualizacion de los estados de timbrado y envio de correo
    {
        dbConn.Open();
        cmd.CommandType = System.Data.CommandType.Text;
        cmd.CommandText = "UPDATE `sistematrasvase_bd`.`facturas_tb` SET  `e_timbrado`='" + b_tim + "', `e_envio`='" + b_corr + "' WHERE  `NO_FACTURA`=" + id;
        cmd.Connection = dbConn;
        cmd.ExecuteNonQuery();
        dbConn.Close();
    }

    public static DataTable Datos_Tabla_Banderas()//Metodo encargado de comprobar estado de facturacion y envio de correos en los registros de la BD
    {
        dbConn.Open();
        cmd.CommandType = System.Data.CommandType.Text;
        cmd.CommandText = "select * from facturas_tb  where `e_envio`=0";
        cmd.Connection = dbConn;
        DataTable datos = new DataTable();//Tabla contenedora de los id de los registros no facturados o timprados
        datos.Columns.Add("ID", typeof(string));
        datos.Columns.Add("b_tim", typeof(string));
        datos.Columns.Add("b_corr", typeof(string));

        MySqlDataReader reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            DataRow fila = datos.NewRow();
            fila["ID"] = reader["NO_FACTURA"].ToString();
            fila["b_tim"] = reader["e_timbrado"].ToString();
            fila["b_corr"] = reader["e_envio"].ToString();
            datos.Rows.Add(fila);
        }
        dbConn.Close();
        return datos;
    }
}
