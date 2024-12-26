.code
GaussianBlurASM proc
    ; RCX - InBuffer       Wska�nik na bufor wej�ciowy
    ; RDX - OutBuffer      Wska�nik na bufor wyj�ciowy
    ; R8D - height         Wysoko�� obrazu
    ; R9D - width          Szeroko�� obrazu
    ; [RSP+40] - start     Indeks startowy
    ; [RSP+48] - end       Indeks ko�cowy
    ; [RSP+56] - blockSize Rozmiar okna rozmycia

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
    mov rsi, rcx          ; InBuffer -> RSI
    mov rdi, rdx          ; OutBuffer -> RDI
    mov r12d, r8d         ; height -> R12D
    mov r13d, r9d         ; width -> R13D
    
    ; Pobranie parametr�w ze stosu
    mov r14d, dword ptr [rbp+48]    ; start -> R14D
    mov r15d, dword ptr [rbp+56]    ; end -> R15D
    mov ebx, dword ptr [rbp+64]     ; blockSize -> EBX

    ; Obliczenie startY
    mov eax, r14d         ; start do EAX
    mov ecx, r13d         ; width do ECX
    imul ecx, 3           ; Pomno�enie przez 3 (RGB)
    cdq                   ; Przygotowanie do dzielenia
    idiv ecx              ; Dzielenie: startY = start / (width * 3)
    mov r8d, eax          ; Zapisanie startY

    ; Podobnie dla endY
    mov eax, r15d        ; Obliczenie endY
    cdq
    idiv ecx
    mov r9d, eax

    ; G��wna p�tla po Y
outer_loop:              ; P�tla po Y
    cmp r8d, r9d         ; Sprawdzenie czy y < endY
    jge done             ; Je�li nie, koniec

    xor edx, edx         ; x = 0

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
    
 ; Seria por�wna� sprawdzaj�cych czy piksel jest na brzegu
    cmp edx, r10d                   ; x < blockSize/2
    jl copy_pixel
    
    mov r11d, r13d
    sub r11d, r10d
    cmp edx, r11d                   ; x >= width - blockSize/2
    jge copy_pixel
                                    ; podobnie dla Y:
    cmp r8d, r10d                   ; y < blockSize/2
    jl copy_pixel
    
    mov r11d, r12d
    sub r11d, r10d
    cmp r8d, r11d                   ; y >= height - blockSize/2
    jge copy_pixel

    ; Inicjalizacja sum kolor�w
    pxor xmm0, xmm0                 ; sum R
    pxor xmm1, xmm1                 ; sum G
    pxor xmm2, xmm2                 ; sum B
    xor r11d, r11d                  ; Licznik pikseli count = 0

    ; P�tla dla okna rozmycia
    mov r14d, r10d                  ; half_block = blockSize/2
    neg r14d                        ; -half_block
                                    ; P�tle po oknie rozmycia
blur_y:                             ; p�tla po y w oknie
    cmp r14d, r10d
    jg blur_done
    
    mov r15d, r10d
    neg r15d                        ; -half_block dla x

blur_x:                             ; p�tla po x w oknie
    cmp r15d, r10d
    jg next_blur_y

    ; Oblicz pozycj� dla piksela w oknie
    mov eax, r8d                    ; y
    add eax, r14d                   ; y + dy
    imul eax, r13d                  ; * width
    add eax, edx                    ; + x
    add eax, r15d                   ; + dx
    imul eax, 3         ; * 3 (RGB)

 ; Dodawanie warto�ci kolor�w
    movzx ecx, byte ptr [rsi + rax]     ; Pobranie sk�adowej Blue
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm2, xmm3                    ; Dodanie do sumy
                                        
    movzx ecx, byte ptr [rsi + rax + 1] ; Pobranie sk�adowej Green
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm1, xmm3                    ; Dodanie do sumy
    
    movzx ecx, byte ptr [rsi + rax + 2] ; Pobranie sk�adowej Red
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm0, xmm3                    ; Dodanie do sumy
    
    inc r11d                            ; count++
    
    inc r15d
    jmp blur_x

next_blur_y:
    inc r14d
    mov r15d, r10d
    neg r15d
    jmp blur_y

blur_done:
    ; Obliczenie �redniej
    cvtsi2ss xmm3, r11d     ; Konwersja licznika na float
    divss xmm0, xmm3        ; Dzielenie R/count
    divss xmm1, xmm3        ; Dzielenie G/count
    divss xmm2, xmm3        ; Dzielenie B/count

    ; Oblicz pozycj� docelow�
    mov eax, r8d                   ; y
    imul eax, r13d                 ; * width
    add eax, edx                   ; + x
    imul eax, 3        ; * 3 (RGB)

    ; Zapisz wynik
    cvttss2si ecx, xmm2
    mov byte ptr [rdi + rax], cl     ; Blue
    cvttss2si ecx, xmm1
    mov byte ptr [rdi + rax + 1], cl ; Green
    cvttss2si ecx, xmm0
    mov byte ptr [rdi + rax + 2], cl ; Red
    
    jmp next_pixel

copy_pixel:
    ; Skopiuj piksel bez zmian
    mov cl, byte ptr [rsi + rax]        ; Kopiowanie B
    mov byte ptr [rdi + rax], cl

    mov cl, byte ptr [rsi + rax + 1]    ; Kopiowanie G
    mov byte ptr [rdi + rax + 1], cl

    mov cl, byte ptr [rsi + rax + 2]    ; Kopiowanie R
    mov byte ptr [rdi + rax + 2], cl

next_pixel:
    inc edx                         ; nast�pny x
    jmp inner_loop

next_y:
    inc r8d                         ; nast�pny y
    jmp outer_loop

done:
    pop r15                         ; Przywr�cenie zachowanych rejestr�w
    pop r14
    pop r13
    pop r12
    pop rdi
    pop rsi
    pop rbx
    pop rbp
    ret                             ; Powr�t z funkcji
GaussianBlurASM endp

end