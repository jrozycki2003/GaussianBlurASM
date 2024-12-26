using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GaussianBlur
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

        [DllImport(@"C:\Users\Gamer\source\repos\GaussianBlurASM\x64\Debug\JAAsm.dll",
            CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        private static extern void GaussianBlurASM(
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
            blurAmountTrackBar.Maximum = 25;
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
                FileText.Filter = "Image Files|.jpg;.jpeg;*.png";
                FileText.Title = "Select an Image File";

                if (FileText.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        originalImage?.Dispose();
                        originalImage = Image.FromFile(FileText.FileName);
                        originalImageBox.Image?.Dispose();
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
                ApplyGaussianBlur();
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

        private void ApplyGaussianBlur()
        {
            if (originalImage == null)
            {
                MessageBox.Show("Please load image.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool useAsmLibrary = librarySelector.SelectedItem?.ToString() == "ASM Library";
            int threadCount = threadCountTrackBar.Value;
            int blockSize = blurAmountTrackBar.Value;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                using (Bitmap sourceBitmap = new Bitmap(originalImage))
                {
                    int width = sourceBitmap.Width;
                    int height = sourceBitmap.Height;

                    // Obsługa błędów
                    if (width <= 0 || height <= 0)
                    {
                        MessageBox.Show("Invalid image dimensions.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (threadCount > width || threadCount > height)
                    {
                        MessageBox.Show("Thread count cannot exceed image dimensions.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Debug.WriteLine($"Processing with: {(useAsmLibrary ? "ASM" : "C++")} Library");
                    Debug.WriteLine($"Image dimensions: {width}x{height}, Block size: {blockSize}, Threads: {threadCount}");

                    using (Bitmap blurredBitmap = new Bitmap(sourceBitmap))
                    {
                        BitmapData sourceData = null;
                        BitmapData blurredData = null;

                        try
                        {
                            Rectangle rect = new Rectangle(0, 0, width, height);
                            sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                            blurredData = blurredBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                            int stride = sourceData.Stride;
                            int bytes = Math.Abs(stride) * height;

                            byte[] sourceBuffer = new byte[bytes];
                            byte[] resultBuffer = new byte[bytes];

                            Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, bytes);

                            IntPtr sourcePtr = Marshal.AllocHGlobal(bytes);
                            IntPtr resultPtr = Marshal.AllocHGlobal(bytes);

                            try
                            {
                                Marshal.Copy(sourceBuffer, 0, sourcePtr, bytes);
                                Marshal.Copy(sourceBuffer, 0, resultPtr, bytes);

                                int bytesPerThread = bytes / threadCount;
                                int[] starts = new int[threadCount];
                                int[] ends = new int[threadCount];

                                for (int i = 0; i < threadCount; i++)
                                {
                                    starts[i] = i * bytesPerThread;
                                    ends[i] = (i == threadCount - 1) ? bytes : (i + 1) * bytesPerThread;
                                }

                                Parallel.For(0, threadCount, i =>
                                {
                                    if (useAsmLibrary)
                                    {
                                        GaussianBlurASM(sourcePtr, resultPtr, height, width, starts[i], ends[i], blockSize);
                                    }
                                    else
                                    {
                                        GaussianBlur(sourcePtr, resultPtr, height, width, starts[i], ends[i], blockSize);
                                    }
                                });

                                Marshal.Copy(resultPtr, resultBuffer, 0, bytes);
                                Marshal.Copy(resultBuffer, 0, blurredData.Scan0, bytes);
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(sourcePtr);
                                Marshal.FreeHGlobal(resultPtr);
                            }
                        }
                        finally
                        {
                            if (sourceData != null) sourceBitmap.UnlockBits(sourceData);
                            if (blurredData != null) blurredBitmap.UnlockBits(blurredData);
                        }

                        stopwatch.Stop();

                        this.Invoke((MethodInvoker)delegate
                        {
                            if (blurredImageBox.Image != null)
                                blurredImageBox.Image.Dispose();
                            blurredImageBox.Image = new Bitmap(blurredBitmap);

                            MessageBox.Show($"Processing completed in {stopwatch.ElapsedMilliseconds} ms\n" +
                                             $"Library: {(useAsmLibrary ? "ASM" : "C++")}\n" +
                                             $"Threads: {threadCount}\n" +
                                             $"Blur Size: {blockSize}",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            originalImage?.Dispose();
            originalImageBox.Image?.Dispose();
            blurredImageBox.Image?.Dispose();
        }
    }
}