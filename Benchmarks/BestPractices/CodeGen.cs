namespace Benchmarks.BestPractices;
internal class CodeGen
{
    static bool IsEven(int i) => (i % 2) == 0;
}


/*
mono:
Program:IsEven(System.Int32):System.Boolean:
       sub      rsp, 8
       mov      [rsp], r15
       mov      r15, rdi
       mov      rcx, r15
       shr      ecx, 1Fh
       mov      rax, r15
       add      eax, ecx
       and      rax, 1
       sub      eax, ecx
       test     eax, eax
       sete     al
       movzx    rax, al
       mov      r15, [rsp]
       add      rsp, 8
       ret

 6.0
Program:IsEven(int):bool:
       test     dil, 1
       sete     al
       movzx    rax, al
       ret  

8.0
Program:IsEven(int):bool (FullOpts):
G_M20272_IG01:  ;; offset=0x0000
G_M20272_IG02:  ;; offset=0x0000
       mov      eax, edi
       not      eax
       and      eax, 1
G_M20272_IG03:  ;; offset=0x0007
       ret  
*/