import { NomesDiaSemana } from '@/lib/diaSemana';

/** Mesmo shape de seção que `SectionList` (`react-native`) espera em
 *  `sections` — `title`/`data`, não nomes em português, pra usar o
 *  resultado direto na prop sem mapear de novo em cada tela. */
export type SecaoDeHorarios<T> = { title: string; data: T[] };

function porHoraInicio<T extends { horaInicio: string }>(a: T, b: T): number {
  return a.horaInicio.localeCompare(b.horaInicio);
}

function formatarDataCurta(data: string): string {
  const [, mes, dia] = data.split('-');
  return `${dia}/${mes}`;
}

/**
 * Agrupa horários recorrentes (padrão semanal, sem data concreta) por dia,
 * na ordem de `NomesDiaSemana` (Domingo..Sábado), e por `horaInicio` dentro
 * de cada dia. Dias sem nenhum item não geram seção (evita cabeçalho vazio).
 */
export function agruparPorDiaSemana<T extends { diaSemana: number; horaInicio: string }>(
  itens: T[],
): SecaoDeHorarios<T>[] {
  return NomesDiaSemana.map((title, diaSemana) => ({
    title,
    data: itens.filter((item) => item.diaSemana === diaSemana).sort(porHoraInicio),
  })).filter((secao) => secao.data.length > 0);
}

/**
 * Agrupa ocorrências concretas (com `data`, ex: aulas já marcadas) por
 * data, em ordem cronológica ascendente, e por `horaInicio` dentro de cada
 * data — diferente de `agruparPorDiaSemana`, que agrupa pelo padrão
 * semanal recorrente, não por ocorrências datadas específicas.
 */
export function agruparPorData<T extends { data: string; diaSemana: number; horaInicio: string }>(
  itens: T[],
): SecaoDeHorarios<T>[] {
  const porData = new Map<string, T[]>();
  for (const item of itens) {
    const grupo = porData.get(item.data) ?? [];
    grupo.push(item);
    porData.set(item.data, grupo);
  }
  return [...porData.entries()]
    .sort(([dataA], [dataB]) => dataA.localeCompare(dataB))
    .map(([data, grupo]) => {
      const itensOrdenados = [...grupo].sort(porHoraInicio);
      return {
        title: `${NomesDiaSemana[itensOrdenados[0].diaSemana]}, ${formatarDataCurta(data)}`,
        data: itensOrdenados,
      };
    });
}
