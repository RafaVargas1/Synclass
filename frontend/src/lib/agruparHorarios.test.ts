import { agruparPorData, agruparPorDiaSemana } from './agruparHorarios';

describe('agruparPorDiaSemana', () => {
  it('agrupa por dia da semana, na ordem Domingo..Sábado, ordenado por horaInicio dentro do dia', () => {
    const itens = [
      { id: 'a', diaSemana: 2, horaInicio: '14:00:00' },
      { id: 'b', diaSemana: 1, horaInicio: '09:00:00' },
      { id: 'c', diaSemana: 2, horaInicio: '08:00:00' },
    ];

    const secoes = agruparPorDiaSemana(itens);

    expect(secoes.map((s) => s.title)).toEqual(['Segunda', 'Terça']);
    expect(secoes[0].data.map((i) => i.id)).toEqual(['b']);
    expect(secoes[1].data.map((i) => i.id)).toEqual(['c', 'a']);
  });

  it('omite dias sem nenhum item', () => {
    const secoes = agruparPorDiaSemana([{ diaSemana: 0, horaInicio: '10:00:00' }]);

    expect(secoes).toHaveLength(1);
    expect(secoes[0].title).toBe('Domingo');
  });

  it('devolve lista vazia quando não há itens', () => {
    expect(agruparPorDiaSemana([])).toEqual([]);
  });
});

describe('agruparPorData', () => {
  it('agrupa por data em ordem cronológica, ordenado por horaInicio dentro da data', () => {
    const itens = [
      { data: '2026-08-25', diaSemana: 2, horaInicio: '14:00:00' },
      { data: '2026-08-18', diaSemana: 2, horaInicio: '09:00:00' },
      { data: '2026-08-25', diaSemana: 2, horaInicio: '08:00:00' },
    ];

    const secoes = agruparPorData(itens);

    expect(secoes.map((s) => s.title)).toEqual(['Terça, 18/08', 'Terça, 25/08']);
    expect(secoes[1].data.map((i) => i.horaInicio)).toEqual(['08:00:00', '14:00:00']);
  });

  it('formata o título como "{DiaSemana}, {dd/mm}"', () => {
    const secoes = agruparPorData([{ data: '2026-08-19', diaSemana: 3, horaInicio: '10:00:00' }]);

    expect(secoes[0].title).toBe('Quarta, 19/08');
  });
});
