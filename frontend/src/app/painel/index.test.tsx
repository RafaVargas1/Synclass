import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { calcularPeriodoUltimosNDias, listarHistoricoFrequenciaDoAluno } from '@/lib/api/historicoFrequencia';
import { listarValorDevido } from '@/lib/api/valorDevido';
import { buscarPerfil } from '@/lib/api/usuarios';
import { listarVinculosAluno } from '@/lib/api/vinculosAluno';
import { listarProximasAulas } from '@/lib/api/cancelamentos';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { periodoDoDia } from '@/lib/periodoDoDia';

import PainelScreen from './index';

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => {
  const { Text } = jest.requireActual('react-native');
  return {
    useRouter: () => ({ replace: mockRouterReplace }),
    Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
      <Text testID={`link-${href}`}>{children}</Text>
    ),
  };
});

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx e TopbarAutenticada.test.tsx).
// Mockado aqui pra manter este arquivo focado no contrato do próprio
// Painel (conteúdo do corpo) — captura as props recebidas pra também
// cobrir o título da aba repassado (issue #133).
const mockTopbarAutenticada = jest.fn();
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: (props: { children?: React.ReactNode; tituloDaAba?: string }) => {
      mockTopbarAutenticada(props);
      return <View>{props.children}</View>;
    },
  };
});

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

jest.mock('@/lib/api/usuarios', () => ({
  buscarPerfil: jest.fn(),
}));

jest.mock('@/lib/api/valorDevido', () => ({
  listarValorDevido: jest.fn(),
}));

jest.mock('@/lib/api/historicoFrequencia', () => {
  const actual = jest.requireActual('@/lib/api/historicoFrequencia');
  return { ...actual, listarHistoricoFrequenciaDoAluno: jest.fn() };
});

jest.mock('@/lib/api/vinculosAluno', () => ({
  listarVinculosAluno: jest.fn(),
}));

jest.mock('@/lib/api/cancelamentos', () => ({
  listarProximasAulas: jest.fn(),
}));

jest.mock('@/lib/periodoDoDia', () => {
  const actual = jest.requireActual('@/lib/periodoDoDia');
  return { ...actual, periodoDoDia: jest.fn() };
});

const useSessaoMock = useSessao as jest.Mock;
const buscarPerfilMock = buscarPerfil as jest.Mock;
const listarValorDevidoMock = listarValorDevido as jest.Mock;
const listarVinculosAlunoMock = listarVinculosAluno as jest.Mock;
const listarProximasAulasMock = listarProximasAulas as jest.Mock;
const periodoDoDiaMock = periodoDoDia as jest.Mock;
const listarHistoricoFrequenciaDoAlunoMock = listarHistoricoFrequenciaDoAluno as jest.Mock;

function aula(status: string) {
  return {
    horarioId: 'h1',
    data: '2026-08-04',
    diaSemana: 2,
    horaInicio: '10:00:00',
    status,
  };
}

function aulaProxima(data: string, horaInicio: string) {
  return {
    horarioId: 'h1',
    data,
    diaSemana: 4,
    horaInicio,
    duracaoMinutos: 60,
    podeCancelar: true,
    cancelavelAte: '2026-08-19T18:00:00',
    prazoCancelamentoMinutos: 120,
  };
}

describe('PainelScreen', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    buscarPerfilMock.mockResolvedValue({ sucesso: false, mensagem: 'erro' });
    listarVinculosAlunoMock.mockReset();
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: [] });
    listarProximasAulasMock.mockReset();
    listarProximasAulasMock.mockResolvedValue({ sucesso: true, aulas: [] });
    periodoDoDiaMock.mockReturnValue('manha');
    mockTopbarAutenticada.mockReset();
    // O Painel com papelAtivo 'Aluno' monta o resumo (#167) que consulta o
    // histórico — resolver com lista vazia mantém este suite focada no
    // contrato do corpo do Painel (cards de ação, sair, saudações).
    listarHistoricoFrequenciaDoAlunoMock.mockReset();
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({ sucesso: true, historico: [] });
  });

  it('sets the tab title to Painel (issue #133)', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(mockTopbarAutenticada).toHaveBeenCalledWith(
      expect.objectContaining({ tituloDaAba: 'Painel' }),
    );
  });

  it('redirects to /login when there is no saved session', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: null,
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(mockRouterReplace).toHaveBeenCalledWith('/login'));
  });

  it('does not redirect while the saved session is still loading', async () => {
    useSessaoMock.mockReturnValue({
      carregando: true,
      token: null,
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(mockRouterReplace).not.toHaveBeenCalled();
  });

  it('shows an action card for each seção of the papel ativo, not a lone "Meu perfil" CTA', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Aluno',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByTestId('link-/aluno/historico-frequencia')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Ver histórico de frequência' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Ver valor devido' })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Meu perfil' })).toBeNull();
  });

  it('calls sair when the Sair button is pressed', async () => {
    const sair = jest.fn();
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      sair,
    });

    await render(<PainelScreen />);
    await fireEvent.press(screen.getByText('Sair'));

    expect(sair).toHaveBeenCalled();
  });

  it('gives the Sair button a touch target of at least 44x44 (issue #115)', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByRole('button', { name: 'Sair' })).toHaveStyle({
      minWidth: 44,
      minHeight: 44,
    });
  });
});

describe('PainelScreen saudação', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });
    listarVinculosAlunoMock.mockReset();
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: [] });
    listarProximasAulasMock.mockReset();
    listarProximasAulasMock.mockResolvedValue({ sucesso: true, aulas: [] });
    useSessaoMock.mockReturnValue({ carregando: false, token: 'token-jwt', sair: jest.fn() });
    listarHistoricoFrequenciaDoAlunoMock.mockReset();
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({ sucesso: true, historico: [] });
  });

  it.each([
    ['manha', 'Bom dia'],
    ['tarde', 'Boa tarde'],
    ['noite', 'Boa noite'],
  ])('exibe "{saudacao}, Ana" para o período %s', async (periodo, saudacao) => {
    periodoDoDiaMock.mockReturnValue(periodo);

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText(`${saudacao}, Ana`)).toBeTruthy());
  });

  it('não mostra saudação antes do perfil resolver', async () => {
    buscarPerfilMock.mockReturnValue(new Promise(() => {}));

    await render(<PainelScreen />);

    expect(screen.queryByText(/^(Bom dia|Boa tarde|Boa noite),/)).toBeNull();
  });
});

describe('PainelScreen resumo de valor a receber', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    periodoDoDiaMock.mockReturnValue('manha');
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });
  });

  it('mostra o total somado dos Alunos no mês e o rótulo "A receber este mês"', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });
    listarValorDevidoMock.mockResolvedValue({
      sucesso: true,
      valoresDevidos: [
        { matriculaId: 'm1', alunoUsuarioId: 'a1', nome: 'Bia', valor: 300, semRegraDefinida: false },
        { matriculaId: 'm2', alunoUsuarioId: 'a2', nome: 'Caio', valor: 450, semRegraDefinida: false },
      ],
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('A receber este mês')).toBeTruthy());
    expect(screen.getByText('R$ 750,00')).toBeTruthy();
  });

  it('mostra a mensagem de valor zerado sem valor em destaque quando a lista é vazia', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('Nenhum valor a receber neste mês.')).toBeTruthy());
    expect(screen.queryByText('R$ 0,00')).toBeNull();
  });

  it('mostra a mesma mensagem de valor zerado quando todos os Alunos estão sem regra definida', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });
    listarValorDevidoMock.mockResolvedValue({
      sucesso: true,
      valoresDevidos: [{ matriculaId: 'm1', alunoUsuarioId: 'a1', nome: 'Bia', valor: null, semRegraDefinida: true }],
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('Nenhum valor a receber neste mês.')).toBeTruthy());
    expect(screen.queryByText(/^R\$/)).toBeNull();
  });

  it('chama listarValorDevido com o usuarioId do Professor sem query string de período', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledWith('prof-1'));
    expect(listarValorDevidoMock).toHaveBeenCalledTimes(1);
    expect(listarValorDevidoMock.mock.calls[0][1]).toBeUndefined();
  });

  it('não mostra a seção "a receber" para o papel ativo Aluno', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Aluno',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByTestId('link-/aluno/historico-frequencia')).toBeTruthy());
    expect(screen.queryByText('A receber este mês')).toBeNull();
    expect(listarValorDevidoMock).not.toHaveBeenCalled();
  });
});

describe('PainelScreen resumo de frequência', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'aluno-1', nome: 'Ana' });
    periodoDoDiaMock.mockReturnValue('manha');
    listarHistoricoFrequenciaDoAlunoMock.mockReset();
  });

  it('shows the resumo with 3 presenças and 1 falta when papelAtivo is Aluno', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Aluno',
      sair: jest.fn(),
    });
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({
      sucesso: true,
      historico: [
        {
          professorId: 'p1',
          nomeProfessor: 'Professor A',
          aulas: [
            aula('Presente'),
            aula('Presente'),
            aula('Presente'),
            aula('Ausente'),
          ],
        },
      ],
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText(/3 presenças · 1 falta/)).toBeTruthy());
  });

  it('shows the empty message when all aulas are NaoRegistrada/Cancelada', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Aluno',
      sair: jest.fn(),
    });
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({
      sucesso: true,
      historico: [
        {
          professorId: 'p1',
          nomeProfessor: 'Professor A',
          aulas: [aula('NaoRegistrada'), aula('Cancelada')],
        },
      ],
    });

    await render(<PainelScreen />);

    await waitFor(() =>
      expect(
        screen.getByText(
          'Você ainda não tem frequência registrada nos últimos 30 dias. Suas aulas aparecem aqui assim que alguma presença ou falta for marcada.',
        ),
      ).toBeTruthy(),
    );
  });

  it('does not show the resumo when papelAtivo is Professor', async () => {
      vinculos: [
        { professorId: 'p1', nome: 'Professor A' },
        { professorId: 'p2', nome: 'Professor B' },
      ],
    });
    listarProximasAulasMock.mockImplementation(async (professorId: string) => {
      if (professorId === 'p1') {
        return { sucesso: true, aulas: [aulaProxima('2026-09-01', '10:00:00')] };
      }
      return { sucesso: true, aulas: [aulaProxima('2026-08-20', '18:00:00')] };
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('Professor B — 20/08/2026 às 18:00:00')).toBeTruthy());
  });

  it('does not show the resumo when papelAtivo is Professor', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('Bom dia, Ana')).toBeTruthy());
    expect(screen.queryByText(/presença|presenças/)).toBeNull();
    expect(listarHistoricoFrequenciaDoAlunoMock).not.toHaveBeenCalled();
  });

  it('degrades to null (no resumo) on network failure but keeps the actions visible', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Aluno',
      sair: jest.fn(),
    });
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'erro',
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByRole('button', { name: 'Ver histórico de frequência' })).toBeTruthy());
    expect(screen.queryByText(/presença|presenças/)).toBeNull();
    expect(screen.queryByText(/Carregando/)).toBeNull();
  });

  it('calls listarHistoricoFrequenciaDoAluno with the last 30 days period', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Aluno',
      sair: jest.fn(),
    });
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({ sucesso: true, historico: [] });

    await render(<PainelScreen />);

    const esperado = calcularPeriodoUltimosNDias(new Date(), 30);
    await waitFor(() => expect(listarHistoricoFrequenciaDoAlunoMock).toHaveBeenCalledWith(esperado));
  });
});

describe('PainelScreen próximo horário do Aluno (issue #165)', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'aluno-1', nome: 'Ana' });
    listarVinculosAlunoMock.mockReset();
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: [] });
    listarProximasAulasMock.mockReset();
    listarProximasAulasMock.mockResolvedValue({ sucesso: true, aulas: [] });
    periodoDoDiaMock.mockReturnValue('manha');
    useSessaoMock.mockReturnValue({ carregando: false, token: 'token-jwt', papelAtivo: 'Aluno', sair: jest.fn() });
  });

  it('Aluno com um Professor mostra o próximo horário dele', async () => {
    listarVinculosAlunoMock.mockResolvedValue({
      sucesso: true,
      vinculos: [{ professorId: 'p1', nome: 'Professor A' }],
    });
    listarProximasAulasMock.mockResolvedValue({
      sucesso: true,
      aulas: [aulaProxima('2026-08-20', '18:00:00')],
    });

    await render(<PainelScreen />);

    await waitFor(() =>
      expect(screen.getByText('Professor A — 20/08/2026 às 18:00')).toBeTruthy(),
    );
    expect(listarProximasAulasMock).toHaveBeenCalledWith('p1');
  });

  it('Aluno com dois Professores mostra o horário mais próximo, não o do primeiro da lista', async () => {
    listarVinculosAlunoMock.mockResolvedValue({
      sucesso: true,
      vinculos: [
        { professorId: 'p1', nome: 'Professor A' },
        { professorId: 'p2', nome: 'Professor B' },
      ],
    });
    listarProximasAulasMock.mockImplementation(async (professorId: string) => {
      if (professorId === 'p1') {
        return { sucesso: true, aulas: [aulaProxima('2026-09-01', '10:00:00')] };
      }
      return { sucesso: true, aulas: [aulaProxima('2026-08-20', '18:00:00')] };
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('Professor B — 20/08/2026 às 18:00')).toBeTruthy());
  });

  it('Aluno sem vínculo mostra o estado vazio', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: [] });

    await render(<PainelScreen />);

    await waitFor(() =>
      expect(screen.getByText('Nenhum horário marcado no momento.')).toBeTruthy(),
    );
    expect(listarProximasAulasMock).not.toHaveBeenCalled();
  });

  it('Aluno com vínculo mas sem aula futura mostra o estado vazio', async () => {
    listarVinculosAlunoMock.mockResolvedValue({
      sucesso: true,
      vinculos: [{ professorId: 'p1', nome: 'Professor A' }],
    });
    listarProximasAulasMock.mockResolvedValue({ sucesso: true, aulas: [] });

    await render(<PainelScreen />);

    await waitFor(() =>
      expect(screen.getByText('Nenhum horário marcado no momento.')).toBeTruthy(),
    );
  });

  it('mostra o estado vazio sem quebrar a tela quando listarVinculosAluno falha (achado de dev-review, PR #176)', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: false, mensagem: 'erro de rede' });

    await render(<PainelScreen />);

    await waitFor(() =>
      expect(screen.getByText('Nenhum horário marcado no momento.')).toBeTruthy(),
    );
    expect(listarProximasAulasMock).not.toHaveBeenCalled();
  });

  it('ignora só o Professor cuja consulta falhou, sem quebrar os demais (achado de dev-review, PR #176)', async () => {
    listarVinculosAlunoMock.mockResolvedValue({
      sucesso: true,
      vinculos: [
        { professorId: 'p1', nome: 'Professor A' },
        { professorId: 'p2', nome: 'Professor B' },
      ],
    });
    listarProximasAulasMock.mockImplementation(async (professorId: string) => {
      if (professorId === 'p1') {
        return { sucesso: false, mensagem: 'erro de rede' };
      }
      return { sucesso: true, aulas: [aulaProxima('2026-08-20', '18:00:00')] };
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('Professor B — 20/08/2026 às 18:00')).toBeTruthy());
  });

  it('não rende o resumo nem chama as funções novas quando o papel ativo é Professor', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.queryByText('Próximo horário')).toBeNull();
    expect(listarVinculosAlunoMock).not.toHaveBeenCalled();
    expect(listarProximasAulasMock).not.toHaveBeenCalled();
  });
});
