using System;
using System.IO.Ports;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Gasolinera
{
    public partial class Form1 : Form
    {
        private SerialPort puertoSerial; // Puerto serial para comunicación con Arduino
        private double precioPorLitro; // Precio por litro de combustible
        private string tipoLlenado; // Tipo de llenado (Prepago o Tanque lleno)
        private string bombaSeleccionada; // Bomba seleccionada
        private bool despachoEnProgreso = false; // Indica si el despacho está en progreso
        private int idDespacho = 0; // Contador de ID para los despachos

        // Variables para los nuevos requisitos
        private int totalDespachos = 0;
        private double totalQuetzales = 0;
        private int conteoPrepago = 0;
        private int conteoTanqueLleno = 0;
        private Dictionary<string, int> conteoBombas = new Dictionary<string, int>
        {
            { "Bomba 1", 0 },
            { "Bomba 2", 0 },
            { "Bomba 3", 0 },
            { "Bomba 4", 0 }
        };

        public Form1()
        {
            InitializeComponent();
            puertoSerial = new SerialPort("COM5", 9600); // Inicializar el puerto serial con la configuración adecuada
            puertoSerial.DataReceived += ManejarDatosRecibidos; // Asignar el manejador de datos recibidos
            puertoSerial.Open(); // Abrir el puerto serial
            EnviarValorInicial(); // Enviar un valor inicial a Arduino
            InicializarInterfaz(); // Inicializar la interfaz de usuario
            timer1.Tick += new EventHandler(Timer1_Tick); // Asignar el manejador del timer
            timer1.Start(); // Iniciar el timer
        }

        private void InicializarInterfaz()
        {
            ReiniciarDisplayBombas(); // Reiniciar los displays de las bombas
            ActualizarDataGridView2(); // Actualizar los datos en dataGridView2
        }

        private void EnviarValorInicial()
        {
            EnviarComandoArduino("flujo", 0, "Bomba 1"); // Enviar comando inicial de flujo 0 a la bomba 1
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (despachoEnProgreso)
            {
                MessageBox.Show("Un despacho ya está en progreso. Por favor, espera a que termine.");
                return;
            }

            // Actualizar el precio por litro cada vez que se presiona el botón
            if (!double.TryParse(txtPrecio.Text, out precioPorLitro))
            {
                MessageBox.Show("Por favor, introduce un valor numérico válido para el precio del día.");
                return;
            }

            tipoLlenado = CBXtipollenado.SelectedItem.ToString(); // Obtener el tipo de llenado seleccionado
            bombaSeleccionada = CBXbomba.SelectedItem.ToString(); // Obtener la bomba seleccionada

            if (tipoLlenado == "Tanque lleno")
            {
                EnviarComandoArduino("tanque_lleno", 0, bombaSeleccionada); // Enviar comando de tanque lleno a Arduino
                MessageBox.Show("Llenando el tanque..."); // Mostrar mensaje
                despachoEnProgreso = true;
            }
            else if (ValidarEntradas(out double cantidad))
            {
                // Si el tipo de llenado es Prepago
                double litros = cantidad / precioPorLitro; // Calcular los litros a despachar
                EnviarComandoArduino("calibracion", litros, bombaSeleccionada); // Enviar comando de calibración a Arduino
                MessageBox.Show("Listo para despachar"); // Mostrar mensaje
                despachoEnProgreso = true;
            }
            else
            {
                MessageBox.Show("Por favor, introduce valores numéricos válidos para el precio y la cantidad."); // Mostrar mensaje de error
            }
        }

        private bool ValidarEntradas(out double cantidad)
        {
            // Validar que las entradas sean números válidos
            cantidad = 0;
            return double.TryParse(txtPrecio.Text, out precioPorLitro) &&
                   (tipoLlenado == "Tanque lleno" || double.TryParse(txtcantidad.Text, out cantidad)) &&
                   cantidad >= 0;
        }

        private void EnviarComandoArduino(string accion, double valor, string bomba)
        {
            // Crear un comando JSON y enviarlo a Arduino
            var comando = new { action = accion, value = valor, bomba = bomba };
            string json = JsonConvert.SerializeObject(comando);
            puertoSerial.WriteLine(json);
        }

        private void ReiniciarDisplayBombas()
        {
            // Reiniciar todos los displays de las bombas
            Llitro.Text = "L 0.00";
            Lquetzal.Text = "Q 0.00";
            Llitro2.Text = "L 0.00";
            Lquetzal2.Text = "Q 0.00";
            Llitro3.Text = "L 0.00";
            Lquetzal3.Text = "Q 0.00";
            Llitro4.Text = "L 0.00";
            Lquetzal4.Text = "Q 0.00";
        }

        private void ReiniciarDisplayBomba(string bomba)
        {
            // Reiniciar el display de la bomba seleccionada
            if (bomba == "Bomba 1")
            {
                Llitro.Text = "L 0.00";
                Lquetzal.Text = "Q 0.00";
            }
            else if (bomba == "Bomba 2")
            {
                Llitro2.Text = "L 0.00";
                Lquetzal2.Text = "Q 0.00";
            }
            else if (bomba == "Bomba 3")
            {
                Llitro3.Text = "L 0.00";
                Lquetzal3.Text = "Q 0.00";
            }
            else if (bomba == "Bomba 4")
            {
                Llitro4.Text = "L 0.00";
                Lquetzal4.Text = "Q 0.00";
            }
        }

        private void ManejarDatosRecibidos(object sender, SerialDataReceivedEventArgs e)
        {
            // Manejar los datos recibidos desde Arduino
            try
            {
                string inData = puertoSerial.ReadLine().Trim();
                if (EsJsonValido(inData))
                {
                    dynamic json = JsonConvert.DeserializeObject(inData);
                    double flujo = json.flujo;
                    bool limiteAlcanzado = json.limiteAlcanzado;
                    string bomba = json.bomba;

                    double valorEnQuetzales = flujo * precioPorLitro; // Calcular valor en quetzales

                    // Actualizar la interfaz de usuario desde el hilo principal
                    this.Invoke(new MethodInvoker(delegate
                    {
                        ActualizarDisplayBomba(bomba, flujo, valorEnQuetzales);
                        if (limiteAlcanzado && despachoEnProgreso)
                        {
                            MessageBox.Show($"Despacho completado en {bomba}");
                            despachoEnProgreso = false; // Resetear el estado del despacho
                            GuardarDespachoCompleto(flujo, valorEnQuetzales); // Guardar los datos del despacho completo
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }));
            }
        }

        private void ActualizarDisplayBomba(string bomba, double flujo, double valorEnQuetzales)
        {
            // Actualizar el display de la bomba seleccionada
            if (bomba == "Bomba 1")
            {
                Llitro.Text = $"L {flujo:F2}";
                Lquetzal.Text = $"Q {valorEnQuetzales:F2}";
            }
            else if (bomba == "Bomba 2")
            {
                Llitro2.Text = $"L {flujo:F2}";
                Lquetzal2.Text = $"Q {valorEnQuetzales:F2}";
            }
            else if (bomba == "Bomba 3")
            {
                Llitro3.Text = $"L {flujo:F2}";
                Lquetzal3.Text = $"Q {valorEnQuetzales:F2}";
            }
            else if (bomba == "Bomba 4")
            {
                Llitro4.Text = $"L {flujo:F2}";
                Lquetzal4.Text = $"Q {valorEnQuetzales:F2}";
            }
        }

        private bool EsJsonValido(string strInput)
        {
            // Verificar si la entrada es un JSON válido
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || (strInput.StartsWith("[") && strInput.EndsWith("]")))
            {
                try
                {
                    JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException) { return false; }
            }
            return false;
        }

        private void GuardarDatos()
        {
            // Obtener los datos de los controles
            string nombreCliente = txtname.Text;
            string precioDia = txtPrecio.Text;
            string pago = txtcantidad.Text;
            DateTime fechaHoraDespacho = DateTime.Now;
            idDespacho++;

            // Datos del despacho
            string datos = $"ID: {idDespacho}, Nombre: {nombreCliente}, Tipo de Despacho: {tipoLlenado}, Precio del Día: {precioDia}, Bomba Seleccionada: {bombaSeleccionada}, Pago: {pago}, Fecha: {fechaHoraDespacho.ToShortDateString()}, Hora: {fechaHoraDespacho.ToLongTimeString()}\n";

            // Guardar los datos en un archivo txt
            File.AppendAllText("datos.txt", datos);
        }

        private void GuardarDespachoCompleto(double litrosDespachados, double totalAPagar)
        {
            // Obtener los datos de los controles
            string nombreCliente = txtname.Text;
            string precioDia = txtPrecio.Text;
            string pago = tipoLlenado == "Tanque lleno" ? totalAPagar.ToString("F2") : txtcantidad.Text;
            DateTime fechaHoraDespacho = DateTime.Now;
            idDespacho++;

            // Datos del despacho completo
            string datos = $"ID: {idDespacho}, Nombre: {nombreCliente}, Tipo de Despacho: {tipoLlenado}, Precio del Día: {precioDia}, Bomba Seleccionada: {bombaSeleccionada}, Litros: {litrosDespachados:F2}, Pago: {pago}, Fecha: {fechaHoraDespacho.ToShortDateString()}, Hora: {fechaHoraDespacho.ToLongTimeString()}\n";

            // Guardar los datos en un archivo txt
            File.AppendAllText("datos.txt", datos);

            // Actualizar los contadores
            totalDespachos++;
            totalQuetzales += totalAPagar;
            if (tipoLlenado == "Prepago")
            {
                conteoPrepago++;
            }
            else if (tipoLlenado == "Tanque lleno")
            {
                conteoTanqueLleno++;
            }

            // Incrementar el conteo de la bomba utilizada
            if (conteoBombas.ContainsKey(bombaSeleccionada))
            {
                conteoBombas[bombaSeleccionada]++;
            }

            // Imprimir los datos en el DataGridView
            dataGridView1.Rows.Add(idDespacho, nombreCliente, bombaSeleccionada, tipoLlenado, litrosDespachados, pago, precioDia, fechaHoraDespacho.ToShortDateString(), fechaHoraDespacho.ToLongTimeString());

            // Actualizar el DataGridView2
            ActualizarDataGridView2();
        }

        private void ActualizarDataGridView2()
        {
            // Calcular la bomba más utilizada y la menos utilizada
            var bombaMasUtilizada = conteoBombas.OrderByDescending(b => b.Value).FirstOrDefault().Key;
            var bombaMenosUtilizada = conteoBombas.OrderBy(b => b.Value).FirstOrDefault().Key;

            // Limpiar el DataGridView2
            dataGridView2.Rows.Clear();

            // Agregar los datos al DataGridView2
            dataGridView2.Rows.Add(totalDespachos, totalQuetzales, conteoPrepago, conteoTanqueLleno, bombaMasUtilizada, bombaMenosUtilizada);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            // Actualizar la fecha y hora cada tick
            labelFecha.Text = DateTime.Now.ToShortDateString();
            labelHora.Text = DateTime.Now.ToLongTimeString();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void button2_Click(object sender, EventArgs e) { }
    }
}
