import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { alocarAluno, desalocarAluno, listarAlocacoes } from '@/lib/api/alocacoes';
import { listarAlunosProvisorios } from '@/lib/api/alunosProvisorios';
import { ModeloAgendamento, obterConfiguracao } from '@/lib/api/configuracao';
import { listarHorarios } from '@/lib/api/horarios';

import AlocacoesProfessorScreen from './alocacoes';

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx). Mockado aqui pra manter este
// arquivo focado no contrato da própria tela, sem precisar mockar
// usePathname/useIsTelaLarga/usePerfilLogado só por causa do menu.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
  // `Link` monta um `asChild` renderizando o filho (padrão do Painel) — aqui
  // apenas repassa os filhos pra não precisar navegar de verdade no teste.
  Link: ({ children }: { children?: React.ReactNode }) => children,
}));

jest.mock('@/lib/api/horarios', () => ({ listarHorarios: jest.fn() }));
jest.mock('@/lib/api/alunosProvisorios', () => ({ listarAlunosProvisorios: jest.fn() }));
jest.mock('@/lib/api/alocacoes', () => ({
  listarAlocacoes: jest.fn(),
  alocarAluno: jest.fn(),
  desalocarAluno: jest.fn(),
}));
jest.mock('@/lib/api/configuracao', () => {
  const actual = jest.requireActual('@/lib/api/configuracao');
  return { ...actual, obterConfiguracao: jest.fn() };
});

const listarHorariosMock = listarHorarios as jest.Mock;
const listarAlunosProvisoriosMock = listarAlunosProvisorios as jest.Mock;
const listarAlocacoesMock = listarAlocacoes as jest.Mock;
const alocarAlunoMock = alocarAluno as jest.Mock;
const desalocarAlunoMock = desalocarAluno as jest.Mock;
const obterConfiguracaoMock = obterConfiguracao as jest.Mock;

const horario = { id: 'h1', diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60, limiteAlunos: 2 };
const aluno = { matriculaId: 'a1', nome: 'Ana', identificador: 'ana@x.com' };
const alocacao = { id: 'al1', horarioId: 'h1', matriculaId: 'a1', createdAt: '2026-01-01T00:00:00Z' };

describe('AlocacoesProfessorScreen', () => {
  beforeEach(() => {
    listarHorariosMock.mockReset();
    listarAlunosProvisoriosMock.mockReset();
    listarAlocacoesMock.mockReset();
    alocarAlunoMock.mockReset();
    desalocarAlunoMock.mockReset();
    obterConfiguracaoMock.mockReset();
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [horario] });
    listarAlunosProvisoriosMock.mockResolvedValue({ sucesso: true, alunos: [aluno] });
    listarAlocacoesMock.mockResolvedValue({ sucesso: true, alocacoes: [] });
  });

  it('shows a loading indicator while obterConfiguracao is pending', async () => {
    obterConfiguracaoMock.mockReturnValue(new Promise(() => {}));

    await render(<AlocacoesProfessorScreen />);

    expect(screen.getByLabelText('Carregando')).toBeTruthy();
  });

  it('shows an error message with a retry action when obterConfiguracao fails', async () => {
    obterConfiguracaoMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
    expect(screen.getByText('Tentar novamente')).toBeTruthy();
  });

  it('shows a message with a CTA instead of the grid when there are no horarios', async () => {
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [] });
    obterConfiguracaoMock.mockResolvedValue({
      sucesso: true,
      definida: true,
      modeloAgendamento: ModeloAgendamento.Fixo,
    });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() =>
      expect(screen.getByText('Nenhum horário cadastrado ainda.')).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar horários')).toBeTruthy();
    expect(screen.queryByText(/configure o modelo/i)).toBeNull();
  });

  it('asks to configure the model when it is not defined and horarios exist', async () => {
    obterConfiguracaoMock.mockResolvedValue({ sucesso: true, definida: false });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() =>
      expect(screen.getByText('Configure o modelo de agendamento antes de alocar Alunos.')).toBeTruthy(),
    );
  });

  it('explains the Livre model without the ninguém se inscreveu line when nobody joined', async () => {
    obterConfiguracaoMock.mockResolvedValue({
      sucesso: true,
      definida: true,
      modeloAgendamento: ModeloAgendamento.Vago,
    });
    listarAlocacoesMock.mockResolvedValue({ sucesso: true, alocacoes: [] });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() =>
      expect(
        screen.getByText('No modelo Livre, os Alunos se inscrevem sozinhos. Ainda ninguém se inscreveu em nenhum horário.'),
      ).toBeTruthy(),
    );
    expect(screen.queryByText(/vago/i)).toBeNull();
  });

  it('explains the Livre model without the ninguém se inscreveu line when there is an alocacao', async () => {
    obterConfiguracaoMock.mockResolvedValue({
      sucesso: true,
      definida: true,
      modeloAgendamento: ModeloAgendamento.Vago,
    });
    listarAlocacoesMock.mockResolvedValue({ sucesso: true, alocacoes: [alocacao] });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() =>
      expect(
        screen.getByText('No modelo Livre, os Alunos se inscrevem sozinhos — não há atribuição manual pelo Professor.'),
      ).toBeTruthy(),
    );
    expect(screen.queryByText(/ninguém se inscreveu/i)).toBeNull();
  });

  it.each([
    [ModeloAgendamento.Fixo, 'Fixo'],
    [ModeloAgendamento.Hibrido, 'Híbrido'],
  ])('renders the alocacao grid untouched for model %s', async (modeloAgendamento) => {
    obterConfiguracaoMock.mockResolvedValue({
      sucesso: true,
      definida: true,
      modeloAgendamento,
    });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());
    expect(screen.queryByText('Configure o modelo de agendamento antes de alocar Alunos.')).toBeNull();
    expect(screen.queryByText('Nenhum horário cadastrado ainda.')).toBeNull();
  });

  // As três a seguir preexistiam à issue #139 (cobertura de
  // AlocacoesConteudo/useGerenciamentoAlocacoes, fora de escopo desta
  // Task) — restauradas após terem sido removidas sem substituição
  // durante a implementação do gate de estado acima.
  it('shows an error message when listarHorarios fails, instead of a silent blank grid', async () => {
    obterConfiguracaoMock.mockResolvedValue({
      sucesso: true,
      definida: true,
      modeloAgendamento: ModeloAgendamento.Fixo,
    });
    listarHorariosMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
  });

  it('allocates the selected aluno and updates the card on success', async () => {
    obterConfiguracaoMock.mockResolvedValue({
      sucesso: true,
      definida: true,
      modeloAgendamento: ModeloAgendamento.Hibrido,
    });
    alocarAlunoMock.mockResolvedValue({ sucesso: true, alocacao });
    await render(<AlocacoesProfessorScreen />);
    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());

    await fireEvent.press(screen.getByText('Alocar'));

    expect(alocarAlunoMock).toHaveBeenCalledWith('h1', 'a1');
    await waitFor(() => expect(screen.getByText('1/2')).toBeTruthy());
  });

  it('deallocates the aluno and updates the card on success', async () => {
    obterConfiguracaoMock.mockResolvedValue({
      sucesso: true,
      definida: true,
      modeloAgendamento: ModeloAgendamento.Fixo,
    });
    listarAlocacoesMock.mockResolvedValue({ sucesso: true, alocacoes: [alocacao] });
    desalocarAlunoMock.mockResolvedValue({ sucesso: true });
    await render(<AlocacoesProfessorScreen />);
    await waitFor(() => expect(screen.getByText('1/2')).toBeTruthy());

    await fireEvent.press(screen.getByText('Remover'));

    expect(desalocarAlunoMock).toHaveBeenCalledWith('h1', 'a1');
    await waitFor(() => expect(screen.getByText('0/2')).toBeTruthy());
  });
});
