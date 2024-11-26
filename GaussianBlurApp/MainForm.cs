using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
                        originalImage = Image.FromFile(FileText.FileName);
                        originalImageBox.Image = new Bitmap(originalImage);
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
                ProcessImage();
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
            // Bezpieczne pobranie wartości z głównego wątku interfejsu użytkownika
            bool useAsmLibrary = false;
            int threadCount = 1;
            int blockSize = 1;

            this.Invoke((MethodInvoker)delegate
            {
                useAsmLibrary = librarySelector.SelectedItem?.ToString() == "ASM Library";
                threadCount = threadCountTrackBar.Value;
                blockSize = blurAmountTrackBar.Value;
            });

            Bitmap bitmap = new Bitmap(originalImage);
            int width = bitmap.Width;
            int height = bitmap.Height;

            Console.WriteLine($"Image dimensions: {width}x{height}, Block size: {blockSize}, Threads: {threadCount}");

            BitmapData data = null;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                Rectangle rect = new Rectangle(0, 0, width, height);
                data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                int stride = data.Stride;
                int bytes = Math.Abs(stride) * height;

                byte[] buffer = new byte[bytes];
                byte[] resultBuffer = new byte[bytes];

                Marshal.Copy(data.Scan0, buffer, 0, bytes);

                IntPtr sourceBuffer = Marshal.AllocHGlobal(bytes);
                IntPtr destinationBuffer = Marshal.AllocHGlobal(bytes);

                try
                {
                    Marshal.Copy(buffer, 0, sourceBuffer, bytes);
                    Marshal.Copy(buffer, 0, destinationBuffer, bytes);

                    // Podział pracy na wątki
                    Parallel.For(0, threadCount, threadIndex =>
                    {
                        int rowsPerThread = height / threadCount;
                        int startRow = threadIndex * rowsPerThread;
                        int endRow = (threadIndex == threadCount - 1) ? height : (threadIndex + 1) * rowsPerThread;

                        int startIndex = startRow * stride;
                        int endIndex = endRow * stride;

                        try
                        {
                            if (useAsmLibrary)
                            {
                                GaussianBlurASM.ApplyBlurThreaded(
                                    sourceBuffer,
                                    destinationBuffer,
                                    width,
                                    height,
                                    startIndex,
                                    endIndex,
                                    blockSize  // Dodaj ten parametr
                                );
                            }
                            else
                            {
                                GaussianBlur(
                                    sourceBuffer,
                                    destinationBuffer,
                                    height,
                                    width,
                                    startIndex,
                                    endIndex,
                                    blockSize
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in thread {threadIndex}: {ex.Message}");
                        }
                    });

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

            // Użyj BeginInvoke, aby nie blokować wątku
            this.BeginInvoke((MethodInvoker)delegate
            {
                if (blurredImageBox.Image != null)
                    blurredImageBox.Image.Dispose();
                blurredImageBox.Image = bitmap;

                MessageBox.Show($"Processing completed in {stopwatch.ElapsedMilliseconds}ms",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
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
        public static extern void ApplyBlurThreaded(
            IntPtr inputBuffer,
            IntPtr outputBuffer,
            int width,
            int height,
            int startIndex,
            int endIndex,
            int blockSize
        );

        // Stałe dla operacji SIMD
        public const int BytesPerPixel = 3;      // Format RGB
        public const int SIMDAlignment = 64;     // Wyrównanie dla operacji SIMD
        public const int PixelsPerBlock = 8;     // Liczba pikseli przetwarzanych w jednym bloku SIMD
    }
}