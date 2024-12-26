.code
GaussianBlurASM proc
    ; RCX - InBuffer       WskaŸnik na bufor wejœciowy
    ; RDX - OutBuffer      WskaŸnik na bufor wyjœciowy
    ; R8D - height         Wysokoœæ obrazu
    ; R9D - width          Szerokoœæ obrazu
    ; [RSP+40] - start     Indeks startowy
    ; [RSP+48] - end       Indeks koñcowy
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

    ; Zapisanie parametrów
    mov rsi, rcx          ; InBuffer -> RSI
    mov rdi, rdx          ; OutBuffer -> RDI
    mov r12d, r8d         ; height -> R12D
    mov r13d, r9d         ; width -> R13D
    
    ; Pobranie parametrów ze stosu
    mov r14d, dword ptr [rbp+48]    ; start -> R14D
    mov r15d, dword ptr [rbp+56]    ; end -> R15D
    mov ebx, dword ptr [rbp+64]     ; blockSize -> EBX

    ; Obliczenie startY
    mov eax, r14d         ; start do EAX
    mov ecx, r13d         ; width do ECX
    imul ecx, 3           ; Pomno¿enie przez 3 (RGB)
    cdq                   ; Przygotowanie do dzielenia
    idiv ecx              ; Dzielenie: startY = start / (width * 3)
    mov r8d, eax          ; Zapisanie startY

    ; Podobnie dla endY
    mov eax, r15d        ; Obliczenie endY
    cdq
    idiv ecx
    mov r9d, eax

    ; G³ówna pêtla po Y
outer_loop:              ; Pêtla po Y
    cmp r8d, r9d         ; Sprawdzenie czy y < endY
    jge done             ; Jeœli nie, koniec

    xor edx, edx         ; x = 0

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
    
 ; Seria porównañ sprawdzaj¹cych czy piksel jest na brzegu
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

    ; Inicjalizacja sum kolorów
    pxor xmm0, xmm0                 ; sum R
    pxor xmm1, xmm1                 ; sum G
    pxor xmm2, xmm2                 ; sum B
    xor r11d, r11d                  ; Licznik pikseli count = 0

    ; Pêtla dla okna rozmycia
    mov r14d, r10d                  ; half_block = blockSize/2
    neg r14d                        ; -half_block
                                    ; Pêtle po oknie rozmycia
blur_y:                             ; pêtla po y w oknie
    cmp r14d, r10d
    jg blur_done
    
    mov r15d, r10d
    neg r15d                        ; -half_block dla x

blur_x:                             ; pêtla po x w oknie
    cmp r15d, r10d
    jg next_blur_y

    ; Oblicz pozycjê dla piksela w oknie
    mov eax, r8d                    ; y
    add eax, r14d                   ; y + dy
    imul eax, r13d                  ; * width
    add eax, edx                    ; + x
    add eax, r15d                   ; + dx
    imul eax, 3         ; * 3 (RGB)

 ; Dodawanie wartoœci kolorów
    movzx ecx, byte ptr [rsi + rax]     ; Pobranie sk³adowej Blue
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm2, xmm3                    ; Dodanie do sumy
                                        
    movzx ecx, byte ptr [rsi + rax + 1] ; Pobranie sk³adowej Green
    cvtsi2ss xmm3, ecx                  ; Konwersja na float
    addss xmm1, xmm3                    ; Dodanie do sumy
    
    movzx ecx, byte ptr [rsi + rax + 2] ; Pobranie sk³adowej Red
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
    ; Obliczenie œredniej
    cvtsi2ss xmm3, r11d     ; Konwersja licznika na float
    divss xmm0, xmm3        ; Dzielenie R/count
    divss xmm1, xmm3        ; Dzielenie G/count
    divss xmm2, xmm3        ; Dzielenie B/count

    ; Oblicz pozycjê docelow¹
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
    inc edx                         ; nastêpny x
    jmp inner_loop

next_y:
    inc r8d                         ; nastêpny y
    jmp outer_loop

done:
    pop r15                         ; Przywrócenie zachowanych rejestrów
    pop r14
    pop r13
    pop r12
    pop rdi
    pop rsi
    pop rbx
    pop rbp
    ret                             ; Powrót z funkcji
GaussianBlurASM endp

end