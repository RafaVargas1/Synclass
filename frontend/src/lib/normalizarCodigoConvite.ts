/**
 * Normaliza o código de convite digitado pelo Aluno para só dígitos,
 * tolerando espaços/máscara (ex: "12 345" ou "1-2-3-4-5" viram "12345") —
 * mesmo guardrail aplicado pelo Domain (`ConviteService.NormalizarCodigo`,
 * issue #63), aplicado também no cliente para dar feedback imediato ao
 * digitar, sem depender só da resposta da Api (defesa em profundidade: o
 * Domain normaliza de novo, não confia só no cliente).
 */
export function normalizarCodigoConvite(codigoBruto: string): string {
  return codigoBruto.replace(/\D/g, '').slice(0, 5);
}
