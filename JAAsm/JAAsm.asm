.code
GaussianBlurASM proc
    ; Opis:
    ; RCX - InBuffer       -> Wska�nik na bufor wej�ciowy (obraz �r�d�owy)
    ; RDX - OutBuffer      -> Wska�nik na bufor wyj�ciowy (obraz wynikowy)
    ; R8D - height         -> Wysoko�� obrazu w pikselach
    ; R9D - width          -> Szeroko�� obrazu w pikselach
    ; [RSP+40] - start     -> Indeks pocz�tkowy dla przetwarzania
    ; [RSP+48] - end       -> Indeks ko�cowy dla przetwarzania
    ; [RSP+56] - blockSize -> Rozmiar okna rozmycia (wielko�� filtru)

    push rbp
    mov rbp, rsp
    push rbx
    push rsi
    push rdi
    push r12
    push r13
    push r14
    push r15

    ; Zapisanie parametr�w
    mov rsi, rcx               ; RSI = wska�nik na bufor wej�ciowy
    mov rdi, rdx               ; RDI = wska�nik na bufor wyj�ciowy
    mov r12d, r8d              ; R12D = wysoko�� obrazu
    mov r13d, r9d              ; R13D = szeroko�� obrazu
    
    ; Pobranie jeszcze dodatkowych parametr�w ze stosu
    mov r14d, dword ptr [rbp+48]    ; R14D = indeks startowy
    mov r15d, dword ptr [rbp+56]    ; R15D = indeks ko�cowy
    mov ebx, dword ptr [rbp+64]     ; EBX = rozmiar okna rozmycia

    ; Obliczenie startY
    ; Obliczenie startY = start / (width * 3) //jest razy 3 bo 3 bytes per pixel
    mov eax, r14d         ; start do EAX
    mov ecx, r13d         ; width do ECX
    imul ecx, 3           ; Pomno�enie przez 3 (RGB)
    cdq                   ; Przygotowanie do dzielenia
    idiv ecx              ; Dzielenie: startY = start / (width * 3)
    mov r8d, eax          ; Zapisanie startY w r8d

    ; Podobnie dla endY
    ; Obliczenie endY = end / (width * 3)
    mov eax, r15d              ; Ten sam proces dla indeksu ko�cowego
    cdq
    idiv ecx
    mov r9d, eax               ; R9D = endY (wiersz ko�cowy)

    ; G��wna p�tla przetwarzania obrazu - iteracja po wierszach Y
    ; G��wna p�tla po Y
outer_loop:              ; P�tla po Y
    cmp r8d, r9d         ; Sprawdzenie czy y < endY
    jge ending             ; Je�li nie, koniec

    xor edx, edx         ; Wyzeruj licznik kolumn (x = 0)
    ;P�tla wewn�trzna - iteracja po kolumnach x
inner_loop:              ; P�tla po X
    cmp edx, r13d        ; Sprawdzenie czy x < width
    jge next_y           ; Je�li nie, nast�pny wiersz

    ; Obliczanie pozycji piksela
    mov eax, r8d                    ; y
    imul eax, r13d                  ; y * width
    add eax, edx                    ; + x
    imul eax, 3                     ; * 3 (RGB)
    
    ; Sprawdzenie czy jeste�my na brzegu
    mov r10d, ebx                   ; blockSize
    shr r10d, 1                     ; blockSize / 2
    
 ; sprawdzanie czy piksel jest na brzegu
    cmp edx, r10d                   ; x < blockSize/2
    jl copy_pixel
    
    mov r11d, r13d            ; Sprawdzenie prawej kraw�dzi
    sub r11d, r10d            ; width - promie�
    cmp edx, r11d             ; Czy x >= width - promie�?
    ; x >= width - blockSize/2
    jge copy_pixel
                                    ; podobnie dla Y:
    cmp r8d, r10d                   ; y < blockSize/2
    ; Czy y < promie�?
    jl copy_pixel
    
    mov r11d, r12d            ; Sprawdzenie dolnej kraw�dzi
    sub r11d, r10d            ; height - promie�
    cmp r8d, r11d             ; y >= height - blockSize/2
    ; Czy y >= height - promie�?
    jge copy_pixel

    ; Inicjalizacja zmiennych dla obliczania rozmycia
    ; Inicjalizacja sum kolor�w
    ; U�ywanie instrukcji wektorowych pxor
    pxor xmm0, xmm0                 ; Wyzerowanie sum kolor�w, sum R
    pxor xmm1, xmm1                 ; sum G
    pxor xmm2, xmm2                 ; sum B
    xor r11d, r11d                  ; Licznik pikseli count = 0

    ; P�tla dla okna rozmycia
    ; Inicjalizacja licznika dy
    mov r14d, r10d                  ; half_block = blockSize/2
    ; dy = -promie�
    neg r14d                        ; -half_block
                                    ; P�tle po oknie rozmycia
blur_y:                             ; p�tla po y w oknie rozmycia
    cmp r14d, r10d           ; Czy dy <= promie�?
    jg blur_end             ; Je�li nie, ko�czymy rozmywanie
    
    mov r15d, r10d           ; Inicjalizacja licznika dx
    ; dx = -promie�
    neg r15d                 ; -half_block dla x

blur_x:                             ; p�tla po x w oknie rozmycia tak samo
    cmp r15d, r10d
    jg next_blur_y

    ; Obliczanie pozycji piksela w oknie: pos = ((y+dy) * width + (x+dx)) * 3
    mov eax, r8d                    ; y
    add eax, r14d                   ; y + dy
    imul eax, r13d                  ; * width
    add eax, edx                    ; + x
    add eax, r15d                   ; + dx
    imul eax, 3                     ; * 3 (RGB)

 ; Dodawanie warto�ci kolor�w
 ; Znowu u�ywanie instrukcji wektorowych cvtsi2ss, addss
    movzx ecx, byte ptr [rsi + rax]     ; Pobranie sk�adowej Blue
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm2, xmm3                    ; Dodanie do sumy
                                        
    movzx ecx, byte ptr [rsi + rax + 1] ; Pobranie sk�adowej Green
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm1, xmm3                    ; Dodanie do sumy
    
    movzx ecx, byte ptr [rsi + rax + 2] ; Pobranie sk�adowej Red
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm0, xmm3                    ; Dodanie do sumy
    
    inc r11d                            ; inkrementacja licznika pikseli, count++
    
    inc r15d                            ; nast�pna kolumna w oknie
    jmp blur_x

next_blur_y:
    inc r14d                 ; Nast�pny wiersz w oknie
    mov r15d, r10d
    neg r15d                 ; Reset dx
    jmp blur_y

    ; Obliczanie �rednich warto�ci kolor�w
blur_end:
    ; Obliczenie �redniej
    cvtsi2ss xmm3, r11d     ; Konwersja licznika na float
    ; �rednie R G i B
    divss xmm0, xmm3        ; Dzielenie R/count
    divss xmm1, xmm3        ; Dzielenie G/count
    divss xmm2, xmm3        ; Dzielenie B/count

    ; Obliczanie pozycji docelowej w buforze wyj�ciowym
    mov eax, r8d                   ; y
    imul eax, r13d                 ; * width
    add eax, edx                   ; + x
    imul eax, 3                    ; * 3 (RGB)

    ; Zapisywanie wynik�w warto�ci kolor�w
    cvttss2si ecx, xmm2
    mov byte ptr [rdi + rax], cl     ; Blue
    cvttss2si ecx, xmm1
    mov byte ptr [rdi + rax + 1], cl ; Green
    cvttss2si ecx, xmm0
    mov byte ptr [rdi + rax + 2], cl ; Red
    
    jmp next_pixel

    ; Kopiowanie piksela brzegowego bez zmian
copy_pixel:
    ; Skopiuj piksel bez zmian
    mov cl, byte ptr [rsi + rax]        ; Kopiowanie B
    mov byte ptr [rdi + rax], cl

    mov cl, byte ptr [rsi + rax + 1]    ; Kopiowanie G
    mov byte ptr [rdi + rax + 1], cl

    mov cl, byte ptr [rsi + rax + 2]    ; Kopiowanie R
    mov byte ptr [rdi + rax + 2], cl

next_pixel:
    inc edx                         ; nast�pna kolumna, nast�pny x
    jmp inner_loop

next_y:
    inc r8d                         ; nast�pny wiersz, nast�pny y
    jmp outer_loop

    ; konc�wka - przywracanie stanu rejestr�w i powr�t
ending:
    pop r15                         ; Przywr�cenie rejestr�w
    pop r14
    pop r13
    pop r12
    pop rdi
    pop rsi
    pop rbx
    pop rbp
    ret                             ; Powr�t z funkcji, koniec rozmycia endp
GaussianBlurASM endp

end