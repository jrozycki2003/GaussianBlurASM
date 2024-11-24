using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace JaProj
{
    public partial class MainForm : Form
    {
        private Image originalImage;

        [DllImport(@"C:\Users\Gamer\source\repos\GaussianBlurASM\x64\Debug\Blur.dll",
            CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Auto)]
        private static extern void GaussianBlur(
            IntPtr InBuffer,
            IntPtr OutBuffer,
            int height,
            int width,
            int start,
            int end,
            int blockSize);


        public MainForm()
        {
            InitializeComponent();

            librarySelector.Items.Add("C++ Library");
            librarySelector.Items.Add("ASM Library");
            librarySelector.SelectedIndex = 0;

            threadCountTrackBar.Minimum = 1;
            threadCountTrackBar.Maximum = 64;
            threadCountTrackBar.Value = Environment.ProcessorCount;

            blurAmountTrackBar.Minimum = 1;
            blurAmountTrackBar.Maximum = 20;
            blurAmountTrackBar.Value = 1;

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            threadCountLabel.Text = $"Thread Count: {threadCountTrackBar.Value}";
            blurAmountLabel.Text = $"Blur Amount: {blurAmountTrackBar.Value}";
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog FileText = new OpenFileDialog())
            {
                FileText.Filter = "Image Files|*.jpg;*.jpeg;*.png";
                FileText.Title = "Select an Image File";

                if (FileText.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Załaduj obraz w formacie JPG/PNG
                        originalImage = Image.FromFile(FileText.FileName);
                        originalImageBox.Image = new Bitmap(originalImage); // Wyświetlenie go w kwadraciku
                        applyBlurButton.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ApplyBlurButton_Click(object sender, EventArgs e)
        {
            if (originalImage == null)
            {
                MessageBox.Show("Please load an image first.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (librarySelector.SelectedItem == null)
            {
                MessageBox.Show("Please select a blur library.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Cursor = Cursors.WaitCursor;
            applyBlurButton.Enabled = false;

            try
            {
                ProcessImage(); // Przetwarzanie obrazu
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing image: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                applyBlurButton.Enabled = true;
            }
        }

        private void ProcessImage()
        {
            // Skonwertuj obraz na format 24bppRGB
            Bitmap bitmap = new Bitmap(originalImage);
            int width = bitmap.Width;
            int height = bitmap.Height;
            int blockSize = blurAmountTrackBar.Value; // Przekazanie rozmiaru z suwaka do blockSize

            BitmapData data = null;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Pobierz bajty z obrazu
                Rectangle rect = new Rectangle(0, 0, width, height);
                data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                int bytes = Math.Abs(data.Stride) * height;
                byte[] buffer = new byte[bytes];
                byte[] resultBuffer = new byte[bytes];

                Marshal.Copy(data.Scan0, buffer, 0, bytes);

                IntPtr sourceBuffer = Marshal.AllocHGlobal(bytes);
                IntPtr destinationBuffer = Marshal.AllocHGlobal(bytes);

                try
                {
                    Marshal.Copy(buffer, 0, sourceBuffer, bytes);

                    int threadCount = threadCountTrackBar.Value; // Liczba wątków
                    int rowsPerThread = height / threadCount; // Rozdzielenie wysokości obrazu na wątki

                    List<Thread> threads = new List<Thread>();
                    for (int i = 0; i < threadCount; i++)
                    {
                        int startRow = i * rowsPerThread;
                        int endRow = (i == threadCount - 1) ? height : (i + 1) * rowsPerThread; // Ostatni wątek zajmuje resztę obrazu

                        // Tworzenie nowego wątku, który wykona rozmycie Gaussa na danym fragmencie obrazu
                        int startIndex = startRow * width * 3;
                        int endIndex = endRow * width * 3;
                        Thread thread = new Thread(() =>
                        {
                            if (librarySelector.SelectedItem.ToString() == "ASM Library")
                            {
                                GaussianBlurASM.ApplyBlur(sourceBuffer, destinationBuffer, height, width, startIndex, endIndex, blockSize);
                            }
                            else
                            {
                                GaussianBlur(sourceBuffer, destinationBuffer, height, width, startIndex, endIndex, blockSize);
                            }
                        });

                        threads.Add(thread);
                        thread.Start();
                    }

                    // Czekanie na zakończenie wszystkich wątków
                    foreach (Thread thread in threads)
                    {
                        thread.Join();
                    }

                    Marshal.Copy(destinationBuffer, resultBuffer, 0, bytes);
                    Marshal.Copy(resultBuffer, 0, data.Scan0, bytes);
                }
                finally
                {
                    Marshal.FreeHGlobal(sourceBuffer);
                    Marshal.FreeHGlobal(destinationBuffer);
                }
            }
            finally
            {
                if (data != null)
                    bitmap.UnlockBits(data);
            }

            stopwatch.Stop();

            if (blurredImageBox.Image != null)
                blurredImageBox.Image.Dispose();
            blurredImageBox.Image = bitmap;

            MessageBox.Show($"Processing completed in {stopwatch.ElapsedMilliseconds}ms", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void ThreadCountTrackBar_Scroll(object sender, EventArgs e)
        {
            UpdateLabels();
        }

        private void BlurAmountTrackBar_Scroll(object sender, EventArgs e)
        {
            UpdateLabels();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Czyszczenie zasobów
            if (originalImage != null)
                originalImage.Dispose();
            if (originalImageBox.Image != null)
                originalImageBox.Image.Dispose();
            if (blurredImageBox.Image != null)
                blurredImageBox.Image.Dispose();
        }
    }

    public class GaussianBlurASM
    {
        [DllImport(@"C:\Users\Gamer\source\repos\GaussianBlurASM\x64\Debug\JAAsm.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void ProcessImage(IntPtr InBuffer, IntPtr OutBuffer,
            int height, int width, int start, int endIndex, int blockSize);

        public static void ApplyBlur(IntPtr InBuffer, IntPtr OutBuffer,
            int height, int width, int start, int endIndex, int blockSize)
        {
            ProcessImage(InBuffer, OutBuffer, height, width, start, endIndex, blockSize);
        }
    }
}