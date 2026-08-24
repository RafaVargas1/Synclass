import { useEffect, useReducer } from 'react';

/**
 * Segundos restantes até `expiraEm` (ISO), atualizados a cada segundo —
 * usado pelo contador do código de entrada de turma (5 minutos de
 * validade) na tela "Adicionar Aluno". Nunca vai abaixo de zero. Recalcula
 * a partir de `Date.now()` a cada render em vez de guardar o valor em
 * estado — o `useReducer` aqui só força um re-render por segundo (via
 * `setInterval`), quem calcula o valor de fato é o corpo da função, pra não
 * perder sincronia se a aba ficar em segundo plano e o `setInterval`
 * atrasar.
 */
export function useContagemRegressiva(expiraEm: string | undefined): number {
  const [, forcarTick] = useReducer((contador: number) => contador + 1, 0);

  useEffect(() => {
    if (!expiraEm) {
      return;
    }
    const intervalId = setInterval(forcarTick, 1000);
    return () => clearInterval(intervalId);
  }, [expiraEm]);

  return calcularSegundosRestantes(expiraEm);
}

function calcularSegundosRestantes(expiraEm: string | undefined): number {
  if (!expiraEm) {
    return 0;
  }
  const diferencaMs = new Date(expiraEm).getTime() - Date.now();
  return Math.max(0, Math.floor(diferencaMs / 1000));
}

/** Formata segundos como `mm:ss`, usado no contador visível da tela. */
export function formatarContagem(segundos: number): string {
  const minutos = Math.floor(segundos / 60);
  const segundosRestantes = segundos % 60;
  return `${String(minutos).padStart(2, '0')}:${String(segundosRestantes).padStart(2, '0')}`;
}
