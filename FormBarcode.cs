using System;
using System.Windows.Forms;

namespace USB_Barcode_Scanner_Tutorial___C_Sharp
{
    public partial class FormBarcode : Form
    {
        private readonly GlobalKeyboardHook globalKeyboardHook = new GlobalKeyboardHook();

        public FormBarcode()
        {
            InitializeComponent();
            // Suscribirse al evento BarcodeScanned
            globalKeyboardHook.BarcodeScanned += GlobalKeyboardHook_BarcodeScanned;

            // Asegurar que el formulario esté centrado inicialmente
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void GlobalKeyboardHook_BarcodeScanned(object sender, string barcode)
        {
            // Asegurarse de que el manejo ocurre en el hilo de UI
            if (InvokeRequired)
            {
                Invoke(new Action(() => GlobalKeyboardHook_BarcodeScanned(sender, barcode)));
                return;
            }

            // Mostrar el código de barras en el TextBox
            textBox1.Text = barcode;

            // Recuperar el foco y centrar el formulario en la pantalla
            this.CenterToScreen();
            this.Activate(); // Trae la ventana al frente
            this.Focus();    // Asegura que el formulario tenga el foco
        }    
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Liberar recursos al cerrar el formulario
            globalKeyboardHook.Dispose();
            base.OnFormClosing(e);
        }
    }
}
