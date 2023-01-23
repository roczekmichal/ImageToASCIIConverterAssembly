.data
asciicodes db '#', '#', '@', '%', '=', '+', '*', ':', '-', '.', ' '

.code
convertLineAsm proc
;Arguments:
;RCX - lineNr, RDX - imageInBytesPtr, R8 - textLinePtr, R9 - ASCII_IM_W 

LOCAL startPos:QWORD, imageInBytesPtr:QWORD, textLinePtr:QWORD

	push rbp
	mov	rbp, rsp
mov imageInBytesPtr, RDX
mov textLinePtr, R8

mov RAX, RCX
mul R9  
mov RCX, RAX		    ; startPosAscii = lineNr * asciiImWidth
mov R11, 3
mul R11
mov startPos, RAX		; startPos = lineNr * asciiImWidth * 3

mov R15, R9

mov R10, 0              ; R10 is loop counter
mov RAX, 3
mul R9
mov R9, RAX             ; 3*ascii_width (LOOP counter top)



startloop:	
	mov RBX, imageInBytesPtr       
	add RBX, startPos   
	add RBX, R10        ; RBX = imageInBytesPtr + startPos + R10 (w) (loop counter)
	mov R11, 0
	mov R11B, [RBX]      ; R11 = red,
	inc RBX
	mov R12, 0
	mov R12B, [RBX]      ;  R12 - green,
	inc RBX
	mov R13, 0
	mov R13B, [RBX]		; R13 - blue.
	;mov R11, 128		; test
	;mov R12, 128
	;mov R13, 128

						; average RGB, R14 - greyscale result
	mov R14, R11    
	add R14, R12	    ; R + G 
	add R14, R13        ; (R + G) + B
	xor RDX, RDX
	mov RAX, R14
	mov R11, 3
	div R11
	mov R14, RAX        ; R14 holds calculated gray scale  

	mov RAX, 10			; convert gray scale to index
	mul R14				;
	xor RDX, RDX
	mov R14, 255	
	div R14				; index (RAX) = gray_scale * 10 / 255

	mov R14, RAX
	lea R11, asciicodes
	add R14, R11
	xor RAX, RAX
	mov AL, byte ptr [R14]		; R14 holds the index
	;mov AL, '@'
	movzx R14, AL		; R14 holds ascii char code

	XOR RDX, RDX
	mov RAX, R10
	mov R11, 3
	div R11				; w / 3
	add RAX, RCX		; RAX = w / 3 + start_pos_ascii
	mov R12, 2
	mul R12				; * 2 because C# holds chars as uint16
	add RAX, textLinePtr ; w / 3 + start_pos_ascii + textLinePtr 

	mov [RAX], R14


	inc R10
	inc R10
	inc R10

	dec R15
	jnz startloop


;;movzx  eax, byte [b2]   ; break the
;;mov    ah,  byte [b3]
;shl    eax, 16         ; partial-reg merge is pretty cheap on SnB/IvB, but very slow on Intel CPUs before Sandybridge.  AMD has no penalty, just (true in this case) dependencies
;mov    al,  byte [b0]
;mov    ah,  byte [b1]
    ;; 5 uops to load + merge 4 bytes into an integer reg, plus 2x merging costs
;movd   xmm0, eax      # cheaper than pinsrd xmm0, edx, 0.  Also zeros the rest of the vector

	pop rbp
ret
convertLineAsm endp
end















