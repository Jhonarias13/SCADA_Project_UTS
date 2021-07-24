using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace proyecto_scada
{
    public partial class Form2 : Form
    {
        SerialPort PuertoSerie = new SerialPort();
        String dato;
        String datoIn;
        String dato_salida;
        string[] words;
        int nivel;
        int estado = 1;
        public Form2()
        {
            InitializeComponent();
            Desable_Panel();
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you want to quit the application?", "notice", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }
        public void Enable_Panel()
        {
            groupPanel3.Enabled = true;
            switchButton2.Enabled = true;
            switchButton2.Value = false;
            switchButton3.Enabled = true;
            switchButton3.Value = false;
            switchButton4.Enabled = true;
            switchButton4.Value = false;
        }
        public void Desable_Panel()
        {
            groupPanel3.Enabled = false;
            switchButton2.Enabled = false;
            switchButton2.Value = false;
            switchButton3.Enabled = false;
            switchButton3.Value = false;
            switchButton4.Enabled = false;
            switchButton4.Value = false;
        }
        public void Desable_Panel_Parcial()
        {
            switchButton2.Enabled = false;
            switchButton2.Value = false;
            switchButton3.Enabled = false;
            switchButton3.Value = false;
            switchButton4.Enabled = false;
            switchButton4.Value = false;
        }
        public void Enable_Panel_Parcial()
        {
            switchButton2.Enabled = true;
            switchButton2.Value = false;
            switchButton3.Enabled = true;
            switchButton3.Value = false;
            switchButton4.Enabled = true;
            switchButton4.Value = false;
        }

        private void Valv_Int_ch(object sender, EventArgs e)
        {
            if (switchButton2.Value)
            {
                textBoxX8.Text = "Open";
            }
            else
            {
                textBoxX8.Text = "Closed";
            }
        }

        private void Valv_out_ch(object sender, EventArgs e)
        {
            if (switchButton3.Value)
            {
                textBoxX9.Text = "Open";
            }
            else
            {
                textBoxX9.Text = "Closed";
            }
        }

        private void Pump_ch(object sender, EventArgs e)
        {
            if (switchButton4.Value)
            {
                textBoxX7.Text = "Run";
            }
            else
            {
                textBoxX7.Text = "Stopped";
            }
        }

        private void buttonX2_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text != "")
            {
                PuertoSerie.PortName = comboBox1.Text;
                PuertoSerie.BaudRate = 9600;
                PuertoSerie.Parity = Parity.None;
                PuertoSerie.DataBits = 8;
                PuertoSerie.StopBits = StopBits.One;
                try
                {
                    PuertoSerie.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening the serial port: " + ex.Message);
                }

            }
            else
            {
                MessageBox.Show("You must choose a port", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (PuertoSerie.IsOpen)
            {
                buttonX2.Enabled = false;
                buttonX3.Enabled = true;
                timer1.Enabled = true;
                PuertoSerie.DiscardInBuffer();
                groupPanel1.Enabled = true;
                radioButton1.Checked = true;
                Enable_Panel();
                MessageBox.Show("SCADA connected system ", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buscar_puertos(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            foreach (string nombres in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(nombres);
            }
        }

        private void buttonX3_Click(object sender, EventArgs e)
        {
            try
            {
                PuertoSerie.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error closing serial port: " + ex.Message);
            }
            if (!PuertoSerie.IsOpen)
            {
                timer1.Enabled = false;
                buttonX2.Enabled = true;
                buttonX3.Enabled = false;
                groupPanel1.Enabled = false;
                radioButton1.Checked = true;
                Desable_Panel();
                MessageBox.Show("SCADA system disconnected", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /*
// protocolo sistema SCADA 

     Valvula de entrada, valvula de salida, motobomba, NL & CR

     Valvula de entrada =   #on , #off
     valvula de salida  =   #on , #off
     motobomba          =   #on , #off

 */
        private void timer1_Tick_1(object sender, EventArgs e)
        {

            if (PuertoSerie.IsOpen)
            {
                if (radioButton1.Checked)  //modo manual
                {
                    dato = switchButton2.Value ? "#on," : "#off,";
                    dato += switchButton3.Value ? "#on," : "#off,";
                    dato += switchButton4.Value ? "#on," : "#off,";

                    enviar_datos();
                    leer_datos();
                    presentar_datos();


                }
                else   // modo automatico
                {
                    start_automatico();
                }
            }
        }
        private void start_automatico()
        {
            int High_level = 0;
            int Low_level = 0;
           
            
            if (switchButton1.Value)  //  switch star activado
            {
                High_level = Convert.ToInt16(textBoxX1.Text);
                Low_level  = Convert.ToInt16(textBoxX2.Text);

                if ((High_level > Low_level) && ((High_level - Low_level) > 10)) //aqui el codigo para trabajar de modo automatico
                {
                    if ((estado == 1)&&(nivel>Low_level))
                    {
                        dato_salida = "#off,#on,#off,"; // vaciado
                        estado = 3;
                    }
                    else if ((estado == 1) && (nivel < Low_level))
                    {
                        dato_salida = "#on,#off,#on,";  // llenado
                        estado = 4;
                    }
                    else if ((estado == 3) && (nivel < Low_level))
                    {
                        dato_salida = "#on,#off,#on,";  //  llenado
                        estado = 4;
                    }
                    else if ((estado == 4) && (nivel > High_level))
                    {
                        dato_salida = "#off,#on,#off,";  // vaciado
                        estado = 3;
                    }
                    PuertoSerie.WriteLine(dato_salida);
                    leer_datos();
                    presentar_datos();
                }
                else
                {
                    switchButton1.Value = false;
                    MessageBox.Show("The value of the high level must be higher than the low level by at least 10 units.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                }
            }
            else
            {
                PuertoSerie.WriteLine("#off,#off,#off,");
                leer_datos();
                presentar_datos();
                estado = 1;
            }

        }
        private void presentar_datos()
        {
            int index = 0;

            words = datoIn.Split(',');
            foreach (string referencia in words)
            {
                index++;
            }
            if (index == 5)  // si llegan los 4 datos presentelos
            {
                textBoxX3.Text = words[0] + " cm";
                nivel = Convert.ToInt16(words[0]);
                textBoxX11.Text = words[0] + " cm";
                textBoxX4.Text = words[1] + " ºC";
                textBoxX10.Text = words[1] + " ºC";
                textBoxX5.Text = words[2] + " psi";
                textBoxX6.Text = words[3] + " lt/min";

                try
                {
                    gaugeControl3.SetPointerValue("Pointer1", Convert.ToInt16(words[3]));

                }
                catch (FormatException)
                {

                }
                try
                {
                    gaugeControl1.SetPointerValue("Pointer1", Convert.ToInt16(words[0]));

                }
                catch (FormatException)
                {

                }
                try
                {
                    gaugeControl2.SetPointerValue("Pointer1", Convert.ToInt16(words[2]));

                }
                catch (FormatException)
                {

                }
            }
        }
        private void enviar_datos()
        {
            PuertoSerie.DiscardInBuffer();
            PuertoSerie.WriteLine(dato);
        }
        private void vaciado()
        {
            PuertoSerie.WriteLine("#off,#on,#off,");
        }
        private void llenado()
        {
            PuertoSerie.WriteLine("#on,#off,#on,");
        }
        private void leer_datos()
        {
            try
            {
                datoIn = PuertoSerie.ReadLine();
            }
            catch (TimeoutException ex)
            {


            }
            textBox1.Text = datoIn;
        }
        private void Manual_auto(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                groupPanel2.Enabled = false;
                switchButton1.Value = false;
                Enable_Panel();
            }
            else
            {
                groupPanel2.Enabled = true;
                Desable_Panel_Parcial();
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }
    }
}
