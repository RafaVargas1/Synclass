import { secoesAluno, secoesProfessor, secoesDoPapel } from './secoesPorPapel';

describe('secoesPorPapel (issue #77)', () => {
  describe('secoesAluno', () => {
    it('expõe as telas do Aluno sem depender de usuarioId, ordenadas por frequência de uso ("Minhas aulas" primeiro)', () => {
      expect(secoesAluno()).toEqual([
        { label: 'Minhas aulas', href: '/aluno/minhas-aulas', icone: 'minhas-aulas' },
        { label: 'Entrar em nova turma', href: '/aluno/entrar-em-turma', icone: 'entrar-turma' },
        {
          label: 'Ver histórico de frequência',
          href: '/aluno/historico-frequencia',
          icone: 'historico',
        },
        { label: 'Ver valor devido', href: '/aluno/valor-devido', icone: 'valor-devido' },
      ]);
    });
  });

  describe('secoesProfessor', () => {
    it('devolve lista vazia sem usuarioId resolvido (todas as seções precisam do professorId na rota)', () => {
      expect(secoesProfessor(undefined)).toEqual([]);
    });

    it('inclui as seções com segmento dinâmico depois que usuarioId resolve, ordenadas por frequência de uso ("Fazer chamada" primeiro)', () => {
      expect(secoesProfessor('prof-1')).toEqual([
        { label: 'Fazer chamada', href: '/professor/prof-1/chamada', icone: 'chamada' },
        { label: 'Gerenciar horários', href: '/professor/prof-1/horarios', icone: 'horarios' },
        {
          label: 'Alocar Aluno em horário',
          href: '/professor/prof-1/alocacoes',
          icone: 'alocacoes',
        },
        { label: 'Meus Alunos', href: '/professor/prof-1/alunos', icone: 'alunos' },
        {
          label: 'Adicionar Aluno',
          href: '/professor/prof-1/alunos/adicionar',
          icone: 'adicionar-aluno',
        },
        { label: 'Ver valor devido', href: '/professor/prof-1/valor-devido', icone: 'valor-devido' },
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
