using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ImageToASCIIconverter
{
    public partial class Form1 : Form
    {
        [DllImport(@"C:\Users\alert\OneDrive\Dokumenty\STUDIA\JA\JA projekt\ImageToASCIIconverter\ImageToASCIIconverter\x64\Debug\JaAsm.dll")]
        private static extern unsafe int convertLineAsm(int lineNr, byte* imageInBytesPtr, char* textLinePtr, int asciiImWidth);
        
        private static Semaphore semaphore = new Semaphore(8, 8);
        private enum Language
        {
            CS,
            ASM
        };

        private Language language;
        //private string[] _AsciiChars = { "#", "#", "@", "%", "=", "+", "*", ":", "-", ".", "&nbsp;" };
        private const int ASCII_IM_W = 256;      // 256
        private char[] asciiChars = { '#', '#', '@', '%', '=', '+', '*', ':', '-', '.', ' ' };
        private string output;
        private byte[] imageInBytes;
        //unsafe private byte* imageInBytes_Ptr;
        private string[] textLines;
        //private char[][] asciiOutput;
        private char[] asciiOutput;
        private Bitmap image;

        public Form1()
        {
            InitializeComponent();
            trackBar.Value = 8;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }


        unsafe void convertLine(int lineNr)
        {
            fixed (byte* imageInBytesPtr = imageInBytes)
            {
                fixed (char* textLinePtr = asciiOutput)
                {
                    semaphore.WaitOne();
                    if (language == Language.CS)
                    {
                        convertLineCs(lineNr, imageInBytesPtr, textLinePtr, ASCII_IM_W);
                    }
                    else
                    {
                        try
                        {
                            convertLineAsm(lineNr, imageInBytesPtr, textLinePtr, ASCII_IM_W);
                        }
                        catch (System.NullReferenceException) { }
                    }                        
                    semaphore.Release();
                }

            }
            //fixed (char* textLinesPtr = textLines[lineNr])
            //{
            //}
        }

        private void btnConvertToAscii_Click(object sender, EventArgs e)
        {
            if (image == null)
            {
                MessageBox.Show("No file chosen!", "File error");
            }
            else
            {
                // ------------------------------- PREPROCESSING --------------------------------------
                btnConvertToAscii.Enabled = false;
                if (radioButton1.Checked)
                    language = Language.CS;
                else
                    language = Language.ASM;
                Bitmap resizedImage = GetReSizedImage(this.image, ASCII_IM_W);

                textLines = new string[resizedImage.Height];
                asciiOutput = new char[resizedImage.Height * resizedImage.Width];

                for (int i = 0; i < asciiOutput.Length; i++)
                {
                    asciiOutput[i] = 'a';
                }

                imageInBytes = convertImageToArray(resizedImage);


                semaphore = new Semaphore(trackBar.Value, trackBar.Value);
                //ThreadPool.SetMaxThreads(trackBar.Value, trackBar.Value);

                Thread[] t = new Thread[resizedImage.Height];

                Stopwatch mywatch = new Stopwatch();

                // ------------------------------- CONVERSION --------------------------------------
                mywatch.Start();
                for (int h = 0; h < resizedImage.Height; h++)
                {
                    //convertLine(h);
                    int temp = h;     // capture value
                    t[h] = new Thread(() =>
                    {
                        convertLine(temp);
                    });
                    t[h].Start();
                }

                for (int i = 0; i < resizedImage.Height; i++)
                {
                    t[i].Join();
                }
                mywatch.Stop();

                // ------------------------------- POSTPROCESSING --------------------------------------
                TimeSpan ts = mywatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                label4.Text = elapsedTime + " sec";
                mywatch.Reset();


                StringBuilder singleString = new StringBuilder();
                for (int i = 0; i < resizedImage.Height; i++)
                {
                    singleString.Append(new string(asciiOutput, i * ASCII_IM_W, ASCII_IM_W));       // collect threaded conversion results
                    singleString.Append("<BR>");     // ADD new line chars
                }
                output = singleString.ToString();
                browserMain.DocumentText = "<pre>" + "<Font size=0>" + output + "</Font></pre>";
                btnConvertToAscii.Enabled = true;
            }
        }
        //unsafe void convertLineAsm(int lineNr, byte* imageInBytesPtr, char* textLinePtr) { }
        //[DllImport(@"C:\Users\alert\OneDrive\Dokumenty\STUDIA\JA\JA projekt\ImageToASCIIconverter\ImageToASCIIconverter\x64\Debug\JaAsm.dll")]
        //private static extern unsafe int convertLineAsm(int lineNr, byte* imageInBytesPtr, char* textLinePtr, int asciiImWidth);
        //private static extern unsafe int
        unsafe void convertLineCs(int lineNr, byte* imageInBytesPtr, char* textLinePtr, int asciiImWidth)
        {
            //StringBuilder sb = new StringBuilder();
            int start_pos = lineNr * asciiImWidth * 3;
            int start_pos_ascii = lineNr * asciiImWidth;            // ASCII_IM_W;
            for (int w = 0; w < asciiImWidth * 3; w += 3)          // ASCII_IM_W
            {
                //Color pixelColor = Color.FromArgb(imageInBytes[start_pos + w], imageInBytes[start_pos + w + 1], imageInBytes[start_pos + w + 2]);
                Color pixelColor = Color.FromArgb(*(imageInBytesPtr + start_pos + w), *(imageInBytesPtr + start_pos + w + 1), *(imageInBytesPtr + start_pos + w + 2));

                //Average out the RGB components to find the Gray Color
                int red = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                int green = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                int blue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                Color grayColor = Color.FromArgb(red, green, blue);

                int index = (grayColor.R * 10) / 255;

                *(textLinePtr + start_pos_ascii + (w / 3)) = asciiChars[index];
            }
        }

        


        private byte[] convertImageToArray(Bitmap image)
        {
            byte[] imageInBytes = new byte[image.Width * image.Height * 3];
            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    Color color = image.GetPixel(w, h);
                    imageInBytes[(h * ASCII_IM_W + w) * 3] = color.R;
                    imageInBytes[(h * ASCII_IM_W + w) * 3 + 1] = color.G;
                    imageInBytes[(h * ASCII_IM_W + w) * 3 + 2] = color.G;
                }
            }
            return imageInBytes;
        }

        private Bitmap GetReSizedImage(Bitmap inputBitmap, int asciiWidth)
        {
            int asciiHeight = 0;
            //Calculate the new Height of the image from its width
            asciiHeight = (int)Math.Ceiling((double)inputBitmap.Height * asciiWidth / inputBitmap.Width / 2);   // gets divided by two to prevent strechting
            Bitmap result = new Bitmap(asciiWidth, asciiHeight);
            Graphics g = Graphics.FromImage((Image)result);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(inputBitmap, 0, 0, asciiWidth, asciiHeight);
            g.Dispose();
            return result;
        }


        private void btnBrowse_Click(object sender, EventArgs e)
        {
            DialogResult diag = openFileDialog1.ShowDialog();
            if (diag == DialogResult.OK)
            {
                txtPath.Text = openFileDialog1.FileName;
            }
            try
            {
                this.image = new Bitmap(txtPath.Text, true);     //Load the Image from the specified path
                pictureBox1.Image = image;
            }
            catch (System.ArgumentException) 
            {
                MessageBox.Show("File format not supported!", "File error");
            }

        }


        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (output == null)
            {
                MessageBox.Show("Nothing to save!", "File error");
            }
            else 
            { 
                saveFileDialog1.Filter = "Text File (*.txt)|.txt|HTML (*.htm)|.htm";
                DialogResult diag = saveFileDialog1.ShowDialog();
                if (diag == DialogResult.OK)
                {
                    if (saveFileDialog1.FilterIndex == 1)
                    {
                        //If the format to be saved is HTML
                        //Replace all HTML spaces to standard spaces
                        //and all linebreaks to CarriageReturn, LineFeed
                        output = output.Replace("&nbsp;", " ").Replace("<BR>", "\r\n");
                    }
                    else
                    {
                        //use <pre></pre> tag to preserve formatting when viewing it in browser
                        output = "<pre>" + output + "</pre>";
                    }
                    StreamWriter sw = new StreamWriter(saveFileDialog1.FileName);
                    sw.Write(output);
                    sw.Flush();
                    sw.Close();
                }
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                radioButton1.Checked = false;
            }
        }



        private void browserMain_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void trackBar_Scroll_1(object sender, EventArgs e)
        {
            label2.Text = Convert.ToString(trackBar.Value);
        }

        private void radioButton1_CheckedChanged_1(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                radioButton2.Checked = false;
            }
        }




        //private string ConvertToAscii3(Bitmap image)
        //{
        //    //Boolean toggle = false;
        //    StringBuilder _asciiart = new StringBuilder();

        //    int _pixwidth = 3;
        //    int _pixhight = _pixwidth * 2;
        //    int _pixseg = _pixwidth * _pixhight;

        //    for (int h = 0; h < image.Height / _pixhight; h++)
        //    {
        //        // segment hight
        //        int _startY = (h * _pixhight);
        //        // segment width
        //        for (int w = 0; w < image.Width / _pixwidth; w++)
        //        {
        //            int _startX = (w * _pixwidth);
        //            int _allBrightness = 0;

        //                // each pix of this segment
        //                for (int y = 0; y < _pixwidth; y++)
        //                {
        //                    for (int x = 0; x < _pixhight; x++)
        //                    {
        //                        int _cY = y + _startY;
        //                        int _cX = x + _startX;
        //                        try
        //                        {
        //                            Color _c = image.GetPixel(_cX, _cY);
        //                            int _b = (int)(_c.GetBrightness() * 100);
        //                            _allBrightness = _allBrightness + _b;
        //                        }
        //                        catch
        //                        {
        //                            _allBrightness = (_allBrightness + 50);
        //                        }
        //                    }
        //                }


        //            int _sb = (_allBrightness / _pixseg);
        //            if (_sb < 10)
        //            {
        //                _asciiart.Append("#");
        //            }
        //            else if (_sb < 17)
        //            {
        //                _asciiart.Append("@");
        //            }
        //            else if (_sb < 24)
        //            {
        //                _asciiart.Append("&");
        //            }
        //            else if (_sb < 31)
        //            {
        //                _asciiart.Append("$");
        //            }
        //            else if (_sb < 38)
        //            {
        //                _asciiart.Append("%");
        //            }
        //            else if (_sb < 45)
        //            {
        //                _asciiart.Append("|");
        //            }
        //            else if (_sb < 52)
        //            {
        //                _asciiart.Append("!");
        //            }
        //            else if (_sb < 59)
        //            {
        //                _asciiart.Append(";");
        //            }
        //            else if (_sb < 66)
        //            {
        //                _asciiart.Append(":");
        //            }
        //            else if (_sb < 73)
        //            {
        //                _asciiart.Append("'");
        //            }
        //            else if (_sb < 80)
        //            {
        //                _asciiart.Append("`");
        //            }
        //            else if (_sb < 87)
        //            {
        //                _asciiart.Append(".");
        //            }
        //            else
        //            {
        //                _asciiart.Append(" ");
        //            }
        //        }
        //        _asciiart.Append("<BR>");
        //    }

        //    image.Dispose();

        //    return _asciiart.ToString();
        //}

        //private string ConvertToAscii(Bitmap image)
        //{
        //    //Boolean toggle = false;
        //    StringBuilder sb = new StringBuilder();

        //    for (int h = 0; h < image.Height; h++)
        //    {
        //        for (int w = 0; w < image.Width; w++)
        //        {
        //            Color pixelColor = image.GetPixel(w, h);
        //            //Average out the RGB components to find the Gray Color
        //            int red = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
        //            int green = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
        //            int blue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
        //            Color grayColor = Color.FromArgb(red, green, blue);

        //            //Use the toggle flag to minimize height-wise stretch
        //            //if (!toggle)
        //            ///{
        //            int index = (grayColor.R * 10) / 255;
        //            sb.Append(_AsciiChars[index]);
        //            //}
        //        }
        //        /*if (!toggle)
        //        {*/
        //        sb.Append("<BR>");
        //        /*   toggle = true;
        //       }
        //       else
        //       {
        //           toggle = false;
        //       }*/
        //    }

        //    return sb.ToString();
        //}

        ///*{
        //    for (int w = 0; w < image.Width; w++)
        //    {
        //        Color pixelColor = image.GetPixel(w, h);
        //        //Average out the RGB components to find the Gray Color
        //        int red = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
        //        int green = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
        //        int blue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
        //        Color grayColor = Color.FromArgb(red, green, blue);

        //        //Use the toggle flag to minimize height-wise stretch
        //        if (!toggle)
        //        {
        //            int index = (grayColor.R * 10) / 255;
        //            sb.Append(_AsciiChars[index]);
        //        }
        //    }
        //    if (!toggle)
        //    {
        //        sb.Append("<BR>");
        //        toggle = true;
        //    }
        //    else
        //    {
        //        toggle = false;
        //    }
        //}*/


        //private string ConvertToAscii2(Bitmap image)
        //{
        //    StringBuilder _asciiart = new StringBuilder();
        //    Rectangle bounds = new Rectangle(0, 0, image.Width, image.Height);
        //    ColorMatrix _matrix = new ColorMatrix();

        //    _matrix[0, 0] = 1 / 3f;
        //    _matrix[0, 1] = 1 / 3f;
        //    _matrix[0, 2] = 1 / 3f;
        //    _matrix[1, 0] = 1 / 3f;
        //    _matrix[1, 1] = 1 / 3f;
        //    _matrix[1, 2] = 1 / 3f;
        //    _matrix[2, 0] = 1 / 3f;
        //    _matrix[2, 1] = 1 / 3f;
        //    _matrix[2, 2] = 1 / 3f;

        //    ImageAttributes _attributes = new ImageAttributes();
        //    _attributes.SetColorMatrix(_matrix);


        //    Graphics gphGrey = Graphics.FromImage(image);
        //    gphGrey.DrawImage(image, bounds, 0, 0, image.Width, image.Height,
        //        GraphicsUnit.Pixel, _attributes);

        //    gphGrey.Dispose();
        //    int _pixwidth;

        //    /*switch (ImageSize)
        //    {
        //        case "1":
        //            {
        //                _pixwidth = 1;
        //                break;
        //            }
        //        case "2":
        //            {
        //                _pixwidth = 2;
        //                break;
        //            }
        //        case "4":
        //            {
        //                _pixwidth = 6;
        //                break;
        //            }
        //        case "5":
        //            {
        //                _pixwidth = 8;
        //                break;
        //            }
        //        default:
        //            {*/
        //                _pixwidth = 3;
        //               // break;
        //            //}
        //    //}
        //    int _pixhight = _pixwidth * 2;
        //    int _pixseg = _pixwidth * _pixhight;

        //    for (int h = 0; h < image.Height / _pixhight; h++)
        //    {
        //        // segment hight
        //        int _startY = (h * _pixhight);
        //        // segment width
        //        for (int w = 0; w < image.Width / _pixwidth; w++)
        //        {
        //            int _startX = (w * _pixwidth);
        //            int _allBrightness = 0;
        //            string Quick = "false";
        //            if (Quick == "True")
        //            {
        //                // each pix of this segment
        //                for (int y = 0; y < _pixwidth; y++)
        //                {
        //                    try
        //                    {
        //                        Color _c = image.GetPixel(_startX, y + _startY);
        //                        int _b = (int)(_c.GetBrightness() * 100);
        //                        _allBrightness = _allBrightness + _b;
        //                    }
        //                    catch
        //                    {
        //                        _allBrightness = (_allBrightness + 50);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                // each pix of this segment
        //                for (int y = 0; y < _pixwidth; y++)
        //                {
        //                    for (int x = 0; x < _pixhight; x++)
        //                    {
        //                        int _cY = y + _startY;
        //                        int _cX = x + _startX;
        //                        try
        //                        {
        //                            Color _c = image.GetPixel(_cX, _cY);
        //                            int _b = (int)(_c.GetBrightness() * 100);
        //                            _allBrightness = _allBrightness + _b;
        //                        }
        //                        catch
        //                        {
        //                            _allBrightness = (_allBrightness + 50);
        //                        }
        //                    }
        //                }
        //            }

        //            int _sb = (_allBrightness / _pixseg);
        //            if (_sb < 10)
        //            {
        //                _asciiart.Append("#");
        //            }
        //            else if (_sb < 17)
        //            {
        //                _asciiart.Append("@");
        //            }
        //            else if (_sb < 24)
        //            {
        //                _asciiart.Append("&");
        //            }
        //            else if (_sb < 31)
        //            {
        //                _asciiart.Append("$");
        //            }
        //            else if (_sb < 38)
        //            {
        //                _asciiart.Append("%");
        //            }
        //            else if (_sb < 45)
        //            {
        //                _asciiart.Append("|");
        //            }
        //            else if (_sb < 52)
        //            {
        //                _asciiart.Append("!");
        //            }
        //            else if (_sb < 59)
        //            {
        //                _asciiart.Append(";");
        //            }
        //            else if (_sb < 66)
        //            {
        //                _asciiart.Append(":");
        //            }
        //            else if (_sb < 73)
        //            {
        //                _asciiart.Append("'");
        //            }
        //            else if (_sb < 80)
        //            {
        //                _asciiart.Append("`");
        //            }
        //            else if (_sb < 87)
        //            {
        //                _asciiart.Append(".");
        //            }
        //            else
        //            {
        //                _asciiart.Append(" ");
        //            }
        //        }
        //        _asciiart.Append("<BR>");
        //    }

        //        image.Dispose();

        //        return _asciiart.ToString();
        //    }




    }
}