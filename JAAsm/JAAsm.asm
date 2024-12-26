.code
GaussianBlurASM proc
    ; Opis:
    ; RCX - InBuffer       -> WskaŸnik na bufor wejœciowy (obraz Ÿród³owy)
    ; RDX - OutBuffer      -> WskaŸnik na bufor wyjœciowy (obraz wynikowy)
    ; R8D - height         -> Wysokoœæ obrazu w pikselach
    ; R9D - width          -> Szerokoœæ obrazu w pikselach
    ; [RSP+40] - start     -> Indeks pocz¹tkowy dla przetwarzania
    ; [RSP+48] - end       -> Indeks koñcowy dla przetwarzania
    ; [RSP+56] - blockSize -> Rozmiar okna rozmycia (wielkoœæ filtru)

    push rbp
    mov rbp, rsp
    push rbx
    push rsi
    push rdi
    push r12
    push r13
    push r14
    push r15

    ; Zapisanie parametrów
    mov rsi, rcx               ; RSI = wskaŸnik na bufor wejœciowy
    mov rdi, rdx               ; RDI = wskaŸnik na bufor wyjœciowy
    mov r12d, r8d              ; R12D = wysokoœæ obrazu
    mov r13d, r9d              ; R13D = szerokoœæ obrazu
    
    ; Pobranie jeszcze dodatkowych parametrów ze stosu
    mov r14d, dword ptr [rbp+48]    ; R14D = indeks startowy
    mov r15d, dword ptr [rbp+56]    ; R15D = indeks koñcowy
    mov ebx, dword ptr [rbp+64]     ; EBX = rozmiar okna rozmycia

    ; Obliczenie startY
    ; Obliczenie startY = start / (width * 3) //jest razy 3 bo 3 bytes per pixel
    mov eax, r14d         ; start do EAX
    mov ecx, r13d         ; width do ECX
    imul ecx, 3           ; Pomno¿enie przez 3 (RGB)
    cdq                   ; Przygotowanie do dzielenia
    idiv ecx              ; Dzielenie: startY = start / (width * 3)
    mov r8d, eax          ; Zapisanie startY w r8d

    ; Podobnie dla endY
    ; Obliczenie endY = end / (width * 3)
    mov eax, r15d              ; Ten sam proces dla indeksu koñcowego
    cdq
    idiv ecx
    mov r9d, eax               ; R9D = endY (wiersz koñcowy)

    ; G³ówna pêtla przetwarzania obrazu - iteracja po wierszach Y
    ; G³ówna pêtla po Y
outer_loop:              ; Pêtla po Y
    cmp r8d, r9d         ; Sprawdzenie czy y < endY
    jge ending             ; Jeœli nie, koniec

    xor edx, edx         ; Wyzeruj licznik kolumn (x = 0)
    ;Pêtla wewnêtrzna - iteracja po kolumnach x
inner_loop:              ; Pêtla po X
    cmp edx, r13d        ; Sprawdzenie czy x < width
    jge next_y           ; Jeœli nie, nastêpny wiersz

    ; Obliczanie pozycji piksela
    mov eax, r8d                    ; y
    imul eax, r13d                  ; y * width
    add eax, edx                    ; + x
    imul eax, 3                     ; * 3 (RGB)
    
    ; Sprawdzenie czy jesteœmy na brzegu
    mov r10d, ebx                   ; blockSize
    shr r10d, 1                     ; blockSize / 2
    
 ; sprawdzanie czy piksel jest na brzegu
    cmp edx, r10d                   ; x < blockSize/2
    jl copy_pixel
    
    mov r11d, r13d            ; Sprawdzenie prawej krawêdzi
    sub r11d, r10d            ; width - promieñ
    cmp edx, r11d             ; Czy x >= width - promieñ?
    ; x >= width - blockSize/2
    jge copy_pixel
                                    ; podobnie dla Y:
    cmp r8d, r10d                   ; y < blockSize/2
    ; Czy y < promieñ?
    jl copy_pixel
    
    mov r11d, r12d            ; Sprawdzenie dolnej krawêdzi
    sub r11d, r10d            ; height - promieñ
    cmp r8d, r11d             ; y >= height - blockSize/2
    ; Czy y >= height - promieñ?
    jge copy_pixel

    ; Inicjalizacja zmiennych dla obliczania rozmycia
    ; Inicjalizacja sum kolorów
    ; U¿ywanie instrukcji wektorowych pxor
    pxor xmm0, xmm0                 ; Wyzerowanie sum kolorów, sum R
    pxor xmm1, xmm1                 ; sum G
    pxor xmm2, xmm2                 ; sum B
    xor r11d, r11d                  ; Licznik pikseli count = 0

    ; Pêtla dla okna rozmycia
    ; Inicjalizacja licznika dy
    mov r14d, r10d                  ; half_block = blockSize/2
    ; dy = -promieñ
    neg r14d                        ; -half_block
                                    ; Pêtle po oknie rozmycia
blur_y:                             ; pêtla po y w oknie rozmycia
    cmp r14d, r10d           ; Czy dy <= promieñ?
    jg blur_end             ; Jeœli nie, koñczymy rozmywanie
    
    mov r15d, r10d           ; Inicjalizacja licznika dx
    ; dx = -promieñ
    neg r15d                 ; -half_block dla x

blur_x:                             ; pêtla po x w oknie rozmycia tak samo
    cmp r15d, r10d
    jg next_blur_y

    ; Obliczanie pozycji piksela w oknie: pos = ((y+dy) * width + (x+dx)) * 3
    mov eax, r8d                    ; y
    add eax, r14d                   ; y + dy
    imul eax, r13d                  ; * width
    add eax, edx                    ; + x
    add eax, r15d                   ; + dx
    imul eax, 3                     ; * 3 (RGB)

 ; Dodawanie wartoœci kolorów
 ; Znowu u¿ywanie instrukcji wektorowych cvtsi2ss, addss
    movzx ecx, byte ptr [rsi + rax]     ; Pobranie sk³adowej Blue
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm2, xmm3                    ; Dodanie do sumy
                                        
    movzx ecx, byte ptr [rsi + rax + 1] ; Pobranie sk³adowej Green
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm1, xmm3                    ; Dodanie do sumy
    
    movzx ecx, byte ptr [rsi + rax + 2] ; Pobranie sk³adowej Red
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm0, xmm3                    ; Dodanie do sumy
    
    inc r11d                            ; inkrementacja licznika pikseli, count++
    
    inc r15d                            ; nastêpna kolumna w oknie
    jmp blur_x

next_blur_y:
    inc r14d                 ; Nastêpny wiersz w oknie
    mov r15d, r10d
    neg r15d                 ; Reset dx
    jmp blur_y

    ; Obliczanie œrednich wartoœci kolorów
blur_end:
    ; Obliczenie œredniej
    cvtsi2ss xmm3, r11d     ; Konwersja licznika na float
    ; œrednie R G i B
    divss xmm0, xmm3        ; Dzielenie R/count
    divss xmm1, xmm3        ; Dzielenie G/count
    divss xmm2, xmm3        ; Dzielenie B/count

    ; Obliczanie pozycji docelowej w buforze wyjœciowym
    mov eax, r8d                   ; y
    imul eax, r13d                 ; * width
    add eax, edx                   ; + x
    imul eax, 3                    ; * 3 (RGB)

    ; Zapisywanie wyników wartoœci kolorów
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
    inc edx                         ; nastêpna kolumna, nastêpny x
    jmp inner_loop

next_y:
    inc r8d                         ; nastêpny wiersz, nastêpny y
    jmp outer_loop

    ; koncówka - przywracanie stanu rejestrów i powrót
ending:
    pop r15                         ; Przywrócenie rejestrów
    pop r14
    pop r13
    pop r12
    pop rdi
    pop rsi
    pop rbx
    pop rbp
    ret                             ; Powrót z funkcji, koniec rozmycia endp
GaussianBlurASM endp

end