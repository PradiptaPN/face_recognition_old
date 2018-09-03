using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Serialization;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Volume;
using Newtonsoft.Json;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Configurations;
using System.Data.SqlClient;
using System.Data;
using SQLite;
using Newtonsoft.Json;
using CsvHelper;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;

namespace IdentifikasiEkspresiWajah
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Deklarasi Variabel
        // Deklarasi variabel kamera
        private CameraProcess camera1_process = null; // process recognition
        private CameraProcess camera1_display = null;
        private CameraProcess camera2_process = null;
        private CameraProcess camera2_display = null;
        private CameraProcess camera3_process = null;
        private CameraProcess camera3_display = null;
        private CameraProcess camera4_process = null;
        private CameraProcess camera4_display = null;
        System.Drawing.Point fpsloc = new System.Drawing.Point(500, 500);

        int timer1_proc;
        int timer2_proc;
        int timer3_proc;
        int timer4_proc;

        Image<Gray, byte> result;

        // Deklarasi variabel classifier dan network
        new CascadeClassifier cascade;
        private Net<double> fernet;
        private Net<double> frnet;

        // Deklarasi variabel label recognition
        string emotionstring = null;
        string[] emotion_labels = { "netral", "lelah", "bingung", "terkejut", "senang", "jijik", "frustasi", "bosan" };
        string[] fr_labels = { "Pradipta", "Faisal", "Jonatan"};
        public Func<double, string> Formatter { get; set; }


        // Deklarasi variabel data pengolahan ekspresi dan pengenalan wajah
        double[,] val_net = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi netral
        double[,] val_lel = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi senang
        double[,] val_bin = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi bingung
        double[,] val_jut = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi lelah
        double[,] val_sen = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi frustasi
        double[,] val_jik = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi terkejut
        double[,] val_fru = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi jijik
        double[,] val_bos = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi bosan
        double[,] val_exc = new double[5000, 9]; // array untuk menampung data yang akan masukkan ke excel
        double[] net_output = new double[8]; // array untuk menampung hasil dari network emosi


        public MainWindow()
        {
            InitializeComponent();

            //Load Haar Cascade
            cascade = new CascadeClassifier(@"..\..\haarcascade_frontalface_default.xml");

            //Load network
            var ferjson = File.ReadAllText(@"..\..\..\Network\fernetwork.json");
            fernet = SerializationExtensions.FromJson<double>(ferjson);

            var frjson = File.ReadAllText(@"..\..\..\Network\frnetwork.json");
            frnet = SerializationExtensions.FromJson<double>(frjson);

            //Load list siswa
            var readsiswajson = System.IO.File.ReadAllText(@"..\..\..\Network\listsiswa.json");
            ListSiswa = JsonConvert.DeserializeObject<System.Collections.Generic.List<string>>(readsiswajson);
            FaceBox.ItemsSource = ListSiswa;

            // inisialisasi array
            for (int j = 0; j < 101; j++)
            {
                for (int k = 0; k < 5000; k++)
                {
                    val_net[j, k] = -1;
                    val_sen[j, k] = -1;
                    val_bin[j, k] = -1;
                    val_lel[j, k] = -1;
                    val_fru[j, k] = -1;
                    val_jut[j, k] = -1;
                    val_jik[j, k] = -1;
                    val_bos[j, k] = -1;
                }
            }

            cv_net1 = new ChartValues<double> { };
            cv_sen1 = new ChartValues<double> { };
            cv_bin1 = new ChartValues<double> { };
            cv_lel1 = new ChartValues<double> { };
            cv_fru1 = new ChartValues<double> { };
            cv_jut1 = new ChartValues<double> { };
            cv_jik1 = new ChartValues<double> { };
            cv_bos1 = new ChartValues<double> { };

            cv_net2 = new ChartValues<double> { };
            cv_sen2 = new ChartValues<double> { };
            cv_bin2 = new ChartValues<double> { };
            cv_lel2 = new ChartValues<double> { };
            cv_fru2 = new ChartValues<double> { };
            cv_jut2 = new ChartValues<double> { };
            cv_jik2 = new ChartValues<double> { };
            cv_bos2 = new ChartValues<double> { };

            cv_net3 = new ChartValues<double> { };
            cv_sen3 = new ChartValues<double> { };
            cv_bin3 = new ChartValues<double> { };
            cv_lel3 = new ChartValues<double> { };
            cv_fru3 = new ChartValues<double> { };
            cv_jut3 = new ChartValues<double> { };
            cv_jik3 = new ChartValues<double> { };
            cv_bos3 = new ChartValues<double> { };


            DataContext = this;

            //Every 1 minutes
            var dayConfig = Mappers.Xy<DateModel>()
              .X(dateModel => dateModel.DateTime.Ticks / TimeSpan.FromMinutes(1).Ticks);
            //and the formatter
            Formatter = value => new DateTime((long)(value * TimeSpan.FromMinutes(1).Ticks)).ToString("t");

        }

        // untuk mengatur camera 1
        private void Camera1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (camera1_process == null)
                {
                    Camera1.Visibility = Visibility.Visible;

                    /* initialize the cameramode object and pass it the event handler */
                    camera1_display = new CameraProcess(display_tick1, 0, 0, 30);
                    camera1_process = new CameraProcess(timer_Tick1, 0, 0, 30);

                    camera1_process.startTimer();
                }
                else
                {
                    camera1_process.stopTimer();
                    camera1_process = null;
                    Camera1.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No Camera Found");
            }

        }

        void display_tick1(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera4_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                DisplayFrame(processFrame, 1);
            }
        }

        void timer_Tick1(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera1_process.queryFrame();

            timer1_proc = DateTime.Now.Millisecond;
            Timer_test.Content = timer1_proc;

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                ProcessFrame(processFrame, 1, timer1_proc);
            }
        }
        // akhir pengaturan camera 1

        // untuk mengatur camera 2
        private void Camera2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (camera2_process == null)
                {
                    Camera2.Visibility = Visibility.Visible;

                    /* initialize the cameramode object and pass it the event handler */
                    camera2_process = new CameraProcess(timer_Tick2, 1, 0, 10);
                    camera2_display = new CameraProcess(display_tick2, 1, 0, 30);

                    camera2_process.startTimer();
                }
                else
                {
                    camera2_process.stopTimer();
                    camera2_process = null;
                    Camera2.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No Camera Found");
            }
        }

        void display_tick2(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera2_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                DisplayFrame(processFrame, 2);
            }
        }

        void timer_Tick2(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera2_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                ProcessFrame(processFrame, 2, timer2_proc);
            }
        }
        // akhir pengaturan camera 2

        // untuk mengatur camera 3
        private void Camera3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (camera3_process == null)
                {
                    Camera3.Visibility = Visibility.Visible;

                    /* initialize the cameramode object and pass it the event handler */
                    camera3_process = new CameraProcess(timer_Tick3, 2, 1, 0);
                    camera3_display = new CameraProcess(display_tick3, 2, 0, 30);

                    camera3_process.startTimer();
                }
                else
                {
                    camera3_process.stopTimer();
                    camera3_process = null;
                    Camera3.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No Camera Found");
            }
        }

        void display_tick3(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera3_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                DisplayFrame(processFrame, 3);
            }
        }

        void timer_Tick3(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera3_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                ProcessFrame(processFrame, 3, timer3_proc);
            }
        }
        // akhir pengaturan camera 3

        // untuk mengatur camera 4
        private void Camera4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (camera4_process == null)
                {
                    Camera4.Visibility = Visibility.Visible;

                    /* initialize the cameramode object and pass it the event handler */
                    camera4_process = new CameraProcess(timer_Tick4, 3, 1, 0);
                    camera4_display = new CameraProcess(display_tick4, 3, 0, 30);


                    camera4_process.startTimer();
                }
                else
                {
                    camera4_process.stopTimer();
                    camera4_process = null;
                    Camera4.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No Camera Found");
            }
        }

        void display_tick4(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera4_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                DisplayFrame(processFrame, 4);
            }
        }

        void timer_Tick4(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera4_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                ProcessFrame(processFrame, 4, timer4_proc);
            }
        }
        // akhir pengaturan camera 4

        private void DisplayFrame(Image<Bgr, Byte> displayproc, int cameranumber)
        {
            // display ke gui
            if (cameranumber == 1)
            {
                var fps1 = camera1_display.Getfps();
                CvInvoke.PutText(displayproc, fps1.ToString(), fpsloc, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                Camera1.Source = BitmapSourceConvert.ToBitmapSource(displayproc);
            }
            else if (cameranumber == 2)
            {
                var fps2 = camera2_display.Getfps();
                CvInvoke.PutText(displayproc, fps2.ToString(), fpsloc, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                Camera2.Source = BitmapSourceConvert.ToBitmapSource(displayproc);
            }
            else if (cameranumber == 3)
            {
                var fps3 = camera3_display.Getfps();
                CvInvoke.PutText(displayproc, fps3.ToString(), fpsloc, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                Camera3.Source = BitmapSourceConvert.ToBitmapSource(displayproc);
            }
            else if (cameranumber == 4)
            {
                var fps4 = camera4_display.Getfps();
                CvInvoke.PutText(displayproc, fps4.ToString(), fpsloc, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                Camera4.Source = BitmapSourceConvert.ToBitmapSource(displayproc);
            }
        }

        private void ProcessFrame(Image<Bgr, Byte> imageproc, int cameranumber, int timer)
        {
            var counter = 0;
            var numface = 0;
            var status = 0;
            var dataShape = new ConvNetSharp.Volume.Shape(48, 48, 1, 1);
            var data = new double[dataShape.TotalLength];

            /* Cek apakah ada frame yang diambil */
            if (imageproc != null)
            {
                var grayframe = imageproc.Convert<Gray, byte>();
                var faces = cascade.DetectMultiScale(grayframe, 1.1, 10, System.Drawing.Size.Empty); //the actual face detection happens here

                foreach (var face in faces)
                {
                    numface = numface + 1;
                    imageproc.Draw(face, new Bgr(System.Drawing.Color.Blue), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them

                    result = imageproc.Copy(face).Convert<Gray, byte>().Resize(48, 48, Inter.Linear); //wajah yang akan di kenali ekspresinya

                    // convert image to mat
                    Mat matImage = new Mat();
                    matImage = result.Mat;

                    // create volume and fill volume with pixels
                    var emotion = BuilderInstance<double>.Volume.From(data, dataShape);
                    for (var i = 0; i < 48; i++)
                    {
                        for (var j = 0; j < 48; j++)
                        {
                            emotion.Set(i, j, 0, MatExtension.GetValue(matImage, i, j));
                        }
                    }

                    // feed the network with volume
                    var results = fernet.Forward(emotion);
                    var c = fernet.GetPrediction();

                    var frresults = frnet.Forward(emotion);
                    var d = frnet.GetPrediction();

                    // mengakses softmax layer expression recognition
                    var softmaxLayer = fernet.Layers[fernet.Layers.Count - 1] as SoftmaxLayer;
                    var activation = softmaxLayer.OutputActivation;
                    var N = activation.Shape.GetDimension(3);
                    var C = activation.Shape.GetDimension(2);

                    // mengambil setiap confidence level dari setiap label
                    for (var k = 0; k < 7; k++)
                    {
                        net_output[k] = Math.Round(activation.Get(1, 1, (k + 1), 0) * 100);
                    }

                    // display prediction result
                    // emotion prediction
                    if (c[0] == 0)
                    {
                        emotionstring = emotion_labels[0];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Blue), 3);
                    }
                    else
                    if (c[0] == 1)
                    {
                        emotionstring = emotion_labels[1];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Red), 3);
                    }
                    else
                    if (c[0] == 2)
                    {
                        emotionstring = emotion_labels[2];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Yellow), 3);
                    }
                    else
                    if (c[0] == 3)
                    {
                        emotionstring = emotion_labels[3];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Green), 3);
                    }
                    else
                    if (c[0] == 4)
                    {
                        emotionstring = emotion_labels[4];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.HotPink), 3);
                    }
                    else
                    if (c[0] == 5)
                    {
                        emotionstring = emotion_labels[5];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Orange), 3);
                    }
                    else
                    if (c[0] == 6)
                    {
                        emotionstring = emotion_labels[6];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Purple), 3);
                    }

                    // face recognition prediction
                    if (d[0] == 0)
                    {
                        CvInvoke.PutText(imageproc, fr_labels[0] + "-" + emotionstring, face.Location, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                        //CvInvoke.PutText(imageproc, emotionstring, face.Location, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                        // create client instance   
                        /*MqttClient client = new MqttClient("192.168.42.1");
                        byte code = client.Connect(Guid.NewGuid().ToString());
                        client.ProtocolVersion = MqttProtocolVersion.Version_3_1;
                        //var hourDifference = (DateTime.Now - lastNotificationTime).TotalHours;
                        //Console.WriteLine("hour difference : " + hourDifference);
                        //if (hourDifference > 0.1)
                        //{
                        Console.Write("Test identifikasi ekspresi wajah");
                        //lastNotificationTime = DateTime.Now;
                        ushort msgId = client.Publish("my_topic", // topic
                        Encoding.UTF8.GetBytes(fr_labels[0] + " merasa " + emotionstring), // message body
                        MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                        false); // retained
                        //}*/
                        // update array
                        while (status == 0)
                        {
                            if (val_net[numface - 1, counter] < 0)
                            {
                                val_net[numface - 1, counter] = net_output[0];
                                val_lel[numface - 1, counter] = net_output[1];
                                val_bin[numface - 1, counter] = net_output[2];
                                val_jut[numface - 1, counter] = net_output[3];
                                val_sen[numface - 1, counter] = net_output[4];
                                val_jik[numface - 1, counter] = net_output[5];
                                val_fru[numface - 1, counter] = net_output[6];
                                val_bos[numface - 1, counter] = net_output[7];
                                status = 1;
                            }
                            else
                            {
                                counter = counter + 1;
                            }
                        }

                        chart1(counter);
                    }
                    else
                    if (d[0] == 1)
                    {
                        CvInvoke.PutText(imageproc, fr_labels[1] + "-" + emotionstring, face.Location, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                        //CvInvoke.PutText(imageproc, emotionstring, face.Location, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                        /*// create client instance   
                        MqttClient client = new MqttClient("192.168.42.1");
                        byte code = client.Connect(Guid.NewGuid().ToString());
                        client.ProtocolVersion = MqttProtocolVersion.Version_3_1;
                        //var hourDifference = (DateTime.Now - lastNotificationTime).TotalHours;
                        //Console.WriteLine("hour difference : " + hourDifference);
                        //if (hourDifference > 0.1)
                        //{
                        Console.Write("Test identifikasi ekspresi wajah");
                        //lastNotificationTime = DateTime.Now;
                        ushort msgId = client.Publish("my_topic", // topic
                        Encoding.UTF8.GetBytes(fr_labels[1] + " merasa " + emotionstring), // message body
                        MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                        false); // retained
                                //}
                                */
                        // update array
                        while (status == 0)
                        {
                            if (val_net[numface - 1, counter] < 0)
                            {
                                val_net[numface - 1, counter] = net_output[0];
                                val_lel[numface - 1, counter] = net_output[1];
                                val_bin[numface - 1, counter] = net_output[2];
                                val_jut[numface - 1, counter] = net_output[3];
                                val_sen[numface - 1, counter] = net_output[4];
                                val_jik[numface - 1, counter] = net_output[5];
                                val_fru[numface - 1, counter] = net_output[6];
                                val_bos[numface - 1, counter] = net_output[7];
                                status = 1;
                            }
                            else
                            {
                                counter = counter + 1;
                            }
                        }

                        chart2(counter);
                    }
                    else
                    if (d[0] == 2)
                    {
                        CvInvoke.PutText(imageproc, fr_labels[2] + "-" + emotionstring, face.Location, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                        //CvInvoke.PutText(imageproc, emotionstring, face.Location, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                        /*//updatearray(status, numface, counter);
                        // create client instance   
                        MqttClient client = new MqttClient("192.168.42.1");
                        byte code = client.Connect(Guid.NewGuid().ToString());
                        client.ProtocolVersion = MqttProtocolVersion.Version_3_1;
                        //var hourDifference = (DateTime.Now - lastNotificationTime).TotalHours;
                        //Console.WriteLine("hour difference : " + hourDifference);
                        //if (hourDifference > 0.1)
                        //{
                            Console.Write("Test identifikasi ekspresi wajah");
                            //lastNotificationTime = DateTime.Now;
                            ushort msgId = client.Publish("my_topic", // topic
                            Encoding.UTF8.GetBytes(fr_labels[2] + " merasa " + emotionstring), // message body
                            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                            false); // retained
                        //}

                        // update array*/
                        while (status == 0)
                        {
                            if (val_net[numface - 1, counter] < 0)
                            {
                                val_net[numface - 1, counter] = net_output[0];
                                val_lel[numface - 1, counter] = net_output[1];
                                val_bin[numface - 1, counter] = net_output[2];
                                val_jut[numface - 1, counter] = net_output[3];
                                val_sen[numface - 1, counter] = net_output[4];
                                val_jik[numface - 1, counter] = net_output[5];
                                val_fru[numface - 1, counter] = net_output[6];
                                val_bos[numface - 1, counter] = net_output[7];
                                status = 1;
                            }
                            else
                            {
                                counter = counter + 1;
                            }
                        }

                        chart3(counter);

                        //addtodatabase(numface, counter);
                    }

                    
                }

                // display ke gui
                if (cameranumber == 1)
                {
                    var fps1 = camera1_display.Getfps();
                    //CvInvoke.PutText(imageproc, fps1.ToString(), fpsloc, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                    Camera1.Source = BitmapSourceConvert.ToBitmapSource(imageproc);
                }
                else if (cameranumber == 2)
                {
                    var fps2 = camera2_display.Getfps();
                    //CvInvoke.PutText(imageproc, fps2.ToString(), fpsloc, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                    Camera2.Source = BitmapSourceConvert.ToBitmapSource(imageproc);
                }
                else if (cameranumber == 3)
                {
                    var fps3 = camera3_display.Getfps();
                    //CvInvoke.PutText(imageproc, fps3.ToString(), fpsloc, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                    Camera3.Source = BitmapSourceConvert.ToBitmapSource(imageproc);
                }
                else if (cameranumber == 4)
                {
                    var fps4 = camera4_display.Getfps();
                    //CvInvoke.PutText(imageproc, fps4.ToString(), fpsloc, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                    Camera4.Source = BitmapSourceConvert.ToBitmapSource(imageproc);
                }
            }
            else
            {
                DisplayFrame(imageproc, cameranumber);
            }


            
        }

        public void updatearray(int status, int numface, int counter)
        {
            // update array
            while (status == 0)
            {
                if (val_net[numface - 1, counter] < 0)
                {
                    val_net[numface - 1, counter] = net_output[0];
                    val_lel[numface - 1, counter] = net_output[1];
                    val_bin[numface - 1, counter] = net_output[2];
                    val_jut[numface - 1, counter] = net_output[3];
                    val_sen[numface - 1, counter] = net_output[4];
                    val_jik[numface - 1, counter] = net_output[5];
                    val_fru[numface - 1, counter] = net_output[6];
                    val_bos[numface - 1, counter] = net_output[7];
                    status = 1;
                }
                else
                {
                    counter = counter + 1;
                }
            }
        }

        public void chart1(int counter)
        {
            cv_net1.Add(val_net[0, counter]);
            cv_sen1.Add(val_sen[0, counter]);
            cv_bin1.Add(val_bin[0, counter]);
            cv_lel1.Add(val_lel[0, counter]);
            cv_fru1.Add(val_fru[0, counter]);
            cv_jut1.Add(val_jut[0, counter]);
            cv_jik1.Add(val_jik[0, counter]);
            cv_bos1.Add(val_bos[0, counter]);

        }

        public void chart2(int counter)
        {
            var now = System.DateTime.Now;

            cv_net2.Add(val_net[0, counter]);
            cv_sen2.Add(val_sen[0, counter]);
            cv_bin2.Add(val_bin[0, counter]);
            cv_lel2.Add(val_lel[0, counter]);
            cv_fru2.Add(val_fru[0, counter]);
            cv_jut2.Add(val_jut[0, counter]);
            cv_jik2.Add(val_jik[0, counter]);
            cv_bos2.Add(val_bos[0, counter]);

            /*
            if (cv_net2.Count > 30)
            {
                cv_net2.RemoveAt(0);
                cv_ang2.RemoveAt(0);
                cv_dis2.RemoveAt(0);
                cv_fea2.RemoveAt(0);
                cv_hap2.RemoveAt(0);
                cv_sad2.RemoveAt(0);
                cv_sur2.RemoveAt(0);
            }*/


        }

        public void chart3(int counter)
        {
            cv_net2.Add(val_net[0, counter]);
            cv_sen2.Add(val_sen[0, counter]);
            cv_bin2.Add(val_bin[0, counter]);
            cv_lel2.Add(val_lel[0, counter]);
            cv_fru2.Add(val_fru[0, counter]);
            cv_jut2.Add(val_jut[0, counter]);
            cv_jik2.Add(val_jik[0, counter]);
            cv_bos2.Add(val_bos[0, counter]);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Chart3.Height = 500;
            Chart3.Width = 1000;
            Chart3.AxisX[0].MinValue = double.NaN;
            Chart3.AxisX[0].MaxValue = double.NaN;
            Chart3.AxisY[0].MinValue = double.NaN;
            Chart3.AxisY[0].MaxValue = double.NaN;

            var myChart = new LiveCharts.Wpf.CartesianChart
            {
                DisableAnimations = true,
                Width = 1200,
                Height = 400,
                Series = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Neutral",
                        Values = cv_net3
                    },
                    new LineSeries
                    {
                        Title = "Senang",
                        Values = cv_sen3
                    },
                    new LineSeries
                    {
                        Title = "Bingung",
                        Values = cv_bin3
                    },
                    new LineSeries
                    {
                        Title = "Lelah",
                        Values = cv_lel3
                    },
                    new LineSeries
                    {
                        Title = "Frustasi",
                        Values = cv_fru3
                    },
                    new LineSeries
                    {
                        Title = "Terkejut",
                        Values = cv_jut3
                    },
                    new LineSeries
                    {
                        Title = "Jijik",
                        Values = cv_jik3
                    },
                    new LineSeries
                    {
                        Title = "Bosan",
                        Values = cv_bos3
                    },
                }
            };
            var viewbox = new Viewbox();
            viewbox.Child = myChart;
            viewbox.Measure(myChart.RenderSize);
            viewbox.Arrange(new Rect(new System.Windows.Point(0, 0), myChart.RenderSize));
            myChart.Update(true, true); //force chart redraw
            viewbox.UpdateLayout();

            SaveToPng(myChart, "chart.png");
            saveToDB();
            //png file was created at the root directory.

            Chart3.Height = 178;
            Chart3.Width = 324;
        }

        public void SaveToPng(FrameworkElement visual, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            EncodeVisual(visual, fileName, encoder);
        }

        private static void EncodeVisual(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            var bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);
            using (var stream = File.Create(fileName)) encoder.Save(stream);
        }

        public class EkspresiSiswa
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public long sessionId { get; set; }
            public string NamaSiswa { get; set; }
            public string EmosiNetral { get; set; }
            public string EmosiSenang { get; set; }
            public string EmosiBingung { get; set; }
            public string EmosiLelah { get; set; }
            public string EmosiFrustasi { get; set; }
            public string EmosiTerkejut { get; set; }
            public string EmosiJijik { get; set; }
            public string EmosiBosan { get; set; }
        }

        private void saveToDB()
        {
            var databasePath = (@"..\..\DBEkspresiSiswa.db");

            var db = new SQLiteConnection(databasePath);
            db.CreateTable<EkspresiSiswa>();
            int siswaCount = 0;
            DateTime now = DateTime.Now;
            var dateId = long.Parse(now.ToString("yyyyMMddHHmmss"));
            foreach (string namaSiswa in fr_labels)
            {
                double[] temp_net = new double[5000];
                double[] temp_sen = new double[5000];
                double[] temp_bin = new double[5000];
                double[] temp_lel = new double[5000];
                double[] temp_fru = new double[5000];
                double[] temp_jut = new double[5000];
                double[] temp_jik = new double[5000];
                double[] temp_bos = new double[5000];

                for (int i = 0; i < 5000; i++)
                {
                    temp_net[i] = val_net[siswaCount, i];
                }
                for (int i = 0; i < 5000; i++)
                {
                    temp_sen[i] = val_sen[siswaCount, i];
                }
                for (int i = 0; i < 5000; i++)
                {
                    temp_bin[i] = val_bin[siswaCount, i];
                }
                for (int i = 0; i < 5000; i++)
                {
                    temp_lel[i] = val_lel[siswaCount, i];
                }
                for (int i = 0; i < 5000; i++)
                {
                    temp_fru[i] = val_fru[siswaCount, i];
                }
                for (int i = 0; i < 5000; i++)
                {
                    temp_jut[i] = val_jut[siswaCount, i];
                }
                for (int i = 0; i < 5000; i++)
                {
                    temp_jik[i] = val_jik[siswaCount, i];
                }
                for (int i = 0; i < 5000; i++)
                {
                    temp_bos[i] = val_bos[siswaCount, i];
                }

                string string_net = JsonConvert.SerializeObject(temp_net);
                string string_sen = JsonConvert.SerializeObject(temp_sen);
                string string_bin = JsonConvert.SerializeObject(temp_bin);
                string string_lel = JsonConvert.SerializeObject(temp_lel);
                string string_fru = JsonConvert.SerializeObject(temp_fru);
                string string_jut = JsonConvert.SerializeObject(temp_jut);
                string string_jik = JsonConvert.SerializeObject(temp_jik);
                string string_bos = JsonConvert.SerializeObject(temp_bos);

                Console.WriteLine("nama siswa ke " + (siswaCount + 1) + " : " + namaSiswa);
                var s = db.Insert(new EkspresiSiswa()
                {
                    sessionId = dateId, // Id berisi kode unik untuk setiap sesi pengajaran
                    NamaSiswa = namaSiswa,
                    EmosiNetral = string_net,
                    EmosiSenang = string_sen,
                    EmosiBingung = string_bin,
                    EmosiLelah = string_lel,
                    EmosiFrustasi = string_fru,
                    EmosiTerkejut = string_jut,
                    EmosiJijik = string_jik,
                    EmosiBosan = string_bos

                });
                siswaCount++;
                var absolutePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pengajaran2.csv");
                using (TextWriter fileWriter = new StreamWriter(absolutePath))
                {
                    var csv = new CsvWriter(fileWriter);
                    csv.WriteRecords(temp_sen);
                    csv.NextRecord();
                    csv.WriteRecords(temp_net);
                }

            }
        }

        public ChartValues<double> cv_net1 { get; set; }
        public ChartValues<double> cv_sen1 { get; set; }
        public ChartValues<double> cv_bin1 { get; set; }
        public ChartValues<double> cv_lel1 { get; set; }
        public ChartValues<double> cv_fru1 { get; set; }
        public ChartValues<double> cv_jut1 { get; set; }
        public ChartValues<double> cv_jik1 { get; set; }
        public ChartValues<double> cv_bos1 { get; set; }

        public ChartValues<double> cv_net2 { get; set; }
        public ChartValues<double> cv_sen2 { get; set; }
        public ChartValues<double> cv_bin2 { get; set; }
        public ChartValues<double> cv_lel2 { get; set; }
        public ChartValues<double> cv_fru2 { get; set; }
        public ChartValues<double> cv_jut2 { get; set; }
        public ChartValues<double> cv_jik2 { get; set; }
        public ChartValues<double> cv_bos2 { get; set; }

        public ChartValues<double> cv_net3 { get; set; }
        public ChartValues<double> cv_sen3 { get; set; }
        public ChartValues<double> cv_bin3 { get; set; }
        public ChartValues<double> cv_lel3 { get; set; }
        public ChartValues<double> cv_fru3 { get; set; }
        public ChartValues<double> cv_jut3 { get; set; }
        public ChartValues<double> cv_jik3 { get; set; }
        public ChartValues<double> cv_bos3 { get; set; }

        /// <summary>
        /// TAB 2, Dipakai untuk mengambil dataset
        /// </summary>
        /// Deklarasi variabel directory training set
        string net_train = @"..\..\..\Dataset\FERDataset\Training\0\";
        string tir_train = @"..\..\..\Dataset\FERDataset\Training\1\";
        string con_train = @"..\..\..\Dataset\FERDataset\Training\2\";
        string sur_train = @"..\..\..\Dataset\FERDataset\Training\3\";
        string hap_train = @"..\..\..\Dataset\FERDataset\Training\4\";
        string dis_train = @"..\..\..\Dataset\FERDataset\Training\5\";
        string fru_train = @"..\..\..\Dataset\FERDataset\Training\6\";
        string bor_train = @"..\..\..\Dataset\FERDataset\Training\7\";

        /// Deklarasi variabel directory test set
        string net_test = @"..\..\..\Dataset\FERDataset\Test\0\";
        string tir_test = @"..\..\..\Dataset\FERDataset\Test\1\";
        string con_test = @"..\..\..\Dataset\FERDataset\Test\2\";
        string sur_test = @"..\..\..\Dataset\FERDataset\Test\3\";
        string hap_test = @"..\..\..\Dataset\FERDataset\Test\4\";
        string dis_test = @"..\..\..\Dataset\FERDataset\Test\5\";
        string fru_test = @"..\..\..\Dataset\FERDataset\Test\6\";
        string bor_test = @"..\..\..\Dataset\FERDataset\Test\7\";

        /// Deklarasi variabel directory validation set
        string net_valid = @"..\..\..\Dataset\FERDataset\Validation\0\";
        string tir_valid = @"..\..\..\Dataset\FERDataset\Validation\1\";
        string con_valid = @"..\..\..\Dataset\FERDataset\Validation\2\";
        string sur_valid = @"..\..\..\Dataset\FERDataset\Validation\3\";
        string hap_valid = @"..\..\..\Dataset\FERDataset\Validation\4\";
        string dis_valid = @"..\..\..\Dataset\FERDataset\Validation\5\";
        string fru_valid = @"..\..\..\Dataset\FERDataset\Validation\6\";
        string bor_valid = @"..\..\..\Dataset\FERDataset\Validation\7\";

        int camstatus;
        int facecounter = 1;
        string namaSiswa;
        List<string> ListSiswa = new List<string>();

        private void CameraClear(object sender, RoutedEventArgs e)
        {
            try
            {
                camera1_process.stopTimer();
                camera1_process = null;
                camera2_process.stopTimer();
                camera2_process = null;
                camera3_process.stopTimer();
                camera3_process = null;
                camera4_process.stopTimer();
                camera4_process = null;
            } catch
            {
                MessageBox.Show("camea has been turned off");
            }
            

            TakeDataset.Visibility = Visibility.Hidden;
        }

        private void Dataset_tick1(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera1_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                    DatasetFrame(processFrame, 1);
            }
        }

        private void UseCamera1(object sender, RoutedEventArgs e)
        {

            try
            {
                TakeDataset.Visibility = Visibility.Visible;
                /* initialize the cameramode object and pass it the event handler */
                camera1_process = new CameraProcess(Dataset_tick1, 0, 0, 30);

                camera1_process.startTimer();

                camstatus = 1;
            }
            catch
            {
                MessageBox.Show("No Camera Found");
            }
        }

        private void Dataset_tick2(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera2_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                DatasetFrame(processFrame, 2);
            }
        }

        private void UseCamera2(object sender, RoutedEventArgs e)
        {

            try
            {
                TakeDataset.Visibility = Visibility.Visible;
                /* initialize the cameramode object and pass it the event handler */
                camera2_process = new CameraProcess(Dataset_tick2, 0, 0, 30);

                camera2_process.startTimer();

                camstatus = 2;
            }
            catch
            {
                MessageBox.Show("No Camera Found");
            }
        }

        private void Dataset_tick3(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera3_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                DatasetFrame(processFrame, 3);
            }
        }

        private void UseCamera3(object sender, RoutedEventArgs e)
        {

            try
            {
                TakeDataset.Visibility = Visibility.Visible;
                /* initialize the cameramode object and pass it the event handler */
                camera3_process = new CameraProcess(Dataset_tick3, 0, 0, 30);

                camera3_process.startTimer();

                camstatus = 3;
            }
            catch
            {
                MessageBox.Show("No Camera Found");
            }
        }

        private void Dataset_tick4(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera4_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                DatasetFrame(processFrame, 4);
            }
        }

        private void UseCamera4(object sender, RoutedEventArgs e)
        {
            try
            {
                TakeDataset.Visibility = Visibility.Visible;

                /* initialize the cameramode object and pass it the event handler */
                camera4_process = new CameraProcess(Dataset_tick4, 0, 0, 30);

                camera4_process.startTimer();

                camstatus = 4;
            }
            catch
            {
                MessageBox.Show("No Camera Found");
            }
        }

        private void DatasetFrame(Image<Bgr, Byte> imageproc, int cameranumber)
        {
            /* Check to see that there was a frame collected */
            if (imageproc != null)
            {
                var grayframe = imageproc.Convert<Gray, byte>();
                var faces = cascade.DetectMultiScale(grayframe, 1.1, 10, System.Drawing.Size.Empty); //the actual face detection happens here

                foreach (var face in faces)
                {
                    imageproc.Draw(face, new Bgr(System.Drawing.Color.Blue), 3); //the detected face(s) is highlighted here using a box that is drawn around it
                }

                TakeDataset.Source = BitmapSourceConvert.ToBitmapSource(imageproc);
            }
        }

        private void AddtoDataset (object sender, RoutedEventArgs e)
        {
            try
            {
                if (camstatus == 1)
                {
                    Image<Bgr, Byte> dataFrame = camera1_process.queryFrame();
                    facecounter = facecounter + 1;
                    AddtoDataset_cont(dataFrame);
                }
                else
            if (camstatus == 2)
                {
                    Image<Bgr, Byte> dataFrame = camera2_process.queryFrame();
                    facecounter = facecounter + 1;
                    AddtoDataset_cont(dataFrame);
                }
                else
            if (camstatus == 3)
                {
                    Image<Bgr, Byte> dataFrame = camera3_process.queryFrame();
                    facecounter = facecounter + 1;
                    AddtoDataset_cont(dataFrame);
                }
                else
            if (camstatus == 4)
                {
                    Image<Bgr, Byte> dataFrame = camera4_process.queryFrame();
                    facecounter = facecounter + 1;
                    AddtoDataset_cont(dataFrame);
                }
            } catch
            {
                MessageBox.Show("Enable Camera 1st");
            }
            
        }

        private void AddtoDataset_cont(Image<Bgr, Byte> imageproc)
        {
            var ttv = facecounter % 3;

            namaSiswa = NameBox.Text;

            /* Check to see that there was a frame collected */
            if (imageproc != null)
            {
                var grayframe = imageproc.Convert<Gray, byte>();
                var faces = cascade.DetectMultiScale(grayframe, 1.1, 10, System.Drawing.Size.Empty); //the actual face detection happens here

                foreach (var face in faces)
                {
                    imageproc.Draw(face, new Bgr(System.Drawing.Color.Blue), 3); //the detected face(s) is highlighted here using a box that is drawn around it
                    result = imageproc.Copy(face).Convert<Gray, byte>().Resize(48, 48, Inter.Linear);
                }
                var Iface = result.Resize(192, 192, Inter.Linear);

                if (EmotionBox.SelectedIndex == 0 && ttv != 0)
                {
                    var saveDir = Directory.GetFiles(net_train);

                    result.Save(net_train + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 1 && ttv != 0)
                {
                    var saveDir = Directory.GetFiles(tir_train);

                    result.Save(tir_train + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 2 && ttv != 0)
                {
                    var saveDir = Directory.GetFiles(con_train);

                    result.Save(con_train + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 3 && ttv != 0)
                {
                    var saveDir = Directory.GetFiles(sur_train);

                    result.Save(sur_train + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 4 && ttv != 0)
                {
                    var saveDir = Directory.GetFiles(hap_train);

                    result.Save(hap_train + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 5 && ttv != 0)
                {
                    var saveDir = Directory.GetFiles(dis_train);

                    result.Save(dis_train + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 6 && ttv != 0)
                {
                    var saveDir = Directory.GetFiles(fru_train);

                    result.Save(fru_train + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 7 && ttv != 0)
                {
                    var saveDir = Directory.GetFiles(bor_train);

                    result.Save(bor_train + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 0 && ttv == 0)
                {
                    var saveDir = Directory.GetFiles(net_test);

                    result.Save(net_test + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 1 && ttv == 0)
                {
                    var saveDir = Directory.GetFiles(tir_test);

                    result.Save(tir_test + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 2 && ttv == 0)
                {
                    var saveDir = Directory.GetFiles(con_test);

                    result.Save(con_test + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 3 && ttv == 0)
                {
                    var saveDir = Directory.GetFiles(sur_test);

                    result.Save(sur_test + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 4 && ttv == 0)
                {
                    var saveDir = Directory.GetFiles(hap_test);

                    result.Save(hap_test + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 5 && ttv == 0)
                {
                    var saveDir = Directory.GetFiles(dis_test);

                    result.Save(dis_test + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 6 && ttv == 0)
                {
                    var saveDir = Directory.GetFiles(fru_test);

                    result.Save(fru_test + saveDir.Length + ".png");
                }
                else
                if (EmotionBox.SelectedIndex == 7 && ttv == 0)
                {
                    var saveDir = Directory.GetFiles(bor_test);

                    result.Save(bor_test + saveDir.Length + ".png");
                }


                if (FaceBox.SelectedIndex == 0)
                {
                    ListSiswa.Add(NameBox.Text);

                    var index2 = ListSiswa.FindIndex(a => a == NameBox.Text);
                    var index = index2 - 1;

                    // If directory does not exist, create it. 
                    if (!Directory.Exists(@"..\..\..\Dataset\FRDataset\Training\" + index + @"\"))
                    {
                        Directory.CreateDirectory(@"..\..\..\Dataset\FRDataset\Training\" + index + @"\");
                    }

                    var siswajson = JsonConvert.SerializeObject(ListSiswa);
                    System.IO.File.WriteAllText(@"..\..\..\network\listsiswa.json", siswajson);

                    var saveDir = Directory.GetFiles(@"..\..\..\Dataset\FRDataset\Training\" + index + @"\");
                    result.Save(@"..\..\..\Dataset\FRDataset\Training\" + index + @"\" + saveDir.Length + ".png");

                    MessageBox.Show("Siswa baru telah ditambahkan, silahkan refresh list");
                }
                else
                {
                    var index2 = ListSiswa.FindIndex(a => a == FaceBox.Text);
                    var index = index2 - 1;

                    // If directory does not exist, create it. 
                    if (!Directory.Exists(@"..\..\..\Dataset\FRDataset\Training\" + index + @"\"))
                    {
                        Directory.CreateDirectory(@"..\..\..\Dataset\FRDataset\Training\" + index + @"\");
                    }


                    var saveDir = Directory.GetFiles(@"..\..\..\Dataset\FRDataset\Training\" + index + @"\");
                    result.Save(@"..\..\..\Dataset\FRDataset\Training\" + index + @"\" + saveDir.Length + ".png");
                }

                facecounter++;
                IdentifiedFace.Source = BitmapSourceConvert.ToBitmapSource(Iface);
            }
        }

        private void RefreshList(object sender, RoutedEventArgs e)
        {
            var readsiswajson = System.IO.File.ReadAllText(@"..\..\..\Network\listsiswa.json");
            ListSiswa = JsonConvert.DeserializeObject<System.Collections.Generic.List<string>>(readsiswajson);
            FaceBox.ItemsSource = ListSiswa;
        }

        private void TrainFERNet(object sender, RoutedEventArgs e)
        {
            
        }

        private void CreateMNistDataset(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(@"..\..\..\JPG-PNG-to-MNIST\build\convert-images-to-mnist-format.exe");
                MessageBox.Show("Dataset telah dibuat");
            }
            catch
            {
                MessageBox.Show("Dataset gagal dibuat");
            }
            
            
        }
    }

    public static class BitmapSourceConvert
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap();

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr);
                return bs;
            }
        }
    }
    
    public class DateModel
    {
        public System.DateTime DateTime { get; set; }
    }
}
