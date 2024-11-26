#include "pch.h"
#include <algorithm>
#include <cmath>

#ifdef Blur_EXPORTS
#define Blur_API __declspec(dllexport)
#else
#define Blur_API __declspec(dllimport)
#endif

extern "C" {
    Blur_API void __stdcall GaussianBlur(
        unsigned char* InBuffer,
        unsigned char* OutBuffer,
        int height,
        int width,
        int start,
        int end,
        int blockSize);
}

void __stdcall GaussianBlur(
    unsigned char* InBuffer,
    unsigned char* OutBuffer,
    int height,
    int width,
    int start,
    int end,
    int blockSize)
{
    const int BytesPerPixel = 3;  // RGB

    // Oblicz rzeczywiste granice przetwarzania
    int startY = start / (width * BytesPerPixel);
    int endY = end / (width * BytesPerPixel);

    // Zabezpieczenie przed wyjściem poza granice
    startY = std::max(0, startY);
    endY = std::min(height, endY);

    for (int y = startY; y < endY; ++y) {
        for (int x = 0; x < width; ++x) {
            // Jeśli piksel jest blisko krawędzi, kopiuj bez rozmycia
            if (x < blockSize / 2 || x >= width - blockSize / 2 ||
                y < blockSize / 2 || y >= height - blockSize / 2) {

                int pixelPos = (y * width + x) * BytesPerPixel;
                OutBuffer[pixelPos] = InBuffer[pixelPos];
                OutBuffer[pixelPos + 1] = InBuffer[pixelPos + 1];
                OutBuffer[pixelPos + 2] = InBuffer[pixelPos + 2];
                continue;
            }

            double sumB = 0, sumG = 0, sumR = 0;
            int count = 0;

            // Rozmycie Gaussa w określonym oknie
            for (int dy = -blockSize / 2; dy <= blockSize / 2; dy++) {
                for (int dx = -blockSize / 2; dx <= blockSize / 2; dx++) {
                    int pos = ((y + dy) * width + (x + dx)) * BytesPerPixel;
                    sumB += InBuffer[pos];
                    sumG += InBuffer[pos + 1];
                    sumR += InBuffer[pos + 2];
                    count++;
                }
            }

            // Zapis uśrednionych wartości
            int pixelPos = (y * width + x) * BytesPerPixel;
            OutBuffer[pixelPos] = static_cast<unsigned char>(sumB / count);
            OutBuffer[pixelPos + 1] = static_cast<unsigned char>(sumG / count);
            OutBuffer[pixelPos + 2] = static_cast<unsigned char>(sumR / count);
        }
    }
}