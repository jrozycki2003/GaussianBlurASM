.data
ALIGN 16
    gaussian_kernel DD 256 DUP(0)  ; Space for Gaussian kernel
    kernel_size DD 0              ; Current kernel size
    two_pi DQ 3.14159265358979323846
    e_const DQ 2.71828182845904523536
    zero DQ 0.0
    one DQ 1.0
    two DQ 2.0

.code
ProcessImage PROC
    ; RCX - Input buffer pointer
    ; RDX - Output buffer pointer
    ; R8D - Width
    ; R9D - Height
    ; [RSP+40] - Blur radius

    push rbp
    mov rbp, rsp
    sub rsp, 80h

    ; Save registers
    push rbx
    push rsi
    push rdi
    push r12
    push r13
    push r14
    push r15

    ; Store parameters
    mov rsi, rcx                    ; Input buffer
    mov rdi, rdx                    ; Output buffer
    mov r12d, r8d                   ; Width
    mov r13d, r9d                   ; Height
    mov r14d, DWORD PTR [rbp+40h]   ; Blur radius

    ; Calculate kernel size
    xor eax, eax                    ; Clear EAX
    mov eax, r14d                   ; Move radius to EAX
    add eax, eax                    ; Multiply by 2
    inc eax                         ; Add 1
    mov DWORD PTR [kernel_size], eax ; Store kernel size

    ; Process pixels
    xor r8d, r8d                    ; Y counter = 0

process_rows:
    xor r9d, r9d                    ; X counter = 0

process_columns:
    ; Calculate pixel offset (y * width + x) * 4
    mov eax, r8d
    mul r12d                        ; EAX = y * width
    add eax, r9d                    ; Add x
    shl eax, 2                      ; Multiply by 4 (RGBA)
    mov r10d, eax                   ; Store offset

    ; Clear accumulators
    xorps xmm4, xmm4                ; Clear blue accumulator
    xorps xmm5, xmm5                ; Clear green accumulator
    xorps xmm6, xmm6                ; Clear red accumulator
    xorps xmm7, xmm7                ; Clear alpha accumulator

    ; Process kernel area
    mov r15d, r14d                  ; Kernel radius
    neg r15d                        ; Start from -radius

kernel_y:
    mov ebx, r14d
    neg ebx                         ; Start from -radius

kernel_x:
    ; Calculate source coordinates
    mov eax, r8d
    add eax, r15d                   ; Source Y = y + ky
    mov ecx, r9d
    add ecx, ebx                    ; Source X = x + kx

    ; Check boundaries
    test eax, eax
    js next_kernel                  ; Skip if Y < 0
    cmp eax, r13d
    jge next_kernel                 ; Skip if Y >= height
    test ecx, ecx
    js next_kernel                  ; Skip if X < 0
    cmp ecx, r12d
    jge next_kernel                 ; Skip if X >= width

    ; Calculate source offset
    push rax                        ; Save RAX
    mul r12d                        ; Y * width
    add eax, ecx                    ; Add X
    shl eax, 2                      ; Multiply by 4
    mov ecx, eax                    ; Store offset in ECX
    pop rax                         ; Restore RAX

    ; Load pixel colors
    movzx edx, BYTE PTR [rsi+rcx]   ; Blue
    cvtsi2ss xmm0, edx
    addss xmm4, xmm0

    movzx edx, BYTE PTR [rsi+rcx+1] ; Green
    cvtsi2ss xmm0, edx
    addss xmm5, xmm0

    movzx edx, BYTE PTR [rsi+rcx+2] ; Red
    cvtsi2ss xmm0, edx
    addss xmm6, xmm0

    movzx edx, BYTE PTR [rsi+rcx+3] ; Alpha
    cvtsi2ss xmm0, edx
    addss xmm7, xmm0

next_kernel:
    inc ebx
    cmp ebx, r14d
    jle kernel_x

    inc r15d
    cmp r15d, r14d
    jle kernel_y

    ; Calculate averages
    mov eax, DWORD PTR [kernel_size]
    mul eax                         ; Total pixels = kernel_size^2
    cvtsi2ss xmm0, eax
    
    ; Divide accumulated values by total pixels
    divss xmm4, xmm0               ; Blue
    divss xmm5, xmm0               ; Green
    divss xmm6, xmm0               ; Red
    divss xmm7, xmm0               ; Alpha

    ; Store processed pixel
    cvttss2si eax, xmm4
    mov BYTE PTR [rdi+r10], al      ; Blue

    cvttss2si eax, xmm5
    mov BYTE PTR [rdi+r10+1], al    ; Green

    cvttss2si eax, xmm6
    mov BYTE PTR [rdi+r10+2], al    ; Red

    cvttss2si eax, xmm7
    mov BYTE PTR [rdi+r10+3], al    ; Alpha

    ; Next pixel
    inc r9d
    cmp r9d, r12d
    jl process_columns

    ; Next row
    inc r8d
    cmp r8d, r13d
    jl process_rows

    ; Restore registers
    pop r15
    pop r14
    pop r13
    pop r12
    pop rdi
    pop rsi
    pop rbx

    mov rsp, rbp
    pop rbp
    ret

ProcessImage ENDP

END