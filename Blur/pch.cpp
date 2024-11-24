#include "pch.h"

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
    int index = start;  // Zacznij od indeksu startowego

    for (int y = start / (width * 3); y < end / (width * 3); ++y) {
        for (int x = (start / 3) % width; x < width; ++x) {

            if (x >= blockSize / 2 && x < width - blockSize / 2 &&
                y >= blockSize / 2 && y < height - blockSize / 2) {

                double sumR = 0, sumG = 0, sumB = 0;
                int count = 0;

                // Obliczanie średniej z sąsiednich pikseli w oknie rozmycia
                for (int dy = -blockSize / 2; dy <= blockSize / 2; dy++) {
                    for (int dx = -blockSize / 2; dx <= blockSize / 2; dx++) {
                        int pos = ((y + dy) * width + (x + dx)) * 3;
                        sumB += InBuffer[pos];
                        sumG += InBuffer[pos + 1];
                        sumR += InBuffer[pos + 2];
                        count++;
                    }
                }

                int pixelPos = (y * width + x) * 3;
                OutBuffer[pixelPos] = static_cast<unsigned char>(sumB / count);
                OutBuffer[pixelPos + 1] = static_cast<unsigned char>(sumG / count);
                OutBuffer[pixelPos + 2] = static_cast<unsigned char>(sumR / count);
            }
            else {
                // Kopiowanie pikseli, jeśli znajdują się na krawędzi
                int pixelPos = (y * width + x) * 3;
                OutBuffer[pixelPos] = InBuffer[pixelPos];
                OutBuffer[pixelPos + 1] = InBuffer[pixelPos + 1];
                OutBuffer[pixelPos + 2] = InBuffer[pixelPos + 2];
            }
        }
    }
}
