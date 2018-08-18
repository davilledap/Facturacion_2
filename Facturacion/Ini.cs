using System;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Data;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Collections.Generic;


namespace Facturacion
{

    public class Ini
    {
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void Principal()
        {
            BD.Open();
            DataTable datos = BD.Datos_Tabla_Banderas();

            for (int i = 0; i < datos.Rows.Count; i++)
            {
                DataRow row = datos.Rows[i];
                int id = Convert.ToInt32(datos.Rows[i]["ID"]);
                int estado_timbrado = Convert.ToInt32(datos.Rows[i]["b_tim"]);
                int estado_correo = Convert.ToInt32(datos.Rows[i]["b_corr"]);

                if (estado_correo == 0)
                {
                    Factura(id);
                    //Timbrado(json,id);
                }
            }
                
        }

        private void Factura(int id)//Metodo encargado de la generacion del Json que porsteriormente sera timbrado y devuelto
        {
            string json = "";
            BD.Open();
            List<string> datos = BD.SeleccionDatos(id);
            // datos generales del CFDI
            dynamic cfdi = new JObject();
            cfdi.Serie = datos[1]; // dato opcional
            cfdi.Folio = Convert.ToInt32(datos[2]); // dato opcional. Tu deberás llevar el control del numeroC:\Users\dadan\Desktop\Timbado_listas2\Timbrado33\App.config de folio en tu app. Puedes utilizar no. de orden, transacción, etc.
            cfdi.Fecha = "AUTO"; // fecha del CFDI o utiliza cfdi.Fecha = 'AUTO' para poner la de nuestro servidor.
            //cfdi.Fecha = datos[3]; // fecha del CFDI o utiliza cfdi.Fecha = 'AUTO' para poner la de nuestro servidor.
            cfdi.FormaPago = datos[4]; // ver catálogo del SAT
            cfdi.CondicionesDePago = datos[5]; // dato opcional
            cfdi.SubTotal = datos[6];
            cfdi.Descuento = datos[7];
            cfdi.Moneda = datos[8]; // ver catálogo del SAT
            cfdi.TipoCambio = Convert.ToInt32(datos[9]); // si es MXN, deberás poner como valor 1. Si no, el tipo de cambio al día.
            cfdi.Total = datos[10]; // máximo 6 decimales
            cfdi.TipoDeComprobante = datos[11]; // ver catálogo del SAT para los tipos de comprobante. En este ejemplo, se refiere I = ingreso
            cfdi.MetodoPago = datos[12]; // PUE: Pago Una Exhibición - ver catálogo del SAT
            cfdi.LugarExpedicion = datos[13]; // código postal del domicilio de expedición
            cfdi.LeyendaFolio = datos[14];

            // datos del emisor
            dynamic emisor = new JObject();
            emisor.Rfc = datos[15];
            emisor.Nombre = datos[16];
            emisor.RegimenFiscal = datos[17];
            cfdi.Emisor = emisor;

            // datos del receptor (cliente)
            dynamic receptor = new JObject();
            receptor.Rfc = datos[18];
            receptor.Nombre = datos[19];
            // receptor.ResidenciaFiscal = datos[20]; // solo se usa cuando el receptor no esté dado de alta en el SAT o sea extranjero
            receptor.NumRegIdTrib = datos[21]; //  solo para extranjeros
            receptor.UsoCFDI = datos[22]; // uso que le dará el cliente al cfdi - ver catalogo del SAT
            receptor.Calle = datos[23];
            receptor.NoExt = datos[24];
            receptor.NoInt = datos[25];
            receptor.Colonia = datos[26];
            receptor.Localidad = datos[27];
            receptor.Referencia = datos[28];
            receptor.Municipio = datos[29];
            receptor.Estado = datos[30];
            receptor.Pais = datos[31];
            receptor.CodigoPostal = datos[32];
            cfdi.Receptor = receptor;

            // conceptos
            dynamic Conceptos = new JArray();

            // agrega concepto 1
            dynamic concepto = new JObject();
            concepto.ClaveProdServ = "01010101";//datos[33];
            concepto.NoIdentificacion = datos[34];
            concepto.Cantidad = Convert.ToInt32(datos[35]);
            concepto.ClaveUnidad = datos[36];
            concepto.Unidad = datos[37];
            concepto.Descripcion = datos[38];
            concepto.ValorUnitario = datos[39];
            concepto.Importe = datos[40];

            // agrega impuestos concepto 1
            dynamic ConceptoImpuestos = new JObject();
            dynamic ConceptoTraslados = new JArray();
            dynamic conceptotraslado = new JObject();
            conceptotraslado.Base = datos[41];
            conceptotraslado.Impuesto = datos[42];
            conceptotraslado.TipoFactor = datos[43];
            conceptotraslado.TasaOCuota = datos[44];
            conceptotraslado.Importe = datos[45];

            // agregamos el traslado al arreglo de traslados. Podemos agregar los que queramos, ya que es un arreglo.
            ConceptoTraslados.Add(conceptotraslado);

            ConceptoImpuestos.Traslados = ConceptoTraslados;

            // agregamos el arreglo de traslados al concepto
            concepto.Impuestos = ConceptoImpuestos;

            // agregamos el concepto al arreglo de conceptos. Igual, se pueden agregar los que queramos.
            Conceptos.Add(concepto);

            // agregamos el arreglo de conceptos al cfdi
            cfdi.Conceptos = Conceptos;

            // ahora, armamos la parte general de impuestos
            dynamic impuestos = new JObject();
            impuestos.TotalImpuestosTrasladados = datos[46];
            // impuestos.TotalImpuestosRetenidos = datos[47];

            dynamic Traslados = new JArray();
            dynamic traslado = new JObject();
            traslado.Impuesto = datos[42];
            traslado.TipoFactor = datos[43];
            traslado.TasaOCuota = datos[44];
            traslado.Importe = datos[45];
            Traslados.Add(traslado); // agregamos el traslado al array de traslados generales del cfdi

            // ya que agregamos todos los traslados y retenciones, ahora agregamos los arrays al cfdi
            impuestos.Traslados = Traslados;
            //impuestos.Retenciones = Retenciones;

            // finalmente, agregamos los impuestos al CFDI
            cfdi.Impuestos = impuestos;

            // generamos el string json
            json = cfdi.ToString();
            Timbrado(json, id);
        }


        private void Timbrado(string json, int id)
        {
            string uuid = null;
            string url = null;

            // generamos el cliente HTTP REST que hará la peticion
            var cliente = new RestClient("https://app.facturadigital.com.mx/api/cfdi");

            // definimos el metodo al que se hará la peticion y el tipo de peticion
            var peticion = new RestRequest("generar", Method.POST);

            // agregamos los datos del cfdi en formato json
            peticion.AddParameter("jsoncfdi", json); // adds to POST or URL querystring based on Method

            // agregamos los headers que contienen el usuario y contraseña de la API de FacturaDigital
            peticion.AddHeader("api-usuario", "demo33");
            peticion.AddHeader("api-password", "demo");

            var respuesta = cliente.Execute<RespuestaTimbrado>(peticion);

            if (respuesta.StatusCode != System.Net.HttpStatusCode.OK)
            {
                // timbrado erroneo
                //Timbrado33.Form1.txtRespuesta.Text = respuesta.Data.mensaje;
                Correo(uuid, url, id);
            }
            else
            {
                // timbrado exitoso
                //MessageBox.Show(respuesta.Data.mensaje);
                //Timbrado33.Form1.txtRespuesta.Text = respuesta.Data.mensaje;

                //txtXML.Text = DecodeFrom64(respuesta.Data.cfdi.XmlBase64);
                uuid = respuesta.Data.Cfdi.UUID;
                //txtFechaTimbrado.Text = respuesta.Data.cfdi.FechaTimbrado.ToString();
                url = respuesta.Data.Cfdi.PDF;
                Correo(uuid, url, id);
            }
        }

        public static void Correo(string uuid, string url, int id)//Metodo encargado del envio de correo electronico
        {
            BD.Open();
            List<string> datos = BD.SeleccionDatos(id);//Llamada al metodo que contiene los datos de la factura, seran añadidos a la estructrua html enviada por correo electronico

            MailMessage mail = new MailMessage
            {
                From = new MailAddress("danoo113@outlook.es")//Correo electronico del remitente
            };
            mail.To.Add(new MailAddress(datos[48]));//Correo electronico del destinatario

            //Asunto del correo electronico
            mail.Subject = "Factura";
            //Cuerpo del correo electronico, contiene el enlace para la descarga de la factura y una vista previa de la misma
            string body =
                            @"
        <!DOCTYPE html>
        <html>
        <head>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <style>
        * {
            box-sizing: border-box;
        }
        .menu {
            float: left;
            width: 20%;
        }
        .menuitem {
            padding: 8px;
            margin-top: 7px;
            border-bottom: 1px solid #f1f1f1;
        }
        .main {
            float: left;
            width: 60%;
            padding: 0 20px;
            overflow: hidden;
        }
        .right {
            background-color: lightblue;
            float: left;
            width: 20%;
            padding: 10px 15px;
            margin-top: 7px;
        }

        @media only screen and (max-width:800px) {
            /* For tablets: */
            .main {
            width: 80%;
            padding: 0;
            }
            .right {
            width: 100%;
            }
        }
        @media only screen and (max-width:500px) {
            /* For mobile phones: */
            .menu, .main, .right {
            width: 100%;
            }
        }

        h5 {text-align: center;}

        table tr {
            text-align: left;
	        }

        h3 {margin-left: 12%;}


        table {
            border-collapse: collapse;
  
 
        }

        th, td {
            padding: 8px;
            text-align: left;
            border-bottom: 2px solid #ffffff;
        }


        p.nueva{
            text-align: center;
            font-weight: bold;
            font-size:25px;
            color:#49F9BC;
            }

        </style>
        </head>
        <body style='font-family:Verdana;'>

        <div style='background-color:#C2FBE7;padding:0px;'>
            <center>
            <img src='http://www.cfenergia.com/img/logotipo.png' width='210' height='70'>
            </center>
        </div>

        <h5>Comercializamos Gas Natural, Gas natural licuado, combustibles líquidos y carbón dentro del territorio de México. </h4>  



            <p class='nueva'>
            <img src='https://portalacademico.cch.unam.mx/repositorio-de-sitios/matematicas/estadistica-probabilidad-1/Estadistica/img/manita.jpg' style='width:30px; height:30px'>  ¡Ha recibido una nueva factura.!  </p>


            <h3> Estimado (a).      " + datos[19] + @"     , ya puedes consultar tu nueva factura. 	</h3>
 	
            <h3> Resumen... </h3>
  
        <center>
            <table bgcolor='#D2F9FC'   width='75%'>
            <tr>
                <th rowspan='2'> Enviada a </th>
                <td> " + datos[19] + @" </ td >
            </tr>
            <tr>
                <td> " + datos[48] + @" </td>
            </tr>

            <tr>
                <th rowspan='2'> Enviada por</th>
                <td> " + datos[16] + @" </ td >
            </tr>
            <tr>
                <td>  " + datos[49] + @"</td>
            </tr>

            <tr>
                <th>Numero de factura</th>
                <td>" + datos[0] + @"</td>
            </tr>

            <tr>
                <th>Fecha de emision</th>
                <td>" + datos[3] + @"</td>
            </tr>

            <tr>
                <th>Importe</th>
                <td>$" + datos[40] + @"</td>
            </tr>
            </table> 
            </center> 

        <br>



            <center>
            <a href=' " + url + @"  ' style='font-size:16px;color:#ffffff;text-decoration:none;border-radius:2px;background-color:#ec5252;border-top:12px solid #ec5252;border-bottom:12px solid #ec5252;border-right:18px solid #ec5252;border-left:18px solid #ec5252;display:inline-block' target='_blank' data-saferedirecturl=''>
                        Ver Factura
            </a>
        </center> 

        <center>
            <h5>¡Gracias por utilizar nuestro servicio de facturacion electronica! </h5>     
        <center>

        <div style='background-color:#C2FBE7;padding:25px;'>
        </div>

        </body>
        </html>
            ";
            string html = uuid + url;

            AlternateView plainView = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Plain);

            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(body, Encoding.UTF8, MediaTypeNames.Text.Html);

            mail.AlternateViews.Add(plainView);
            mail.AlternateViews.Add(htmlView);

            SmtpClient client = new SmtpClient
            {
                Credentials = new System.Net.NetworkCredential("danoo113@outlook.es", "unodos12"),
                Port = 587,
                Host = "smtp-mail.outlook.com",
                EnableSsl = true //Esto es para que vaya a través de SSL que es obligatorio con GMail
            };
            try
            {
                if (uuid == null || url == null)//En caso de que el timbrado no se realizara correctamente no se enviara el correo y los estados se pondran en 0

                {
                    BD.Open();
                    BD.Actualizar(id, 0, 0);
                    Scheduler cc = new Scheduler();
                    cc.Start();
                    //MessageBox.Show("El timbrado no fue genearo");
                }
                else//Comprobacion de timbrado en caso de ser exitoso sera enviado al cliente y los estados de envio seran cambiados a 1
                {
                    int estado_correo = 1;
                    int estado_timbrado = 1;
                    BD.Open();
                    BD.Actualizar(id, estado_timbrado, estado_correo);//Llama al metodo Actualizar para cambiar el estado de envio y facturacion
                    //bd.actualizar escada (id, no_cliente,bandera_scada)
                    client.Send(mail);
                }
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        public class Cfdi
        {
            public string NoCertificado { get; set; }
            public string UUID { get; set; }
            public DateTime FechaTimbrado { get; set; }
            public string RfcProvCertif { get; set; }
            public string SelloCFD { get; set; }
            public string NoCertificadoSAT { get; set; }
            public string SelloSAT { get; set; }
            public string CadenaOrigTFD { get; set; }
            public string CadenaQR { get; set; }
            public string XmlBase64 { get; set; }
            public string PDF { get; set; }
        }
        public class RespuestaTimbrado
        {
            public string Mensaje { get; set; }
            public int Codigo { get; set; }
            public Cfdi Cfdi { get; set; }
        }
        public class RespuestaError
        {
            public string Mensaje { get; set; }
            public int Codigo { get; set; }
        }
        static public string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
            string returnValue = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }
    }
}
