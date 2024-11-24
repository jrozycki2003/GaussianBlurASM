.data
ALIGN 16
    gaussian_kernel DD 256 DUP(0)  ; Space for Gaussian kernel (must be initialized)
    kernel_size DD 0               ; Current kernel size
    thread_count DD 1              ; Number of threads to use
    chunk_size DD 0                ; Size of chunk for each thread
    temp_buffer DD 1024 DUP(0)     ; Temporary buffer for convolution

.code

; Function to initialize Gaussian kernel
InitGaussianKernel PROC
    ; RCX - Radius
    ; Preserves all registers except RAX
    push rbx
    push rcx
    push rdx
    
    ; Calculate kernel size (2*radius + 1)
    mov eax, ecx
    add eax, eax
    inc eax
    mov [kernel_size], eax
    
    ; TODO: Add actual Gaussian kernel calculation here
    ; This should populate gaussian_kernel with proper weights
    ; Sum of weights should equal 1.0 when represented as fixed point
    
    pop rdx
    pop rcx
    pop rbx
    ret
InitGaussianKernel ENDP

; Macro to process a single pixel with proper bounds checking
process_pixel_sse MACRO
    LOCAL skip_pixel
    
    ; Check bounds
    cmp rcx, 0
    jl skip_pixel
    cmp rcx, r13
    jge skip_pixel
    
    ; Load pixel data (RGB)
    movzx eax, BYTE PTR [rsi + rcx*3]      ; R
    movzx ebx, BYTE PTR [rsi + rcx*3 + 1]  ; G
    movzx edx, BYTE PTR [rsi + rcx*3 + 2]  ; B
    
    ; Convert to packed words
    pinsrw xmm1, eax, 0                    ; Insert R
    pinsrw xmm2, ebx, 0                    ; Insert G
    pinsrw xmm3, edx, 0                    ; Insert B
    
skip_pixel:
ENDM

ProcessImage PROC
    ; RCX - Input buffer pointer
    ; RDX - Output buffer pointer
    ; R8 - Height
    ; R9 - Width
    ; [RSP+40] - Thread count
    ; [RSP+48] - Blur radius
    
    push rbp
    mov rbp, rsp
    sub rsp, 80h
    
    ; Save non-volatile registers
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
    mov r12, r8                     ; Height
    mov r13, r9                     ; Width
    mov eax, DWORD PTR [rbp+40h]   ; Thread count
    mov DWORD PTR [thread_count], eax
    mov r14d, DWORD PTR [rbp+48h]  ; Blur radius
    
    ; Initialize Gaussian kernel
    mov rcx, r14                    ; Pass radius to kernel init
    call InitGaussianKernel
    
    ; Calculate chunk size for each thread
    mov eax, r12d                   ; Height
    mul r13d                        ; Total pixels = height * width
    mov ebx, DWORD PTR [thread_count]
    div ebx                         ; Pixels per thread
    mov DWORD PTR [chunk_size], eax
    
    ; Initialize SSE
    pxor xmm7, xmm7                ; Zero XMM7 for accumulator
    
    ; Process chunks in parallel
    xor r15, r15                   ; Thread counter
    
thread_loop:
    ; Calculate chunk boundaries
    mov eax, DWORD PTR [chunk_size]
    mul r15d                       ; Start offset = chunk_size * thread_number
    mov r8d, eax                   ; R8D = start offset
    
    add eax, DWORD PTR [chunk_size]
    mov r9d, eax                   ; R9D = end offset
    
    ; Process each pixel in chunk
    mov rcx, r8                    ; Current pixel index
    
pixel_loop:
    ; Clear accumulators
    pxor xmm1, xmm1                ; R accumulator
    pxor xmm2, xmm2                ; G accumulator
    pxor xmm3, xmm3                ; B accumulator
    
    ; Apply kernel to neighborhood
    mov rbx, r14                   ; Kernel radius
    neg rbx                        ; Start from -radius
    
kernel_loop:
    ; Calculate neighbor pixel position
    mov rax, rcx
    add rax, rbx                   ; Add kernel offset
    
    ; Process pixel with bounds checking
mov eax, [rbx + rdx*4] ; Poprawiona skala

    
    inc rbx
    cmp rbx, r14                   ; Compare with radius
    jle kernel_loop
    
    ; Normalize and store result
    ; Convert accumulated fixed-point values back to bytes
    packuswb xmm1, xmm1            ; Pack R values
    packuswb xmm2, xmm2            ; Pack G values
    packuswb xmm3, xmm3            ; Pack B values
    
    ; Store processed pixel
mov eax, [rbx + rdx*4] ; Poprawiona skala

mov eax, [rbx + rdx*4] ; Poprawiona skala
mov eax, [rbx + rdx*4] ; Poprawiona skala
mov eax, [rbx + rdx*4] ; Poprawiona skala

mov eax, [rbx + rdx*4] ; Poprawiona skala

mov eax, [rbx + rdx*4] ; Poprawiona skala

    
    ; Move to next pixel
    inc rcx
    cmp rcx, r9
    jl pixel_loop
    
    ; Next thread
    inc r15d
    cmp r15d, DWORD PTR [thread_count]
    jl thread_loop
    
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
