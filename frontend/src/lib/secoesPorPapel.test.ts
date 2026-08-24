import { secoesAluno, secoesProfessor, secoesDoPapel } from './secoesPorPapel';

describe('secoesPorPapel (issue #77)', () => {
  describe('secoesAluno', () => {
    it('expõe as telas do Aluno sem depender de usuarioId, ordenadas por frequência de uso ("Minhas aulas" primeiro)', () => {
      expect(secoesAluno()).toEqual([
        { label: 'Minhas aulas', href: '/aluno/minhas-aulas', icone: 'calendar-outline' },
        { label: 'Entrar em nova turma', href: '/aluno/entrar-em-turma', icone: 'enter-outline' },
        {
          label: 'Ver histórico de frequência',
          href: '/aluno/historico-frequencia',
          icone: 'bar-chart-outline',
        },
        { label: 'Ver valor devido', href: '/aluno/valor-devido', icone: 'cash-outline' },
      ]);
    });
  });

  describe('secoesProfessor', () => {
    it('devolve lista vazia sem usuarioId resolvido (todas as seções precisam do professorId na rota)', () => {
      expect(secoesProfessor(undefined)).toEqual([]);
    });

    it('inclui as seções com segmento dinâmico depois que usuarioId resolve, ordenadas por frequência de uso ("Fazer chamada" primeiro)', () => {
      expect(secoesProfessor('prof-1')).toEqual([
        { label: 'Fazer chamada', href: '/professor/prof-1/chamada', icone: 'clipboard-outline' },
        { label: 'Gerenciar horários', href: '/professor/prof-1/horarios', icone: 'time-outline' },
        {
          label: 'Alocar Aluno em horário',
          href: '/professor/prof-1/alocacoes',
          icone: 'link-outline',
        },
        { label: 'Meus Alunos', href: '/professor/prof-1/alunos', icone: 'people-outline' },
        {
          label: 'Adicionar Aluno',
          href: '/professor/prof-1/alunos/adicionar',
          icone: 'person-add-outline',
        },
        { label: 'Ver valor devido', href: '/professor/prof-1/valor-devido', icone: 'cash-outline' },
      ]);
    });
  });

  describe('secoesDoPapel', () => {
    it('retorna as seções do papel ativo Professor', () => {
      expect(secoesDoPapel('Professor', 'prof-1')).toEqual(secoesProfessor('prof-1'));
    });

    it('retorna as seções do papel ativo Aluno', () => {
      expect(secoesDoPapel('Aluno', undefined)).toEqual(secoesAluno());
    });

    it('retorna lista vazia para papel desconhecido', () => {
      expect(secoesDoPapel(undefined, undefined)).toEqual([]);
    });
  });
});
