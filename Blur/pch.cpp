#include "pch.h"
#include <algorithm>
#include <cmath>

#ifdef Blur_EXPORTS
#define Blur_API __declspec(dllexport)
#else
#define Blur_API __declspec(dllimport)
#endif

extern "C" {
    // Funkcja eksportowana z biblioteki DLL używająca __stdcall
    Blur_API void __stdcall GaussianBlur(
        unsigned char* InBuffer,    // Wskaźnik na bufor wejściowy z pikselami obrazu
        unsigned char* OutBuffer,   // Wskaźnik na bufor wyjściowy na przetworzony obraz
        int height,                 // Wysokość obrazu w pikselach
        int width,                  // Szerokość obrazu w pikselach
        int start,                  // Początkowy indeks dla przetwarzania równoległego
        int end,                    // Końcowy indeks dla przetwarzania równoległego
        int blockSize);            // Rozmiar okna rozmycia (większy = silniejsze rozmycie)
}

// Implementacja funkcji rozmycia Gaussa
void __stdcall GaussianBlur(
    unsigned char* InBuffer,
    unsigned char* OutBuffer,
    int height,
    int width,
    int start,
    int end,
    int blockSize)
{
    const int BytesPerPixel = 3;  // Stała określająca liczbę bajtów na piksel (format RGB)

    // Obliczanie rzeczywistych granic przetwarzania w wymiarze Y
    // Konwertujemy indeksy bajtów na indeksy wierszy obrazu
    int startY = start / (width * BytesPerPixel);
    int endY = end / (width * BytesPerPixel);

    // Zabezpieczenie przed wyjściem poza granice obrazu
    startY = std::max(0, startY);          // Upewniamy się, że nie zaczynamy przed obrazem
    endY = std::min(height, endY);         // Upewniamy się, że nie wychodzimy poza obraz

    // Główna pętla przetwarzająca obraz wiersz po wierszu
    for (int y = startY; y < endY; ++y) {
        // Pętla przetwarzająca każdy piksel w wierszu
        for (int x = 0; x < width; ++x) {
            // Sprawdzanie czy piksel jest na brzegu obszaru rozmycia
            // Jeśli tak, kopiujemy go bez zmian aby uniknąć efektów brzegowych
            if (x < blockSize / 2 || x >= width - blockSize / 2 ||
                y < blockSize / 2 || y >= height - blockSize / 2) {

                // Obliczanie pozycji piksela w buforze (każdy piksel to 3 bajty)
                int pixelPos = (y * width + x) * BytesPerPixel;

                // Kopiowanie wartości RGB bez zmian
                OutBuffer[pixelPos] = InBuffer[pixelPos];         // B
                OutBuffer[pixelPos + 1] = InBuffer[pixelPos + 1]; // G
                OutBuffer[pixelPos + 2] = InBuffer[pixelPos + 2]; // R
                continue;  // Przejście do następnego piksela
            }

            // Zmienne do obliczania średniej wartości koloru w oknie rozmycia
            double sumB = 0, sumG = 0, sumR = 0;  // Sumy dla każdej składowej RGB
            int count = 0;  // Licznik pikseli w oknie rozmycia

            // Pętle przechodzące przez okno rozmycia
            // Okno ma wymiary blockSize x blockSize i jest wycentrowane na aktualnym pikselu
            for (int dy = -blockSize / 2; dy <= blockSize / 2; dy++) {
                for (int dx = -blockSize / 2; dx <= blockSize / 2; dx++) {
                    // Obliczanie pozycji piksela w oknie rozmycia
                    int pos = ((y + dy) * width + (x + dx)) * BytesPerPixel;

                    // Sumowanie wartości RGB z sąsiednich pikseli
                    sumB += InBuffer[pos];      // Dodawanie B
                    sumG += InBuffer[pos + 1];  // Dodawanie G
                    sumR += InBuffer[pos + 2];  // Dodawanie R
                    count++;  // Zwiększanie licznika pikseli
                }
            }

            // Obliczanie pozycji aktualnego piksela w buforze wyjściowym
            int pixelPos = (y * width + x) * BytesPerPixel;

            // Zapisywanie uśrednionych wartości RGB do bufora wyjściowego
            OutBuffer[pixelPos] = static_cast<unsigned char>(sumB / count);     // Średnia B
            OutBuffer[pixelPos + 1] = static_cast<unsigned char>(sumG / count); // Średnia G
            OutBuffer[pixelPos + 2] = static_cast<unsigned char>(sumR / count); // Średnia R
        }
    }
}