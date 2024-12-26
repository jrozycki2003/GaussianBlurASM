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
        
        private Image originalImage;// Przechowuje oryginalny obraz przed przetworzeniem


        [DllImport(@"C:\Users\Gamer\source\repos\GaussianBlurASM\x64\Debug\Blur.dll",
            CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        private static extern void GaussianBlur(
            IntPtr InBuffer,          // Wskaźnik do bufora wejściowego
            IntPtr OutBuffer,         // Wskaźnik do bufora wyjściowego
            int height,               // Wysokość obrazu
            int width,                // Szerokość obrazu
            int start,                // Początkowy indeks dla wątku
            int end,                  // Końcowy indeks dla wątku
            int blockSize);           // Rozmiar okna rozmycia


        [DllImport(@"C:\Users\Gamer\source\repos\GaussianBlurASM\x64\Debug\JAAsm.dll",
            CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        private static extern void GaussianBlurASM(
            IntPtr InBuffer,          // Wskaźnik do bufora wejściowego
            IntPtr OutBuffer,         // Wskaźnik do bufora wyjściowego
            int height,               // Wysokość obrazu
            int width,                // Szerokość obrazu
            int start,                // Początkowy indeks dla wątku
            int end,                  // Końcowy indeks dla wątku
            int blockSize);           // Rozmiar okna rozmycia


        public MainForm()
        {
            InitializeComponent();

            // Dodanie opcji wyboru biblioteki
            librarySelector.Items.Add("C++ Library");
            librarySelector.Items.Add("ASM Library");
            librarySelector.SelectedIndex = 0;

            // Konfiguracja suwaka liczby wątków
            threadCountTrackBar.Minimum = 1;
            threadCountTrackBar.Maximum = 64;
            threadCountTrackBar.Value = Environment.ProcessorCount;  // Domyślnie liczba rdzeni CPU

            // Konfiguracja suwaka intensywności rozmycia
            blurAmountTrackBar.Minimum = 1;
            blurAmountTrackBar.Maximum = 25;
            blurAmountTrackBar.Value = 1;

            UpdateLabels();
        }

        // Aktualizuje etykiety wyświetlające wartości suwaków
        private void UpdateLabels()
        {
            threadCountLabel.Text = $"Thread Count: {threadCountTrackBar.Value}";
            blurAmountLabel.Text = $"Blur Amount: {blurAmountTrackBar.Value}";
        }

        // Obsługa przycisku wczytywania obrazu
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
                        // Zwolnienie zasobów poprzedniego obrazu
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

        // Obsługa przycisku aplikowania rozmycia
        private void ApplyBlurButton_Click(object sender, EventArgs e)
        {
            // Sprawdzenie czy obraz został wczytany
            if (originalImage == null)
            {
                MessageBox.Show("Please load an image first.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Sprawdzenie czy wybrano bibliotekę
            if (librarySelector.SelectedItem == null)
            {
                MessageBox.Show("Please select a blur library.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Zmiana kursora na oczekujący i dezaktywacja przycisku
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
                // Przywrócenie normalnego stanu interfejsu
                Cursor = Cursors.Default;
                applyBlurButton.Enabled = true;
            }
        }

        // Główna metoda aplikująca rozmycie Gaussa
        private void ApplyGaussianBlur()
        {
            // Pobranie parametrów przetwarzania
            bool useAsmLibrary = librarySelector.SelectedItem?.ToString() == "ASM Library";
            int threadCount = threadCountTrackBar.Value;
            int blockSize = blurAmountTrackBar.Value;

            // Start pomiaru czasu
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                using (Bitmap sourceBitmap = new Bitmap(originalImage))//Tworzenie kopii obrazu
                {
                    int width = sourceBitmap.Width;
                    int height = sourceBitmap.Height;

                    // sprawdzenie wymiarów obrazu
                    if (width <= 0 || height <= 0)
                    {
                        MessageBox.Show("Invalid image dimensions.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // sprawdzenie liczby wątków
                    if (threadCount > width || threadCount > height)
                    {
                        MessageBox.Show("Thread count cannot exceed image dimensions.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (Bitmap blurredBitmap = new Bitmap(sourceBitmap))
                    {
                        BitmapData sourceData = null;
                        BitmapData blurredData = null;

                        try
                        {
                            // danie bitmapy w pamięci
                            Rectangle rect = new Rectangle(0, 0, width, height);
                            sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);//LockBits blokuje obszar bitmapy w pamięci
                            //tylko odczyt dla źródła
                            blurredData = blurredBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);//Format24bppRgb - format 24-bitowy (3 bajty na piksel: RGB)
                            //tylko zapis dla wyniku

                            //Obliczanie rozmiaru bufora
                            int stride = sourceData.Stride;//Stride to liczba bajtów w jednym wierszu obrazu
                            //Całkowity rozmiar w bajtach to stride × wysokość
                            int bytes = Math.Abs(stride) * height;

                            // Utworzenie buforów dla danych obrazu
                            byte[] sourceBuffer = new byte[bytes];//Tworzenie buforów na dane źródłowe i wynikowe
                            //Kopiowanie danych z bitmapy do bufora źródłowego
                            byte[] resultBuffer = new byte[bytes];

                            // Kopiowanie danych do bufora
                            Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, bytes);

                            // Alokacja pamięci niezarzadzanej dla cpp i asm
                            IntPtr sourcePtr = Marshal.AllocHGlobal(bytes);//IntPtr to wskaźnik na pamięć niezarządzaną
                            IntPtr resultPtr = Marshal.AllocHGlobal(bytes);

                            try
                            {
                                // Kopiowanie danych do pamięci niemanaged
                                Marshal.Copy(sourceBuffer, 0, sourcePtr, bytes);
                                Marshal.Copy(sourceBuffer, 0, resultPtr, bytes);

                                // Obliczenie zakresów dla wątków
                                int bytesPerThread = bytes / threadCount;//Przygotowanie do przetwarzania wielowątkowego
                                int[] starts = new int[threadCount];
                                int[] ends = new int[threadCount];

                                for (int i = 0; i < threadCount; i++)
                                {
                                    starts[i] = i * bytesPerThread;
                                    ends[i] = (i == threadCount - 1) ? bytes : (i + 1) * bytesPerThread;
                                }
                                // Podział danych na równe części dla każdego wątku
                                // Równoległe przetwarzanie obrazu
                                Parallel.For(0, threadCount, i =>//Uruchomienie równoległego przetwarzania
                                                                 //Każdy wątek przetwarza swoją część obrazu
                                                                 //Wybór między biblioteką ASM a C++
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

                                // Kopiowanie wyników z powrotem i zwalnianie zasobów
                                Marshal.Copy(resultPtr, resultBuffer, 0, bytes);//Kopiowanie wyniku z pamięci niezarządzanej do bufora
                                Marshal.Copy(resultBuffer, 0, blurredData.Scan0, bytes);//Kopiowanie z bufora do bitmapy wynikowej
                            }
                            finally
                            {
                                // Zwolnienie pamięci
                                Marshal.FreeHGlobal(sourcePtr);
                                Marshal.FreeHGlobal(resultPtr);
                            }
                        }
                        finally
                        {
                            // Odblokowanie bitmapki
                            if (sourceData != null) sourceBitmap.UnlockBits(sourceData);
                            if (blurredData != null) blurredBitmap.UnlockBits(blurredData);
                        }

                        stopwatch.Stop();

                        // wyswietlenie wyniku
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (blurredImageBox.Image != null)
                                blurredImageBox.Image.Dispose();
                            blurredImageBox.Image = new Bitmap(blurredBitmap);

                            // Wyświetlenie wyników przetwarzania
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

        // Obsługa zmiany wartości suwaka liczby wątków
        private void ThreadCountTrackBar_Scroll(object sender, EventArgs e)
        {
            UpdateLabels();
        }

        // Obsługa zmiany wartości suwaka intensywności rozmycia
        private void BlurAmountTrackBar_Scroll(object sender, EventArgs e)
        {
            UpdateLabels();
        }

        // posprzątanie zasobów przy zamykaniu programu
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            originalImage?.Dispose();
            originalImageBox.Image?.Dispose();
            blurredImageBox.Image?.Dispose();
        }
    }
}