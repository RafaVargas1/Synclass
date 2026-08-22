import { secoesAluno, secoesProfessor, secoesDoPapel } from './secoesPorPapel';

describe('secoesPorPapel (issue #77)', () => {
  describe('secoesAluno', () => {
    it('expõe as duas telas do Aluno sem depender de usuarioId', () => {
      expect(secoesAluno()).toEqual([
        { label: 'Ver histórico de frequência', href: '/aluno/historico-frequencia' },
        { label: 'Ver valor devido', href: '/aluno/valor-devido' },
      ]);
    });
  });

  describe('secoesProfessor', () => {
    it('sempre inclui Cadastrar Aluno, mesmo sem usuarioId resolvido', () => {
      expect(secoesProfessor(undefined)).toEqual([
        { label: 'Cadastrar Aluno', href: '/professor/alunos/cadastro' },
      ]);
    });

    it('inclui as seções com segmento dinâmico apenas depois que usuarioId resolve', () => {
      expect(secoesProfessor('prof-1')).toEqual([
        { label: 'Cadastrar Aluno', href: '/professor/alunos/cadastro' },
        { label: 'Gerenciar horários', href: '/professor/prof-1/horarios' },
        { label: 'Meus Alunos', href: '/professor/prof-1/alunos' },
        { label: 'Alocar Aluno em horário', href: '/professor/prof-1/alocacoes' },
        { label: 'Convidar Aluno', href: '/professor/prof-1/convites/novo' },
        { label: 'Ver valor devido', href: '/professor/prof-1/valor-devido' },
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
