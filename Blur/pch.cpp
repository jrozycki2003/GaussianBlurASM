#include "pch.h"
#include <emmintrin.h> // Wykorzystanie instrukcji wektorowych 

#ifdef Blur_EXPORTS
#define Blur_API __declspec(dllexport)
#else
#define Blur_API __declspec(dllimport)
#endif

// Deklaracja funkcji eksportowanej z DLL
extern "C" {
    Blur_API void __stdcall GaussianBlur(
        unsigned char* InBuffer, // Bufor wejściowy (obraz wejściowy)
        unsigned char* OutBuffer, // Bufor wyjściowy (obraz wynikowy)
        int height,  // Wysokość obrazu
        int width,   // Szerokość obrazu
        int start,   // Indeks początkowy do przetwarzania
        int end,     // Indeks końcowy do przetwarzania
        int blockSize // Rozmiar bloku do rozmycia
    );
}

// Funkcja realizująca algorytm rozmycia Gaussa
void __stdcall GaussianBlur(
    unsigned char* InBuffer,
    unsigned char* OutBuffer,
    int height,
    int width,
    int start,
    int end,
    int blockSize)
{
    int index = 0;

    // Obliczanie połowy rozmiaru bloku, używane w pętli
    int halfBlockSize = blockSize / 2;

    // Pętla przetwarzająca piksele obrazu w zadanym zakresie (od start do end)
    for (int i = start; i < end; i += 3) {
        int x = (i / 3) % width;    // Obliczanie współrzędnej x piksela
        int y = (i / 3) / width;   // Obliczanie współrzędnej y piksela

        // Sprawdzenie, czy piksel znajduje się w obrębie obrazu, aby nie wyjść poza jego granice
        if (x >= halfBlockSize && x < width - halfBlockSize &&
            y >= halfBlockSize && y < height - halfBlockSize) {

            // Inicjalizacja zmiennych do sumowania wartości kanałów R, G, B
            __m128d sumR = _mm_setzero_pd(); // Suma dla R
            __m128d sumG = _mm_setzero_pd(); // Suma dla G
            __m128d sumB = _mm_setzero_pd(); // Suma dla B
            int count = 0; // Licznik pikseli w obrębie bloku

            // Pętla przetwarzająca piksele w obrębie bloku wokół aktualnego piksela
            for (int dy = -halfBlockSize; dy <= halfBlockSize; dy++) {
                for (int dx = -halfBlockSize; dx <= halfBlockSize; dx++) {
                    int pos = ((y + dy) * width + (x + dx)) * 3; // Obliczanie pozycji piksela w buforze

                    // Ładowanie wartości piksela do rejestru SIMD
                    __m128i pixel = _mm_loadu_si128(reinterpret_cast<const __m128i*>(&InBuffer[pos]));

                    // Rozpakowanie wartości piksela do poszczególnych kanałów (R, G, B)
                    __m128i lo = _mm_unpacklo_epi8(pixel, _mm_setzero_si128()); // Pierwsze 8 bajtów (RGB)
                    __m128i hi = _mm_unpackhi_epi8(pixel, _mm_setzero_si128()); // Kolejne 8 bajtów (RGB)

                    // Oddzielanie kanałów: R, G, B z rozpakowanych wartości
                    __m128d b = _mm_cvtepi32_pd(_mm_unpacklo_epi16(lo, _mm_setzero_si128())); // B
                    __m128d g = _mm_cvtepi32_pd(_mm_unpackhi_epi16(lo, _mm_setzero_si128())); // G
                    __m128d r = _mm_cvtepi32_pd(_mm_unpacklo_epi16(hi, _mm_setzero_si128())); // R

                    // Dodawanie wartości do sum dla każdego kanału
                    sumB = _mm_add_pd(sumB, b); // Sumowanie kanału B
                    sumG = _mm_add_pd(sumG, g); // Sumowanie kanału G
                    sumR = _mm_add_pd(sumR, r); // Sumowanie kanału R
                    count++; // Zwiększenie licznika pikseli
                }
            }

            // Obliczanie średnich wartości dla każdego kanału (rozmycie)
            sumB = _mm_div_pd(sumB, _mm_cvtepi32_pd(_mm_set1_epi32(count))); // Średnia dla kanału B
            sumG = _mm_div_pd(sumG, _mm_cvtepi32_pd(_mm_set1_epi32(count))); // Średnia dla kanału G
            sumR = _mm_div_pd(sumR, _mm_cvtepi32_pd(_mm_set1_epi32(count))); // Średnia dla kanału R

            // Zapisywanie obliczonych średnich wartości z powrotem do bufora wyjściowego
            OutBuffer[index] = static_cast<unsigned char>(_mm_cvtsd_f64(sumB)); // Przechowywanie wartości B
            OutBuffer[index + 1] = static_cast<unsigned char>(_mm_cvtsd_f64(sumG)); // Przechowywanie wartości G
            OutBuffer[index + 2] = static_cast<unsigned char>(_mm_cvtsd_f64(sumR)); // Przechowywanie wartości R
        }
        else {
            // Jeśli piksel jest poza zakresem, kopiowanie wartości bez zmian
            OutBuffer[index] = InBuffer[i];          // Kopiowanie wartości R
            OutBuffer[index + 1] = InBuffer[i + 1];  // Kopiowanie wartości G
            OutBuffer[index + 2] = InBuffer[i + 2];  // Kopiowanie wartości B
        }
        index += 3; // Przejście do kolejnego piksela (każdy piksel ma 3 składniki R,G,B, dlatego indeks+3)
    }
}
