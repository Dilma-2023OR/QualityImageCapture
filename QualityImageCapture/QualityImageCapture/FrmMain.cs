using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QualityImageCapture.Class;
using QualityImageCapture.RuncardServices;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Globalization;
using System.Runtime.InteropServices;
using AForge.Imaging.Filters;

namespace QualityImageCapture
{
    public partial class FrmMain : Form
    {
        private Button buttonPass;
        public FrmMain()
        {
            InitializeComponent();

        }
        
        //Config Connection
        INIFile localConfig = new INIFile(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\QualityImageCapture\config.ini");

        //Runcard Connection
        runcard_wsdlPortTypeClient servicio = new runcard_wsdlPortTypeClient("runcard_wsdlPort");

        unitStatus unitStatus;
        private FilterInfoCollection videoDevice; //Lista de Cámaras disponibles
        private VideoCaptureDevice videoSource; //Dispositivo de captura de video
        private Bitmap currentFrame;  //Imagen actual de la cámara

        string msg = string.Empty;
        int error = 0;

        //Config Data
        string warehouseBin = string.Empty;
        string warehouseLoc = string.Empty;
        string partClass = string.Empty;
        string machineId = string.Empty;
        string opcode = string.Empty;
        int seqnum = 0;
        
        string partnum = string.Empty;

        string link = string.Empty;
        string EWO = string.Empty;
        int errExpendor = 0;
        private void FrmMain_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(localConfig.FilePath)))
                {
                    //Config Directory
                    string direccion = Directory.GetCurrentDirectory() + "\\config.ini";
                    Directory.CreateDirectory(Path.GetDirectoryName(localConfig.FilePath));
                    File.Copy(direccion, localConfig.FilePath);
                }

                warehouseBin = localConfig.Read("RUNCARD_INFO", "warehouseBin");
                warehouseLoc = localConfig.Read("RUNCARD_INFO", "warehouseLoc");
                partClass = localConfig.Read("RUNCARD_INFO", "partClass");
                machineId = localConfig.Read("RUNCARD_INFO", "machineID");
                opcode = localConfig.Read("RUNCARD_INFO", "opcode");

                link = localConfig.Read("PATH", "imagesPath");
                //Control Adjust
                lblOpCode.Text = opcode;

            }
            catch (Exception ex) { }
        }

        private void InitializeCamera()
        {
            //Obtener los dispositivos de video (cámaras conectadas)
            videoDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            //Si hay cámaras disponibles, usar la primera
            if (videoDevice.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevice[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                videoSource.Start();
            }
        }

        private void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            //Almacenar el frame actual
            currentFrame = (Bitmap)eventArgs.Frame.Clone();

            //Solo ajustar tamaño la primera vez para evitar parpadeo constante

            this.Invoke(new Action(() =>
            {
                pbImagen.Image = currentFrame;

                if (pbImagen.Width != currentFrame.Width || pbImagen.Height != currentFrame.Height)
                {
                    pbImagen.Width = currentFrame.Width;
                    pbImagen.Height = currentFrame.Height;
                }
            }));
            //Mostrar el frame en un PictureBox (si lo tienes en el formulario)
            //pbImagen.Image = currentFrame;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning) { 
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            base.OnFormClosing(e);
        }

        private void pbCapture_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                string rutaPredeterminada = @"" + link + EWO + "\\" + opcode + "\\" + seqnum;
                string serial = tbNumSerie.Text + "_" + DateTime.Now.ToString("MMddyyyyHHmmss");
                string nombreArchivoFijo = serial + ".jpg";

                string rutaBase = Path.Combine(rutaPredeterminada, nombreArchivoFijo);
                string destinationFilePath = rutaBase;

                try
                {
                    if (!Directory.Exists(rutaPredeterminada))
                    {
                        Directory.CreateDirectory(rutaPredeterminada);
                    }

                    if (CanWriteToDirectory(Path.GetDirectoryName(destinationFilePath)))
                    {
                        currentFrame.Save(destinationFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                        ////detener la cámara web
                        //if (videoSource != null && videoSource.IsRunning)
                        //{
                        //    videoSource.SignalToStop();
                        //    videoSource.WaitForStop();
                        //}

                        Message message = new Message("Foto guardada con éxito");
                        message.ShowDialog();

                        pbImagen.Image = System.Drawing.Image.FromFile(destinationFilePath);

                        btnPass.Enabled = true;
                        //pass();
                    }
                    else
                    {
                        Message message = new Message("No tienes permisos para guardar en esta carpeta.");
                        message.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    Message message = new Message("Error al cargar la imagen");
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al cargar la imagen:" + ex.Message + "\n");

                }

            }

        }

        //Función para verificar si se puede escribir en el directorio
        private bool CanWriteToDirectory(string directoryPath)
        {
            try
            {
                //Verificar si el directorio existe y si es accesible
                if (Directory.Exists(directoryPath)) {
                    //Intentar crear un archivo temporal para verificar permisos de escritura
                    string temFile = Path.Combine(directoryPath, "tempFile.txt");
                    using (FileStream fs = File.Create(temFile)) { }
                    File.Delete(temFile); //Eliminar el archivo temporal después de la verificación
                    return true;
                }
                return false;
            }
            catch (Exception ex) {
                return false; //Si ocurre una excepción, no tiene permisos
            }
        }

        private void tbNumSerie_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter & tbNumSerie.Text != string.Empty)
            {
                try {
                    string scanInfo = string.Empty;
                    string serie = string.Empty;
                    string opcode1 = string.Empty;
                    errExpendor = 0;
                    foreach (var charScan in tbNumSerie.Text.ToUpper())
                    {
                        //Convert to Char
                        char c = Convert.ToChar(Convert.ToInt32(charScan));

                        if (!char.IsControl(c))
                            scanInfo = scanInfo + c;
                    }

                    if (scanInfo != "")
                    {
                        var fetchInv = servicio.fetchInventoryItems(scanInfo, "", "", "", "", "", 0, "", "", out error, out msg);

                        //Si no falla y existe
                        if (error == 0 & fetchInv.Length > 0)
                        {
                            string partStatus = fetchInv[0].status;
                            string partNum = fetchInv[0].partnum;
                            string partRev = fetchInv[0].partrev;
                            string serial = fetchInv[0].serial;
                            float quantity = fetchInv[0].qty;
                            EWO = fetchInv[0].workorder;
                            serie = fetchInv[0].serial;
                            opcode1 = fetchInv[0].opcode;
                            seqnum = fetchInv[0].seqnum;
                        }
                        else
                        {
                            //Log
                            File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + scanInfo + ", Serial no registrado en sistema.\n");

                            Message message = new Message(scanInfo + ", Serial no registrado en sistema.");
                            message.ShowDialog();
                            errExpendor = 1;
                        }
                    }
                       

                    if (errExpendor == 0)
                    {
                        if (opcode1 == opcode)
                        {
                            tbNumSerie.Enabled = false;
                            btnPass.Enabled = true;
                            pbImagen.Enabled = false;
                            tbNumSerie.Text = serie;
                            InitializeCamera();
                        }
                        else
                        {
                            Message message = new Message("Serial " + serie + " fuera de flujo");
                            message.ShowDialog();

                            tbNumSerie.Clear();
                            tbNumSerie.Focus();
                        }
                    }
                    else
                    {
                        btnPass.Enabled = false;
                        tbNumSerie.Clear();
                        tbNumSerie.Focus();
                    }
                }
                catch (Exception ex) {
                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al consultar el status del serial " + ex.Message + "\n");
                }
            }
        }

        public void pass() 
        {
            string scanInfo = "";

            foreach (char c in tbNumSerie.Text)
            {
                if (!char.IsControl(c))
                {
                    scanInfo = scanInfo + c;
                }
            }

            int response = 0;


            serialTransaction(tbNumSerie.Text, out response);

            if (response != 0)
            {
                //Control Adjust
                tbNumSerie.Clear();
                tbNumSerie.Focus();
                return;
            }

            //detener la cámara web
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }

            //Control Adjust
            tbNumSerie.Enabled = true;
            tbNumSerie.Clear();
            tbNumSerie.Focus();
            pbImagen.Image = null;
            btnPass.Enabled = false;
        }


        private void btnPass_Click(object sender, EventArgs e)
        {
            pass();
        }

        private void serialTransaction(string serial, out int response)
        {

            InventoryItem[] fetchInv = null;
            string workorder = string.Empty;
            string operation = string.Empty;
            string partnum = string.Empty;
            string partrev = string.Empty;
            string status = string.Empty;
            int step = 0;

            //response
            response = 0;
            try { 
                fetchInv = servicio.fetchInventoryItems(serial, "", "", "", "", "", 0, "", "", out error, out msg);
                workorder = fetchInv[0].workorder;
                operation = fetchInv[0].opcode;
                partnum = fetchInv[0].partnum;
                partrev = fetchInv[0].partrev;
                status = fetchInv[0].status;
                step = fetchInv[0].seqnum;
            }
            catch (Exception ex)
            {
                //Feedback
                Message message = new Message("Error al consultar el status del serial " + serial);
                message.ShowDialog();

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al consultar el status del serial " + serial + ":" + ex.Message + "\n");

                //Response
                response = -1;
                return;
            }

            if (status == "IN QUEUE" & operation == opcode | status == "IN PROGRESS" & operation == opcode)
            {
                //Transaction Item
                transactionItem transItem = new transactionItem();
                transItem.workorder = workorder;
                transItem.warehouseloc = warehouseLoc;
                transItem.warehousebin = warehouseBin;
                transItem.username = "ftest";
                transItem.machine_id = machineId;
                transItem.transaction = "MOVE";
                transItem.opcode = operation;
                transItem.serial = serial;
                transItem.trans_qty = 1;
                transItem.seqnum = step;
                transItem.comment = "TRANSACCION HECHA POR SISTEMA";

                dataItem[] inputData = new dataItem[] { };
                bomItem[] bomData = new bomItem[] { };
                try { 
                    //Transaction
                    var transaction = servicio.transactUnit(transItem, inputData, bomData, out msg);

                    //MessageBox.Show(msg);
                    if (!msg.Contains("ADVANCE"))
                    {
                        //Feedback
                        MostrarMensajeFlotanteNoPass("Pase no otorgado al serial " + serial);

                        //Log
                        File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Pase NO otorgado al serial " + serial + ":" + msg + "\n");

                        //Response
                        response = -1;

                        return;
                    }

                    //Feedback
                    MostrarMensajeFlotante("Serial " + serial + " Completado");

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\Log.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + "," + msg + "\n");
                }
                catch (Exception ex)
                {
                    //Feedback
                    Message message = new Message("Error al dar el pase al serial " + serial);
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al dar el pase al serial " + serial + ":" + ex.Message + "\n");
                    //Response
                    response = -1;
                    return;
                }
            }
            else
            {
                //Get Instructions
                var getInstructions = servicio.getWorkOrderStepInstructions(workorder, step.ToString(), out error, out msg);

                //Feedback

                MostrarMensajeFlotanteNoPass("Serial " + serial + " sin flujo, " + status + ":" + getInstructions.opdesc);

                //Response
                response = -1;
            }
        }

        private void MostrarMensajeFlotante(string mensaje)
        {
            // Crear un formulario emergente flotante
            Form flotanteForm = new Form();
            flotanteForm.FormBorderStyle = FormBorderStyle.None;  // Sin bordes
            flotanteForm.StartPosition = FormStartPosition.CenterScreen;  // Centrado en la pantalla
            flotanteForm.BackColor = Color.Green;  // Fondo verde (puedes cambiar el color)
            flotanteForm.Opacity = 0.9;  // Opacidad para hacerlo semitransparente
            flotanteForm.TopMost = true;  // Asegura que esté sobre otras ventanas
            flotanteForm.Width = 600;  // Ancho de la ventana flotante
            flotanteForm.Height = 200;  // Alto de la ventana flotante

            // Crear un label para mostrar el mensaje
            Label mensajeLabel = new Label();
            mensajeLabel.AutoSize = false;
            mensajeLabel.Size = new Size(flotanteForm.Width, flotanteForm.Height);
            mensajeLabel.Text = mensaje;
            mensajeLabel.Font = new Font("Arial", 48, FontStyle.Bold);  // Tamaño grande de la fuente
            mensajeLabel.ForeColor = Color.White;  // Color de texto blanco
            mensajeLabel.TextAlign = ContentAlignment.MiddleCenter;  // Centrado en el label

            // Añadir el label al formulario flotante
            flotanteForm.Controls.Add(mensajeLabel);

            // Mostrar el mensaje durante 3 segundos y luego cerrar
            flotanteForm.Show();
            Timer timer = new Timer();
            timer.Interval = 3000;  // 3000 milisegundos = 3 segundos
            timer.Tick += (sender, e) =>
            {
                flotanteForm.Close();
                timer.Stop();
            };
            timer.Start();
        }

        private void MostrarMensajeFlotanteNoPass(string mensaje)
        {
            // Crear un formulario emergente flotante
            Form flotanteForm = new Form();
            flotanteForm.FormBorderStyle = FormBorderStyle.None;  // Sin bordes
            flotanteForm.StartPosition = FormStartPosition.CenterScreen;  // Centrado en la pantalla
            flotanteForm.BackColor = Color.Red;  // Fondo verde (puedes cambiar el color)
            flotanteForm.Opacity = 0.9;  // Opacidad para hacerlo semitransparente
            flotanteForm.TopMost = true;  // Asegura que esté sobre otras ventanas
            flotanteForm.Width = 600;  // Ancho de la ventana flotante
            flotanteForm.Height = 200;  // Alto de la ventana flotante

            // Crear un label para mostrar el mensaje
            Label mensajeLabel = new Label();
            mensajeLabel.AutoSize = false;
            mensajeLabel.Size = new Size(flotanteForm.Width, flotanteForm.Height);
            mensajeLabel.Text = mensaje;
            mensajeLabel.Font = new Font("Arial", 48, FontStyle.Bold);  // Tamaño grande de la fuente
            mensajeLabel.ForeColor = Color.White;  // Color de texto blanco
            mensajeLabel.TextAlign = ContentAlignment.MiddleCenter;  // Centrado en el label

            // Añadir el label al formulario flotante
            flotanteForm.Controls.Add(mensajeLabel);

            // Mostrar el mensaje durante 3 segundos y luego cerrar
            flotanteForm.Show();
            Timer timer = new Timer();
            timer.Interval = 3000;  // 3000 milisegundos = 3 segundos
            timer.Tick += (sender, e) =>
            {
                flotanteForm.Close();
                timer.Stop();
            };
            timer.Start();
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}
